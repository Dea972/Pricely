# Pricely

Comparateur et veille de prix multi-enseignes. Parti du négoce de matériaux de construction, élargi à un comparateur généraliste.

Principe : **une offre = un produit × une enseigne → un prix**. On compare les offres d'un même produit de référence.

Lire avant de coder : `docs/architecture/ubiquitous-language.md`, `docs/architecture/decisions.md`, `docs/architecture/model.md`, `.claude/rules/backend-architecture.md` (couches, CQRS, contrats). Avant de toucher au front : `docs/design/README.md` (maquettes, tokens, principes de l'assistant).

## Stack

- .NET 10 (`net10.0`), ASP.NET Core minimal APIs, .NET Aspire 13.6 (AppHost + ServiceDefaults)
- PostgreSQL (Npgsql) : EF Core 10 (écriture et lectures, `AsNoTracking` + projection) ; Dapper seulement dans l'Infrastructure, pour une lecture que LINQ rend mal (jointure latérale, agrégat `FILTER`, médiane), derrière un `ReadStore` ; un `DbContext` et un schéma par module, `UseSnakeCaseNamingConvention()`
- MediatR (commandes / requêtes / pipeline behaviors), FluentValidation
- MassTransit in-memory + outbox / inbox maison (tables par module) traitées par des jobs Quartz
- Tests : xUnit, FluentAssertions, NetArchTest.Rules, Bogus, bUnit (composants du front)

## Structure

```
aspire/                        AppHost (PostgreSQL + pgAdmin, Keycloak) et ServiceDefaults (OpenTelemetry, health checks)
src/API/Pricely.Api            Host HTTP (n'applique aucune migration)
src/API/Pricely.MigrationService  Worker lancé avant l'API par l'AppHost : applique les migrations de tous les modules puis s'arrête (ADR-018)
src/API/Pricely.DevSeed        Jeu d'essai local (typologies, produits, enseignes, profils, 13 semaines de prix) par les commandes des modules ;
                               ressource `pricely-seed` de l'AppHost, lancée à la main, rejouable sans doublon
src/Web/Pricely.Web            Front Blazor (ADR-015) : Interactive Server, OIDC côté serveur, appelle l'API en HTTP uniquement
src/Agents/                    Agents de Pricely (ADR-014) : un projet par agent (Pricely.Agents.Assistant), déclarations sur
                               Common uniquement ; ils sont exécutés par le module Agentic
src/Adapters/Pricely.Sdk       Composition : AddPricelyAsync() enregistre tous les modules, MapPricelyEndpoints() sous /api
src/Common/                    Briques partagées : Domain (Entity, Result, Error, DomainEvent), Application (ICommand, IQuery,
                               behaviors, IEventBus, IDateTimeProvider, AI : ILlmGateway, AiFeature),
                               Infrastructure (IDbConnectionFactory, outbox, inbox, auth, Quartz, MassTransit, Ai : passerelle + fournisseur Anthropic),
                               Presentation (IEndpoint, ApiResults, Mcp : serveur MCP HTTP)
src/Modules/<Module>/          Pricely.Modules.<Module>.{Domain,Application,Infrastructure,Presentation,IntegrationEvents,Contracts}
                               (`IntegrationEvents` et `Contracts` sont optionnels : ce que le module publie, ce qu'il laisse lire, ADR-022)
src/Modules/<Module>/test/     <Module>.UnitTests (domaine), <Module>.ArchitectureTests (couches, conventions)
test/Pricely.ArchitectureTests Frontières entre modules, découverte des tools MCP
test/Pricely.Common.UnitTests  Passerelle LLM
```

## Modules

| Module | Agrégats | Rôle |
|---|---|---|
| Users | `User` | Profils, rôles, permissions métier (l'identité est portée par Keycloak, ADR-010) |
| Catalog | `ProductCategory`, `Product` | Produits de référence (GTIN, marque, référence fabricant) et typologies (arbre de 3 niveaux max) ; contrat `IProductCategoryReader` (lecture des typologies par les autres modules) |
| Pricing | `Merchant`, `CollectionProfile`, `Offer`, `PriceObservation` | Enseignes, profils de collecte par typologie, offres, historique de prix, collecte |
| AiEngine | `Model` (+ `ModelPrice`), `FeatureSetting`, `LlmCall`, `AgentActivity`, `WebSearch` | Catalogue des modèles et tarifs, modèle ciblé par fonctionnalité IA, journal des appels LLM, des recherches web et coûts, journal des tools et décisions des agents, statistiques d'usage |
| Agentic | `AgentConversation`, `PendingAction` | Module générique (réutilisable, ne connaît aucun autre module) : exécute les agents déclarés (`AddAgent`) avec les tools fournis (`AddAgentTools`), conversations, actions d'écriture soumises à validation |

Pricing ne lit jamais la base de Catalog. Les **typologies** se lisent par le contrat `Catalog.Contracts` (`IProductCategoryReader`, ADR-022) : pas de copie. Les **produits** aussi (`IProductReader`, `ProductSummary`, ADR-025) : Pricing ne garde aucune copie du catalogue, il lit chez Catalog quand il en a besoin (planificateur de collecte, noms et identité des produits des offres).

## Règles d'architecture (non négociables)

1. Chaque module est une Clean Architecture complète. **Aucune référence entre modules**, sauf vers le projet `IntegrationEvents` ou `Contracts` d'un autre module.
2. Graphe de références autorisé :

| Projet | Peut référencer |
|---|---|
| `<M>.Domain` | `Common.Domain` |
| `<M>.Application` | `<M>.Domain`, `Common.Application`, `<M>.IntegrationEvents` (pour publier), `<Autre>.Contracts` (pour lire) |
| `<M>.Presentation` | `<M>.Application`, `Common.Presentation`, `<Autre>.IntegrationEvents` (handlers d'événements entrants) |
| `<M>.Infrastructure` | `<M>.Application`, `<M>.Presentation`, `<M>.IntegrationEvents`, `<M>.Contracts` (pour l'implémenter), `Common.Infrastructure`, `<Autre>.IntegrationEvents` |
| `<M>.IntegrationEvents` | `Common.Application` uniquement |
| `<M>.Contracts` | `Common.Domain` uniquement (pour `Result`) |
| `Pricely.Sdk` | `Infrastructure` de chaque module |

3. Besoin d'une donnée d'un autre module : lire ponctuellement → son **contrat** (`<Autre>.Contracts`, lu depuis Application) ; réagir à un changement → **événement d'intégration** ; garder une copie locale (événement + read model) seulement si on le justifie (lecture très fréquente, historisation, disponibilité). **Jamais** d'autre référence de projet, jamais de clé étrangère entre schémas (colonne `Guid` simple, commentée). Jamais d'interrogation en boucle d'un contrat pour détecter un changement.
4. `Domain` ne dépend ni d'EF Core, ni de MassTransit, ni d'ASP.NET, ni de Dapper. `Application` ne dépend ni d'EF Core ni de Dapper (ni d'aucun accès aux données) : elle lit et écrit par des ports (dépôts, `ReadStore`) implémentés dans `Infrastructure`.
5. Tout système externe passe par un port défini dans `Application` et implémenté dans `Infrastructure` (couche anti-corruption). Exemple : `IOfferCollector`.
6. Les tests `ArchitectureTests` font foi. Un test rouge signale un problème de conception, pas un test à modifier.

## Où va quoi (conventions du code existant)

- **Agrégat** : `Domain/<Agrégats>/<Agrégat>.cs`, hérite de `Entity`, `Guid Id`, constructeur privé sans paramètre, factory statique (`Register`, `Define`, `Propose`, `Create`...) qui retourne `Result<T>` si elle valide, setters privés, `Guid.CreateVersion7()`. Événements levés via `Raise(...)`.
- **Erreurs** : `Domain/<Agrégats>/<Agrégat>Errors.cs`, codes `"<Agrégats>.<Cas>"` (ex. `Offers.NotPendingReview`). Exceptions réservées aux cas anormaux.
- **Value Object** : `sealed record`, constructeur privé + `static Result<T> Create(...)` qui valide (`Money`, `Gtin`). Données descriptives sans invariant : `sealed record` positionnel (`MatchEvidence`, `ProductDescription`).
- **Événement de domaine** : `sealed class XxxDomainEvent(...) : DomainEvent`, propriétés `{ get; init; }`.
- **Commande** : `Application/<Agrégats>/<UseCase>/` avec `<UseCase>Command` (`sealed record : ICommand` ou `ICommand<T>`), `<UseCase>CommandHandler` (`internal sealed`), `<UseCase>CommandValidator` (`internal sealed`, FluentValidation).
- **Requête** : `<UseCase>Query`, `<UseCase>QueryHandler` et `<X>Response` dans `Application/<Agrégats>/<UseCase>/`. **Le handler est toujours dans Application** (ADR-023), jamais dans Infrastructure (`QueryHandlerTests`), `internal sealed`. Il ne touche pas aux données : il appelle un **`I<Sujet>ReadStore`** (port de lecture défini dans `Application/<Agrégats>/`, ADR-024) qui renvoie des `Response` ou des lignes, et fait ce qui reste (erreur `NotFound`, règle du domaine, enrichissement par le contrat d'un autre module). Le `ReadStore` est `internal sealed` dans `Infrastructure/<Agrégats>/<Sujet>ReadStore.cs` : EF Core `AsNoTracking` + projection `Select`, ou, si LINQ le rend mal, Dapper sur `IDbConnectionFactory` (`Common.Infrastructure`), PostgreSQL, colonnes snake_case aliasées avec `nameof`. Un dépôt du Domain suffit si la requête a besoin d'un agrégat (ex. `GetEffectiveCollectionProfileQueryHandler`).
- **Contrat** (ADR-022) : `Pricely.Modules.<M>.Contracts/<Sujet>/` — interfaces `I<Sujet>Reader`, `sealed record` immuables, erreurs statiques ; méthodes `Task<Result<T>>` avec `CancellationToken` en dernier, volumes bornés ; aucun type du Domain. Implémentation `internal sealed` dans `<M>.Infrastructure/Contracts/<Sujet>/` (EF Core `AsNoTracking` + projection, pas de requête MediatR, pas de règle métier), enregistrée dans `<M>Module`. Exemple : `IProductCategoryReader` lu par `CollectionProfileLookup`.
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
- Toujours propager le `CancellationToken` (CA2016). Dapper (dans un `ReadStore`) : `new CommandDefinition(sql, param, cancellationToken: ct)`.
- Comparaisons de chaînes : `StringComparison.Ordinal*`, `ToUpperInvariant()`. `List<T>.Find` plutôt que `FirstOrDefault`.

## PostgreSQL : pièges connus (ADR-012)

- Les comparaisons de texte sont **sensibles à la casse** (SQL Server ne l'était pas) : colonnes d'unicité « métier » (e-mail, nom d'enseigne, nom de typologie, identifiant de modèle, code de fonctionnalité) en `citext` (`HasColumnType("citext")` + `modelBuilder.HasPostgresExtension("citext")`) ; recherches en `ILIKE`.
- `DateTime` écrit en `timestamptz` : `Kind` doit être `Utc` (`IDateTimeProvider.UtcNow`), jamais `Now` / `Unspecified`.
- Une colonne `date` est lue par Dapper en `DateOnly` : `CAST(col AS timestamp)` dans le SQL si la réponse est un `DateTime`.
- Paramètre nullable testé par `IS NULL` : `CAST(@p AS <type>) IS NULL` (Npgsql n'infère pas le type d'un `NULL`).
- Pas de `TOP` / `CROSS APPLY` / `OUTER APPLY` : `LIMIT`, `JOIN LATERAL (... LIMIT 1) x ON TRUE`, `LEFT JOIN LATERAL ... ON TRUE`. Booléens : `WHERE flag`, pas `= 1`.
- Contenu des messages outbox / inbox en `text`, pas `jsonb` (jsonb réordonne les clés, or `$type` doit rester en premier). Relecture des lots avec `FOR UPDATE SKIP LOCKED`.

## Domaine Pricing : règles métier

- **Rapprochement** : créer une `Offer`, c'est rapprocher une page enseigne d'un produit de référence. `OfferMatchingPolicy` confirme automatiquement uniquement sur un GTIN identique (comparé sur 14 chiffres) ou une marque **et** une référence fabricant identiques (normalisées). Tout le reste reste `Proposed` et attend une revue humaine (`Confirm` / `Reject`). Une offre rejetée n'accepte plus d'observations.
- **Profil de collecte** par typologie : enseignes favorites → la typologie la plus proche qui en définit l'emporte (remplacement) ; instructions → concaténées de la racine vers la feuille. Voir `CollectionProfileResolver`.
- **Garde-fou prix** : une observation qui s'écarte de plus de 40 % de la médiane des dernières observations des autres offres confirmées (même devise) est signalée (`PriceAnomalyPolicy`).
- **Collecte** : `CollectProductOffersCommand` → `OfferCollectionPlanner` (produit, profil résolu, enseignes actives) → `IOfferCollector` (port) → `CollectedOfferRecorder` (une sauvegarde par offre collectée). Les offres d'enseignes non demandées sont ignorées. `CollectOffersJob` (Quartz) est désactivé par défaut (`Pricing:Collection:Enabled`).
- `IOfferCollector` = `AgentOfferCollector` (ADR-017, ADR-026) : d'abord, pour chaque enseigne qui déclare une page produit par GTIN (`Merchant.ProductPageTemplate`, `{gtin13}` / `{gtin14}`), `ProductPageShortcut` ouvre cette page et lit l'offre dans ses données structurées sans le modèle (même GTIN exigé) ; ensuite l'agent `pricing-collector` (fonctionnalité `pricing.offer-collection`) cherche sur le site de chaque enseigne (`Merchant.SearchTemplate` avec `{query}`, pas d'API de recherche web) et soumet ce qu'il lit. Toute page lue passe par `MerchantPageReader` (adresse et redirections sur le site de l'enseigne, `Merchant.Owns` ; connexion aux seules adresses publiques, `PublicAddressGuard` ; instantané `PageSnapshot`) ; tout prix soumis par `SubmittedOfferVerifier` (le prix doit figurer dans l'instantané cité, et être l'un des prix que la page déclare en JSON-LD quand elle en déclare, `PageSnapshot.DeclaredPrices`, ce qui écarte livraison et éco-contribution ; jamais une page de recherche ; un GTIN, une marque ou une référence absents de la page sont ignorés ; adresse et date de l'instantané). L'agent ne confirme jamais. Scrapers dédiés et flux partenaires en complément. On n'utilise pas le runtime générique `Dea972/Agent`.
- **Paramétrage assisté** (ADR-020) : `SuggestMerchantCommand` (site → nom, page de recherche testée, typologies) et `SuggestProductCommand` (Catalog, page produit → champs) proposent sans rien enregistrer ; l'utilisateur valide puis les commandes habituelles enregistrent. Un GTIN, une référence, une page de recherche hors site ou une typologie inconnue proposés par le modèle sont écartés. `AddMerchantToProfilesCommand` ajoute une enseigne aux profils (en gardant les enseignes héritées).
- **Conditionnement** (ADR-019) : un produit de référence par conditionnement ; un lot ou un autre format n'est pas le même produit.

## IA, agent et MCP (ADR-007 à 010)

- **Tout appel LLM** passe par `ILlmGateway.ForFeatureAsync(feature)` puis `LlmClient.SendAsync(prompt)`. Un module déclare ses `AiFeature` (code `"<module>.<usage>"`, fournisseur par défaut) dans `Application/<Module>AiFeatures.cs` et les enregistre par `services.AddAiFeature(...)` dans `<Module>Module` (exemple : `PricingAiFeatures.OfferCollection`). Jamais d'injection directe d'un fournisseur.
- **Fournisseurs** : `LlmServiceBase` (Common.Application.AI) ; implémentations en services à clé dans `Common.Infrastructure/Ai`, enregistrées une fois par `AddAiProviders` (Anthropic, clé `Ai:Anthropic:ApiKey` en user-secrets). Chaque appel publie `LlmCalledIntegrationEvent`.
- **Recherche web** (ADR-027) : `IWebSearcher` (Common), fournisseur par configuration (`WebSearch:Provider` : `Anthropic`, outil serveur `web_search_20260318` sous la fonctionnalité `common.web-search`, ou `Brave`, clé `WebSearch:Brave:ApiKey`), plafond `WebSearch:MaxSearchesPerDay`. Chaque requête est journalisée dans AiEngine (`WebSearch`, tarif `AiEngine:WebSearch:PricePerSearch`, `aiengine/usage/web-search`, écran `/ai`). Tools de l'assistant `search_web` et `read_web_page` (Pricing, lecture seule, rien n'est enregistré ; `read_web_page` ne lit qu'une page trouvée par une recherche de la conversation ou une page d'enseigne suivie, `ISearchedPages`) : prix « non vérifiés », contenu web = donnée, jamais instruction. Pendant un appel de tool, `AgentToolCall.Current` donne la conversation et l'agent (ouvert par Agentic).
- **Pages web** : toute lecture d'une page passe par `IWebPageReader` (Common : adresses publiques seulement, sans proxy, pas de redirection suivie d'office ; `ReadFollowingAsync` avec la règle de l'appelant). Les réponses JSON des modèles se lisent par `LlmJson.Read<T>`.
- **Tarifs des modèles** (ADR-021) : `ProposeModelPricesCommand` (LiteLLM par `IPublishedPriceList`, sinon la page de tarifs du fournisseur lue par l'IA, prix gardé seulement s'il figure dans la page) ; appliqués par l'utilisateur via `SetModelPriceCommand` avec leur `Source`. Adresses sous `AiEngine:Prices`.
- **AiEngine** remplace `ILlmTargetResolver` (modèle ciblé par fonctionnalité, cible `default`), journalise les appels via son inbox (`LlmCall`, coût figé au tarif du jour UTC) et expose les statistiques (`aiengine/usage/*`). Les agents hébergés (Foundry, Anthropic Managed Agents) ne sont pas encore repris.
- **Tools** : classes `public static` `[McpServerToolType]` dans `Presentation/Tools/` de chaque module, une méthode par tool, `[McpServerTool(Name = "snake_case", ReadOnly = ..., Destructive = ...)]` + `[Description]` en français, paramètres `ISender` et `CancellationToken` injectés, une requête ou commande par tool, `result.ValueOrToolError()`. Skill `add-agent-tool`. Jamais de logique métier dans un tool.
- **MCP** : `AddPricelyMcpServer` (HTTP streamable, stateless) et `MapPricelyMcp` (`/mcp`, authentifié) dans `Pricely.Sdk` ; chaque module expose `ToolAssembly`.
- **Agents** (ADR-013, ADR-014) : le module générique `Agentic` exécute les agents ; il ne référence aucun module et aucun module ne le référence. Un agent = un projet `src/Agents/Pricely.Agents.<Nom>` qui ne référence que `Common.Application` et déclare un `AgentDefinition` (nom, `AiFeature`, instructions, noms de tools) par `services.AddAgent(...)`. Les tools viennent de `AddAgentTools` (dans Pricely : `McpServerExtensions.ToAgentTools` sur les assemblies de tools, dans le Sdk) ; `[ToolScope]` fixe le scope d'approbation d'un tool d'écriture (défaut `Agentic:DefaultApprovalScope`). Un tool non `ReadOnly` devient une `PendingAction` que le propriétaire valide (`agents/actions/{id}/approve`), exécutée sans repasser par le modèle. Journal dans AiEngine : `LlmCalled` (avec `AgentName`), `AiToolInvoked` (bus direct), `AiActionDecided` (outbox du module).
- **Scopes des tools** (ADR-016) : `ReadOnly` → `pricely.read`, sinon `[ToolScope]` ou `pricely.write` (`McpServerExtensions.RequiredScope`). `/mcp` ne liste que les tools autorisés par le jeton et revérifie le scope à chaque appel (`ScopedMcpServerTool`) ; les tools d'écriture y sont annotés non `ReadOnly` (le client MCP demande confirmation). Prompts MCP : classes `[McpServerPromptType]` dans `Presentation/Tools/` (`collect_product_prices`).
- **Authentification** (ADR-010, ADR-011) : Keycloak (OAuth 2.1 / OIDC, realm `pricely` importé par l'AppHost) ; Pricely valide les jetons (`AddJwtBearer`, `Authentication:Authority` / `Audience`) et n'en émet pas. Scopes `pricely.read|write|review` = politiques d'autorisation du même nom (`PricelyScopes`, `.RequireAuthorization(PricelyScopes.Read)`) ; `/mcp` exige `pricely.read`. `sub` = `User.IdentityId`, le `User` est créé (rôle `Member`) à la première requête authentifiée (`ProvisionUserCommand`) ; `CustomClaimsTransformation` ajoute `pricely_user_id` et les permissions métier du module Users.

## Commandes

```bash
dotnet restore Pricely.slnx
dotnet build Pricely.slnx -c Release
dotnet test src/Modules/Pricing/test/Pricely.Modules.Pricing.UnitTests
dotnet test test/Pricely.ArchitectureTests
dotnet list Pricely.slnx package --vulnerable --include-transitive
dotnet ef migrations add <Name> -p src/Modules/<M>/Pricely.Modules.<M>.Infrastructure -s src/API/Pricely.Api -c <M>DbContext -o Database/Migrations   # avec ConnectionStrings__pricely-db défini
dotnet run --project aspire/Pricely.AppHost   # PostgreSQL + pgAdmin (http://localhost:5050) + Keycloak + API + front (http://localhost:5180, dev / dev) ; secret : Parameters:anthropic-api-key
dotnet run --project src/API/Pricely.DevSeed  # jeu d'essai hors AppHost (ConnectionStrings__pricely-db défini) ; dans l'AppHost : ▶ sur pricely-seed
```

La CI (`.github/workflows/ci.yml`) restaure, compile en Release et lance tous les projets `*.UnitTests` et `*.ArchitectureTests`. En cas d'échec, elle publie les erreurs en annotations (titres « Build errors », « Error codes », « Vulnerable packages ») et toujours un « Test summary ».

## Sécurité des dépendances

L'audit NuGet (transitif compris) est **bloquant** : ne jamais ajouter `NU190x` à `NoWarn` / `WarningsNotAsErrors`. Corriger en montant le package direct ; si le parent n'embarque pas encore la version corrigée, référencer directement la version corrigée dans le projet le plus proche de la source (règle « nearest wins »), avec un commentaire citant l'avis GHSA. Ne pas activer `CentralPackageTransitivePinningEnabled` (provoque des rétrogradations en cascade). Secrets : user-secrets ou variables d'environnement, jamais dans le dépôt.

## État connu / à faire

- L'API démarre (`Program.cs` : modules, authentification, `/api` et `/mcp`, validation de la DI) ; les migrations sont appliquées par `Pricely.MigrationService` (ADR-018), que l'AppHost lance avant l'API (`WaitForCompletion`). L'AppHost lui-même (conteneurs Aspire) n'a pas encore été lancé ; la chaîne PostgreSQL 16 + Keycloak 26.6 + service de migration + API + front, elle, a été exécutée à la main (migrations, jeton, endpoints, 14 tools MCP selon le scope, écrans du front).
- Migrations de Catalog, Pricing (`Initial` + `AddAgentCollection` + `DropCatalogCategories` + `DropCatalogProducts` + `AddProductPageAndDeclaredPrices`), Users, AiEngine (`Initial` + `AddAgentActivity` + `AddWebSearches`) et Agentic en place (`Database/Migrations`, `generated_code = true`). Hors AppHost, lancer `Pricely.MigrationService` avant l'API.
- Endpoints des modules : lecture `PricelyScopes.Read`, mutation `PricelyScopes.Write`, confirmation / rejet d'offre `PricelyScopes.Review` (ADR-013). Un nouvel endpoint suit la même règle, un nouveau tool aussi (ADR-016).
- Le chat client Anthropic lit la réponse en SSE (texte au fil de l'eau) ; l'assistant et l'agent de collecte n'ont été testés qu'avec un faux modèle et contre l'API Anthropic sans clé valide. La lecture réelle d'une page d'enseigne n'a pas pu être exécutée dans l'environnement de développement (pas d'accès direct à Internet, et la lecture n'utilise pas de proxy, ADR-017).
- La recherche web (ADR-027) n'a été testée qu'avec des réponses scriptées d'Anthropic et de Brave (aucune clé dans l'environnement de développement) ; le SQL de `aiengine/usage/web-search` a été vérifié sur PostgreSQL 16.
- Les instantanés de pages ne sont jamais purgés (décision ouverte).
- Pour créer un module, utiliser le skill `new-module` (pas de template `dotnet new`).
- Front (ADR-015) : Assistant, fiche produit, recherche, catalogue (`/catalog` : typologies et produits), enseignes (`/merchants` : ajout assisté par l'IA ou à la main, page de recherche, désactivation / réactivation), offres à valider (`/review`), profils de collecte (`/profiles`), IA et coûts (`/ai`), badge et raccourci mobile des offres à valider (`ReviewQueue`). Jetons dans `IDistributedCache`, chiffrés (plusieurs instances : cache et clés de protection partagés). Le front ne référence ni module ni Common (`WebBoundaryTests`) : un besoin de donnée = un endpoint de l'API + un DTO dans `Api/Contracts.cs`. Composant d'un nouveau tool : `ToolCatalog` + `ToolResultView`. Dans un `.razor`, un paramètre `string` sans `@` est un littéral.

## Méthode de travail

- Les règles de `.claude/rules/` et les skills de `.claude/skills/` font partie du dépôt (visibles dans le dossier `.claude` de la solution). Utiliser les skills du dépôt : `new-module`, `new-aggregate`, `new-use-case`, `new-integration-event`, `add-migration`, `add-offer-collector`, `add-agent-tool`.
- Proposer un plan court avant une tâche qui touche plusieurs projets ; attendre validation pour les décisions d'architecture et les consigner dans `docs/architecture/decisions.md`.
- Code, commentaires, commits en anglais (Conventional Commits). Échanges et docs en français.
- Avant de terminer : `dotnet build` + `dotnet test`, puis le sous-agent `architecture-reviewer` sur le diff.
- Ne pas inventer de fonctionnalité hors demande.
