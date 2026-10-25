# Pricely

Comparateur et veille de prix multi-enseignes. Parti du négoce de matériaux de construction, élargi à un comparateur généraliste.

Principe : **une offre = un produit × une enseigne → un prix**. On compare les offres d'un même produit de référence.

Lire avant de coder : `docs/architecture/ubiquitous-language.md`, `docs/architecture/decisions.md`, `docs/architecture/model.md`.

## Stack

- .NET 10 (`net10.0`), ASP.NET Core minimal APIs, .NET Aspire 13.6 (AppHost + ServiceDefaults)
- SQL Server : EF Core 10 (écriture) + Dapper (lecture), un `DbContext` et un schéma par module, `UseSnakeCaseNamingConvention()`
- MediatR (commandes / requêtes / pipeline behaviors), FluentValidation
- MassTransit in-memory + outbox / inbox maison (tables par module) traitées par des jobs Quartz
- Tests : xUnit, FluentAssertions, NetArchTest.Rules, Bogus

## Structure

```
aspire/                        AppHost (SQL Server) et ServiceDefaults (OpenTelemetry, health checks)
src/API/Pricely.Api            Host HTTP
src/Adapters/Pricely.Sdk       Composition : AddPricelyAsync() enregistre tous les modules, MapPricelyEndpoints() sous /api
src/Common/                    Briques partagées : Domain (Entity, Result, Error, DomainEvent), Application (ICommand, IQuery,
                               behaviors, IEventBus, IDbConnectionFactory, IDateTimeProvider, AI : ILlmGateway, AiFeature),
                               Infrastructure (outbox, inbox, auth, Quartz, MassTransit, Ai : passerelle + fournisseur Anthropic),
                               Presentation (IEndpoint, ApiResults, Mcp : serveur MCP HTTP)
src/Modules/<Module>/          Pricely.Modules.<Module>.{Domain,Application,Infrastructure,Presentation,IntegrationEvents}
src/Modules/<Module>/test/     <Module>.UnitTests (domaine), <Module>.ArchitectureTests (couches, conventions)
test/Pricely.ArchitectureTests Frontières entre modules, découverte des tools MCP
test/Pricely.Common.UnitTests  Passerelle LLM
```

## Modules

| Module | Agrégats | Rôle |
|---|---|---|
| Users | `User` | Profils, rôles, permissions métier (l'identité est portée par Keycloak, ADR-010) |
| Catalog | `ProductCategory`, `Product` | Produits de référence (GTIN, marque, référence fabricant) et typologies (arbre de 3 niveaux max) |
| Pricing | `Merchant`, `CollectionProfile`, `Offer`, `PriceObservation` | Enseignes, profils de collecte par typologie, offres, historique de prix, collecte |
| AiEngine | `Model` (+ `ModelPrice`), `FeatureSetting`, `LlmCall` | Catalogue des modèles et tarifs, modèle ciblé par fonctionnalité IA, journal des appels LLM et coûts, statistiques d'usage |

Pricing ne lit jamais Catalog : il garde des read models locaux `CatalogProduct` / `CatalogCategory`, alimentés par les événements d'intégration de Catalog (inbox, upsert idempotent sur l'identifiant source).

## Règles d'architecture (non négociables)

1. Chaque module est une Clean Architecture complète. **Aucune référence entre modules**, sauf vers le projet `IntegrationEvents` d'un autre module.
2. Graphe de références autorisé :

| Projet | Peut référencer |
|---|---|
| `<M>.Domain` | `Common.Domain` |
| `<M>.Application` | `<M>.Domain`, `Common.Application`, `<M>.IntegrationEvents` (pour publier) |
| `<M>.Presentation` | `<M>.Application`, `Common.Presentation`, `<Autre>.IntegrationEvents` (handlers d'événements entrants) |
| `<M>.Infrastructure` | `<M>.Application`, `<M>.Presentation`, `<M>.IntegrationEvents`, `Common.Infrastructure`, `<Autre>.IntegrationEvents` |
| `<M>.IntegrationEvents` | `Common.Application` uniquement |
| `Pricely.Sdk` | `Infrastructure` de chaque module |

3. Besoin d'une donnée d'un autre module : événement d'intégration + read model local. **Jamais** de référence de projet, jamais de clé étrangère entre schémas (colonne `Guid` simple, commentée).
4. `Domain` ne dépend ni d'EF Core, ni de MassTransit, ni d'ASP.NET, ni de Dapper.
5. Tout système externe passe par un port défini dans `Application` et implémenté dans `Infrastructure` (couche anti-corruption). Exemple : `IOfferCollector`.
6. Les tests `ArchitectureTests` font foi. Un test rouge signale un problème de conception, pas un test à modifier.

## Où va quoi (conventions du code existant)

- **Agrégat** : `Domain/<Agrégats>/<Agrégat>.cs`, hérite de `Entity`, `Guid Id`, constructeur privé sans paramètre, factory statique (`Register`, `Define`, `Propose`, `Create`...) qui retourne `Result<T>` si elle valide, setters privés, `Guid.CreateVersion7()`. Événements levés via `Raise(...)`.
- **Erreurs** : `Domain/<Agrégats>/<Agrégat>Errors.cs`, codes `"<Agrégats>.<Cas>"` (ex. `Offers.NotPendingReview`). Exceptions réservées aux cas anormaux.
- **Value Object** : `sealed record`, constructeur privé + `static Result<T> Create(...)` qui valide (`Money`, `Gtin`). Données descriptives sans invariant : `sealed record` positionnel (`MatchEvidence`, `ProductDescription`).
- **Événement de domaine** : `sealed class XxxDomainEvent(...) : DomainEvent`, propriétés `{ get; init; }`.
- **Commande** : `Application/<Agrégats>/<UseCase>/` avec `<UseCase>Command` (`sealed record : ICommand` ou `ICommand<T>`), `<UseCase>CommandHandler` (`internal sealed`), `<UseCase>CommandValidator` (`internal sealed`, FluentValidation).
- **Requête** : `<UseCase>Query` + `<X>Response` dans `Application`. Le handler est dans **Infrastructure** (`internal sealed`, Dapper + `IDbConnectionFactory`, SQL Server, colonnes snake_case aliasées avec `nameof`) sauf s'il a besoin de logique de domaine (alors dans Application, ex. `GetEffectiveCollectionProfileQueryHandler`).
- **Publication inter-modules** : un `DomainEventHandler<T>` dans Application publie l'`IntegrationEvent` via `IEventBus`. L'outbox garantit la livraison.
- **Consommation inter-modules** : `IntegrationEventHandler<T>` dans **Presentation** (`internal sealed`, suffixe `IntegrationEventHandler`) qui envoie une commande ; `PricingModule.ConfigureConsumers` déclare `IntegrationEventConsumer<T>` ; l'inbox du module assure l'idempotence.
- **Endpoint** : une classe `internal sealed` par endpoint dans `Presentation/<Agrégats>/`, implémente `IEndpoint`, route sans préfixe `/api` (ajouté par `MapPricelyEndpoints`), `.RequireAuthorization().WithTags(Tags.X)`, accepte un `CancellationToken`, renvoie `result.Match(Results.Ok|NoContent, ApiResults.Problem)`. DTO d'entrée : classe imbriquée `Request` avec `{ get; init; }`.
- **Persistance** : `Infrastructure/<Agrégats>/<Agrégat>Configuration.cs` et `<Agrégat>Repository.cs`. Toujours `builder.Ignore(x => x.DomainEvents)`. Enums en chaîne (`HasConversion<string>()`). VO non nuls en `ComplexProperty` avec `HasColumnName` explicite ; VO nullable en `HasConversion`. Collections possédées : `OwnsMany` + `UsePropertyAccessMode(Field)`. Entité enfant (`HasMany`) dont l'`Id` est fixé par le domaine : `ValueGeneratedNever()`, sinon EF Core fait un `UPDATE` au lieu d'un `INSERT` quand on l'ajoute à un agrégat déjà suivi.
- **Module** : `Infrastructure/<Module>Module.cs` expose `HandlerAssemblies` et `Add<Module>Module(configuration, connectionString)`, enregistré dans `Pricely.Sdk/ServiceCollectionExtensions.cs`. Options dans `appsettings.json` sous `<Module>:Outbox|Inbox|...`.

## Analyseurs : pièges connus

Le build échoue sur le moindre avertissement (`TreatWarningsAsErrors`, `AnalysisMode=All`, `EnforceCodeStyleInBuild`, SonarAnalyzer, `.editorconfig` strict). Pièges déjà rencontrés :

- `var` **obligatoire** quand le type est évident (IDE0007) : `var id = Guid.NewGuid();`, `var x = Foo.Create()` quand `Create` retourne `Foo`, `var d = list.ToDictionary(...)`. Types **explicites** partout ailleurs, y compris les types intégrés (`bool`, `string`, `int`) et les résultats `await`.
- `Result<IReadOnlyCollection<T>>` : la conversion implicite ne marche pas vers une interface. Écrire `Result.Success<IReadOnlyCollection<T>>(list)`.
- Plus de 7 paramètres (S107), constructeur primaire compris : regrouper en record ou utiliser des propriétés `required init` (événements d'intégration, `CollectedOffer`).
- Lambdas à une seule instruction : corps expression (IDE0053). Propriétés : corps expression.
- Pas de parenthèses inutiles, `if` / `else` d'affectation simple → ternaire, `??` / `?.` quand possible.
- Paramètres `string` nommés `*url*` / `*uri*` interdits (CA1054) : utiliser `Uri` ou un autre nom (`website`, `productPage`).
- Toujours propager le `CancellationToken` (CA2016). Dapper : `new CommandDefinition(sql, param, cancellationToken: ct)`.
- Comparaisons de chaînes : `StringComparison.Ordinal*`, `ToUpperInvariant()`. `List<T>.Find` plutôt que `FirstOrDefault`.

## Domaine Pricing : règles métier

- **Rapprochement** : créer une `Offer`, c'est rapprocher une page enseigne d'un produit de référence. `OfferMatchingPolicy` confirme automatiquement uniquement sur un GTIN identique (comparé sur 14 chiffres) ou une marque **et** une référence fabricant identiques (normalisées). Tout le reste reste `Proposed` et attend une revue humaine (`Confirm` / `Reject`). Une offre rejetée n'accepte plus d'observations.
- **Profil de collecte** par typologie : enseignes favorites → la typologie la plus proche qui en définit l'emporte (remplacement) ; instructions → concaténées de la racine vers la feuille. Voir `CollectionProfileResolver`.
- **Garde-fou prix** : une observation qui s'écarte de plus de 40 % de la médiane des dernières observations des autres offres confirmées (même devise) est signalée (`PriceAnomalyPolicy`).
- **Collecte** : `CollectProductOffersCommand` → `OfferCollectionPlanner` (produit, profil résolu, enseignes actives) → `IOfferCollector` (port) → `CollectedOfferRecorder` (une sauvegarde par offre collectée). Les offres d'enseignes non demandées sont ignorées. `CollectOffersJob` (Quartz) est désactivé par défaut (`Pricing:Collection:Enabled`).
- L'implémentation actuelle de `IOfferCollector` est `NotConfiguredOfferCollector`, qui échoue explicitement. La cible est l'agent IA de Pricely (ADR-008), qui passe par la passerelle LLM (ADR-007) et les tools de collecte (ADR-009) ; scrapers dédiés et flux partenaires en complément. On n'utilise pas le runtime générique `Dea972/Agent`.

## IA, agent et MCP (ADR-007 à 010)

- **Tout appel LLM** passe par `ILlmGateway.ForFeatureAsync(feature)` puis `LlmClient.SendAsync(prompt)`. Un module déclare ses `AiFeature` (code `"<module>.<usage>"`, fournisseur par défaut) dans `Application/<Module>AiFeatures.cs` et les enregistre par `services.AddAiFeature(...)` dans `<Module>Module` (exemple : `PricingAiFeatures.OfferCollection`). Jamais d'injection directe d'un fournisseur.
- **Fournisseurs** : `LlmServiceBase` (Common.Application.AI) ; implémentations en services à clé dans `Common.Infrastructure/Ai`, enregistrées une fois par `AddAiProviders` (Anthropic, clé `Ai:Anthropic:ApiKey` en user-secrets). Chaque appel publie `LlmCalledIntegrationEvent`.
- **AiEngine** remplace `ILlmTargetResolver` (modèle ciblé par fonctionnalité, cible `default`), journalise les appels via son inbox (`LlmCall`, coût figé au tarif du jour UTC) et expose les statistiques (`aiengine/usage/*`). Les agents hébergés (Foundry, Anthropic Managed Agents) ne sont pas encore repris.
- **Tools** : classes `public static` `[McpServerToolType]` dans `Presentation/Tools/` de chaque module, une méthode par tool, `[McpServerTool(Name = "snake_case", ReadOnly = ..., Destructive = ...)]` + `[Description]` en français, paramètres `ISender` et `CancellationToken` injectés, une requête ou commande par tool, `result.ValueOrToolError()`. Skill `add-agent-tool`. Jamais de logique métier dans un tool.
- **MCP** : `AddPricelyMcpServer` (HTTP streamable, stateless) et `MapPricelyMcp` (`/mcp`, authentifié) dans `Pricely.Sdk` ; chaque module expose `ToolAssembly`.
- **Agent interne** (`IChatClient` + `FunctionInvokingChatClient`, approbations, conversations) : à venir (étape 4 de la feuille de route).
- **Authentification** (ADR-010, ADR-011) : Keycloak (OAuth 2.1 / OIDC, realm `pricely` importé par l'AppHost) ; Pricely valide les jetons (`AddJwtBearer`, `Authentication:Authority` / `Audience`) et n'en émet pas. Scopes `pricely.read|write|review` = politiques d'autorisation du même nom (`PricelyScopes`, `.RequireAuthorization(PricelyScopes.Read)`) ; `/mcp` exige `pricely.read`. `sub` = `User.IdentityId`, le `User` est créé (rôle `Member`) à la première requête authentifiée (`ProvisionUserCommand`) ; `CustomClaimsTransformation` ajoute `pricely_user_id` et les permissions métier du module Users.
- Référence d'implémentation : AiEngine et Common AI de ChaussonStore (patterns uniquement, pas de code métier Chausson).

## Commandes

```bash
dotnet restore Pricely.slnx
dotnet build Pricely.slnx -c Release
dotnet test src/Modules/Pricing/test/Pricely.Modules.Pricing.UnitTests
dotnet test test/Pricely.ArchitectureTests
dotnet list Pricely.slnx package --vulnerable --include-transitive
dotnet ef migrations add <Name> -p src/Modules/<M>/Pricely.Modules.<M>.Infrastructure -s src/API/Pricely.Api -c <M>DbContext -o Database/Migrations   # avec ConnectionStrings__pricely-db défini
dotnet run --project aspire/Pricely.AppHost   # SQL Server + Keycloak + API ; secret : Parameters:anthropic-api-key
```

La CI (`.github/workflows/ci.yml`) restaure, compile en Release et lance tous les projets `*.UnitTests` et `*.ArchitectureTests`. En cas d'échec, elle publie les erreurs en annotations (titres « Build errors », « Error codes », « Vulnerable packages ») et toujours un « Test summary ».

## Sécurité des dépendances

L'audit NuGet (transitif compris) est **bloquant** : ne jamais ajouter `NU190x` à `NoWarn` / `WarningsNotAsErrors`. Corriger en montant le package direct ; si le parent n'embarque pas encore la version corrigée, référencer directement la version corrigée dans le projet le plus proche de la source (règle « nearest wins »), avec un commentaire citant l'avis GHSA. Ne pas activer `CentralPackageTransitivePinningEnabled` (provoque des rétrogradations en cascade). Secrets : user-secrets ou variables d'environnement, jamais dans le dépôt.

## État connu / à faire

- L'API démarre (`Program.cs` : modules, authentification, `/api` et `/mcp`, validation de la DI) ; en Development elle applique les migrations des quatre modules au démarrage. L'AppHost complet (SQL Server + Keycloak + API) n'a pas encore été exécuté de bout en bout dans un environnement avec conteneurs : le faire avant de s'appuyer dessus.
- Migrations `Initial` de Catalog, Pricing, Users et AiEngine en place (`Database/Migrations`, `generated_code = true`). Hors Development, aucun mécanisme n'applique les migrations (décision ouverte).
- Seul `/mcp` exige un scope (`pricely.read`) ; les endpoints existants sont « authentifié » et le filtrage des tools par scope reste à faire.
- Les tests d'intégration hérités (Users, solution) sont exclus de la solution : ils référencent un module `Ticketing` inexistant.
- `Users/USAGE_GUIDE.md` décrit un template `dotnet new` absent du dépôt : ne pas s'y fier, utiliser le skill `new-module`.
- Le code Key Vault de `Common.Infrastructure` (`AzureKeyvaultClient`, `ConfigureWithSecretsAsync`) n'est plus utilisé par l'authentification ; à supprimer ou à réutiliser pour les secrets.

## Méthode de travail

- Utiliser les skills du dépôt : `new-module`, `new-aggregate`, `new-use-case`, `new-integration-event`, `add-migration`, `add-offer-collector`, `add-agent-tool`.
- Proposer un plan court avant une tâche qui touche plusieurs projets ; attendre validation pour les décisions d'architecture et les consigner dans `docs/architecture/decisions.md`.
- Code, commentaires, commits en anglais (Conventional Commits). Échanges et docs en français.
- Avant de terminer : `dotnet build` + `dotnet test`, puis le sous-agent `architecture-reviewer` sur le diff.
- Ne pas inventer de fonctionnalité hors demande.
