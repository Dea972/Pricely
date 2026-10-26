---
name: new-module
description: Create a new Pricely module (Domain, Application, Infrastructure, Presentation, IntegrationEvents, unit and architecture tests) wired into the solution, the SDK and the CI. Use when the user asks for a new module or bounded context.
---

# Nouveau module

Référence à copier : `src/Modules/Catalog` (module simple, publie des événements) ou `src/Modules/Pricing` (consomme des événements via l'inbox).

Avant de créer un module, vérifier que c'est bien un **bounded context** (langage et invariants propres). Une intégration technique (scraping, flux, API externe) n'est pas un module : c'est un port dans `Application` et un adaptateur dans `Infrastructure` (voir `docs/architecture/decisions.md`, ADR-001). En cas de doute, demander.

## Étapes

Soit `<M>` le nom du module (PascalCase, pluriel ou nom de contexte : `Alerts`, `Search`).

1. **Projets** sous `src/Modules/<M>/`, csproj minimalistes (le `Directory.Build.props` fixe `net10.0`, nullable, analyseurs) :
   - `Pricely.Modules.<M>.Domain` → `Common.Domain`
   - `Pricely.Modules.<M>.IntegrationEvents` → `Common.Application` (seulement si le module publie)
   - `Pricely.Modules.<M>.Application` → Domain, IntegrationEvents (si publie), `Common.Application`
   - `Pricely.Modules.<M>.Presentation` → Application, `Common.Presentation` (+ `<Autre>.IntegrationEvents` s'il consomme)
   - `Pricely.Modules.<M>.Infrastructure` → Application, Presentation, IntegrationEvents, `Common.Infrastructure`, package `Npgsql.EntityFrameworkCore.PostgreSQL`
2. **Fichiers de base** :
   - `Application/AssemblyReference.cs`, `Presentation/AssemblyReference.cs`, `Presentation/Tags.cs`
   - `Application/Abstractions/Data/IUnitOfWork.cs`
   - `Infrastructure/Database/Schemas.cs` (`internal const string <M> = "<m>";` en minuscules)
   - `Infrastructure/Database/<M>DbContext.cs` : `HasDefaultSchema`, configurations outbox (`OutboxMessage`, `OutboxMessageConsumer`) et inbox (`InboxMessage`, `InboxMessageConsumer`), puis celles du module. Implémente `IUnitOfWork`.
   - `Infrastructure/Outbox/*` : copier depuis `Catalog.Infrastructure/Outbox` en remplaçant le namespace, `Schemas.Catalog` et `ModuleName`.
   - Si le module consomme des événements : copier aussi `Pricing.Infrastructure/Inbox/*`.
   - `Infrastructure/<M>Module.cs` : modèle `CatalogModule.cs` / `PricingModule.cs` (`HandlerAssemblies`, `Add<M>Module`, `AddDbContext` `UseNpgsql` + snake_case + `InsertOutboxMessagesInterceptor` + historique de migrations dans le schéma, repositories, `AddDomainEventHandlers`, `AddIntegrationEventHandlers` si consommateur, `AddEndpoints`, options Outbox / Inbox, `ConfigureConsumers` si consommateur).
3. **Composition** :
   - `src/Adapters/Pricely.Sdk/Pricely.Sdk.csproj` : référence vers `<M>.Infrastructure`.
   - `ServiceCollectionExtensions.AddPricelyAsync` : ajouter `<M>Module.HandlerAssemblies` à `AddApplication`, `<M>Module.ConfigureConsumers` à `AddInfrastructure` si consommateur, `services.Add<M>Module(configuration, connectionString)`.
   - `src/API/Pricely.Api/appsettings.json` : section `<M>` (`Outbox`, `Inbox` : `IntervalInSeconds`, `BatchSize`).
4. **Tests** sous `src/Modules/<M>/test/` :
   - `Pricely.Modules.<M>.ArchitectureTests` : copier ceux de Catalog, adapter `Abstractions/BaseTest.cs` (un type de chaque assembly).
   - `Pricely.Modules.<M>.UnitTests` : copier csproj et `Abstractions/BaseTest.cs` de Catalog.
   - `test/Pricely.ArchitectureTests/ModuleBoundaryTests.cs` : ajouter les assemblies du module et les assertions de frontière dans les deux sens ; référencer `<M>.Infrastructure` dans le csproj.
5. **Solution** : ajouter les projets à `Pricely.slnx` (dossiers `/src/Modules/<M>/` et `/src/Modules/<M>/test/`). La CI découvre seule les projets `*.UnitTests` / `*.ArchitectureTests`.
6. **Docs** : ligne dans le tableau des modules de `CLAUDE.md` et `README.md`, termes dans `docs/architecture/ubiquitous-language.md`.
7. `dotnet build Pricely.slnx` puis `dotnet test` sur les projets de test du module et `test/Pricely.ArchitectureTests`.

## Vérifications

- Aucun `ProjectReference` vers un autre module hors `*.IntegrationEvents`.
- Toutes les classes `internal sealed` sauf `AssemblyReference`, `<M>Module`, `<M>DbContext`, contrats d'Application et types du domaine.
