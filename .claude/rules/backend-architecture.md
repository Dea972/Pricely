---
paths:
  - "src/Modules/**"
  - "src/Common/**"
  - "src/Adapters/**"
  - "src/API/**"
  - "src/Agents/**"
  - "aspire/**"
  - "test/**"
---

# Backend — architecture modulaire

Référence détaillée : `CLAUDE.md` (règles non négociables, conventions, pièges), `docs/architecture/decisions.md` (ADR), `docs/architecture/model.md`.
En cas de désaccord entre ce fichier et `CLAUDE.md`, `CLAUDE.md` fait foi. Les tests `ArchitectureTests` font foi sur les deux.

## Couches d'un module

Chaque module `src/Modules/<M>/` = projets `Pricely.Modules.<M>.{Domain,Application,Infrastructure,Presentation}`, un `.IntegrationEvents` optionnel (ce que le module publie), un `.Contracts` optionnel (surface publique de lecture, ADR-022) et un dossier `test/`.

| Couche | Peut référencer | Ne doit pas référencer |
|---|---|---|
| Domain | `Common.Domain` | tout le reste (ni EF Core, MassTransit, ASP.NET, Dapper) |
| Application | `Common.Application`, son Domain, son IntegrationEvents, le `.Contracts` d'un autre module | Infrastructure, Presentation, **EF Core** |
| Presentation | `Common.Presentation`, son Application, `<Autre>.IntegrationEvents` (handlers d'événements entrants) | Infrastructure, Domain en direct |
| Infrastructure | `Common.Infrastructure`, son Application, sa Presentation, son IntegrationEvents, son `.Contracts` (pour l'implémenter), `<Autre>.IntegrationEvents` | l'Infrastructure d'un autre module |
| Contracts | `Common.Domain` (pour `Result`) | tout le reste |
| IntegrationEvents | `Common.Application` | tout le reste |

Infrastructure → Presentation est voulu : l'Infrastructure scanne les `IEndpoint` et les consumers. La Presentation ne référence jamais l'Infrastructure.

## CQRS : où vit chaque handler

- **Commandes ET requêtes : le handler est toujours dans `Application`** (`internal sealed`), à côté de sa commande / requête : `Application/<Agrégats>/<UseCase>/`. Jamais de `IQueryHandler` dans l'Infrastructure (test `QueryHandlerTests`).
- Une **commande** passe par les dépôts du Domain (`IRepository` + `IUnitOfWork`) et les agrégats.
- Une **requête** lit sans charger d'agrégat : Dapper sur `IDbConnectionFactory` (défini dans `Common.Application`), SQL PostgreSQL, schéma explicite (`pricing.offers`), colonnes snake_case aliasées par `nameof(XResponse.Prop)`, `new CommandDefinition(sql, param, cancellationToken: ct)`. Si elle a besoin d'une règle du domaine, elle l'appelle (fonction pure du Domain) sur les lignes lues.
- Pas d'EF Core dans l'Application : l'écriture passe par les dépôts, implémentés dans l'Infrastructure.
- L'Infrastructure n'implémente que des **ports** (dépôts, `IOfferCollector`, `IWebPageReader`, lecteurs de contrat, jobs, outbox / inbox).

## Isolation des modules

- **Aucune `ProjectReference` entre modules**, sauf vers `<Autre>.IntegrationEvents` (Presentation et Infrastructure du module qui réagit) et `<Autre>.Contracts` (Application du module qui lit).
- Pas de FK inter-modules, pas de `DbContext` partagé : **un `DbContext` et un schéma SQL par module**. Une référence cross-module est un `Guid` simple, commenté.
- Communication : `DomainEventHandler` → `IEventBus` publie un `IntegrationEvent` → outbox du module émetteur → MassTransit → inbox idempotente du consommateur.
- `IntegrationEvent` : nommé `<Sujet><Action>IntegrationEvent`, primitifs uniquement, contrat stable (ajouter des champs, ne jamais renommer ni retirer), léger, identifiant = `domainEvent.Id`.

## Contrats de module (`<M>.Contracts`)

Décision : ADR-022.

| Besoin | Mécanisme |
|---|---|
| Lire ponctuellement la donnée d'un autre module | son **contrat** |
| Réagir à un changement dans un autre module | **IntegrationEvent** (outbox / inbox), qui peut ne porter que des identifiants |
| Garder une copie locale | IntegrationEvent + read model, **à justifier** (lecture très fréquente, historisation, disponibilité) |

Jamais d'interrogation en boucle d'un contrat pour détecter un changement : la réaction passe par un événement.

- **Contenu** : interfaces, `sealed record` immuables et erreurs statiques ; un dossier par sujet (`Categories/`). Aucun type du Domain, aucun `IQueryable`, aucune entité EF.
- **Méthodes** : `async`, `Task<Result<T>>`, `CancellationToken` en dernier, volumes bornés (pagination par curseur plutôt qu'`IAsyncEnumerable`) : le contrat doit pouvoir être servi demain par HTTP.
- **Lecture seule** : aucune écriture, aucun effet de bord.
- **Implémentation** : `internal sealed` dans `<M>.Infrastructure/Contracts/<Sujet>/`, enregistrée dans `<M>Module`, EF Core `AsNoTracking` + projection `Select` sur le `DbContext` du module. **Pas de requête MediatR** (une requête porte une intention métier, un contrat est une lecture technique), aucune règle métier dans la requête, aucune autorisation (appel interne de confiance, jamais exposé tel quel par un endpoint).
- Test d'intégration à prévoir dans le projet de tests du module propriétaire (décision ouverte, voir `decisions.md`).

## Hôtes et DI

- Chaque module expose `Add<Module>Module(services, configuration, connectionString)` dans `Infrastructure/<Module>Module.cs` ; il est enregistré dans `Pricely.Sdk/ServiceCollectionExtensions.cs` (+ `MigrationExtensions.cs` pour les migrations).
- API = hôte mince : minimal APIs `IEndpoint`, mappées sous `/api`. L'API n'applique aucune migration (`Pricely.MigrationService`, ADR-018).
- Les tools MCP / agents sont des classes de `Presentation/Tools/` sans logique métier (skill `add-agent-tool`).
- Config par module dans `appsettings.json` sous `<Module>:Outbox|Inbox|...` ; secrets en user-secrets ou variables d'environnement.
- NuGet : Central Package Management (`Directory.Packages.props`), jamais de version dans un `.csproj`.

## Avant de coder

- La feature appartient-elle vraiment à ce module ? (règle de collecte dans Catalog → Pricing ?)
- Un pattern équivalent existe-t-il déjà dans un autre module ? Le copier plutôt que le réinventer.
- Le build échoue sur le moindre avertissement : voir « Analyseurs : pièges connus » dans `CLAUDE.md`.
