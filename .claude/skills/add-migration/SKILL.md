---
name: add-migration
description: Create or apply an EF Core migration for one Pricely module DbContext (one schema and one migrations history table per module). Use after changing an entity configuration or adding an aggregate.
---

# Migration EF Core par module

Chaque module a son `DbContext` (`CatalogDbContext`, `PricingDbContext`, `UsersDbContext`), son schéma (`catalog`, `pricing`, `users`) et sa table `__EFMigrationsHistory` dans ce schéma (`MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.X)`). Les noms SQL sont en snake_case (`UseSnakeCaseNamingConvention`).

## Prérequis (à faire une seule fois, pas encore en place)

Aucune migration n'existe encore et `dotnet ef` ne peut pas construire les contextes tant que l'un des deux points suivants n'est pas fait. Proposer le choix à l'utilisateur avant de le faire :

- **Option A (recommandée)** : `Program.cs` appelle `builder.Services.AddPricelyAsync(connectionString, builder.Configuration)` et `app.MapPricelyEndpoints()`, et `Pricely.Api` référence `Microsoft.EntityFrameworkCore.Design` (`PrivateAssets="all"`, version à ajouter dans `Directory.Packages.props`, alignée sur EF Core). Attention : le `DbContext` Users est commenté dans `UsersModule` et doit être enregistré.
- **Option B** : une `IDesignTimeDbContextFactory<XDbContext>` `internal sealed` par module dans `Infrastructure/Database/`, qui lit `ConnectionStrings:Database` (variable d'environnement).

Outil : `dotnet tool install --global dotnet-ef` (version alignée sur EF Core 10).

## Créer une migration

```bash
dotnet ef migrations add <Name> \
  -p src/Modules/<M>/Pricely.Modules.<M>.Infrastructure \
  -s src/API/Pricely.Api \
  -c <M>DbContext \
  -o Database/Migrations
```

- `<Name>` en PascalCase, descriptif : `Initial`, `AddOfferConfirmationMethod`.
- Une migration = un module. Ne jamais générer une migration qui touche le schéma d'un autre module.
- Relire le fichier généré : schéma correct, pas de clé étrangère vers un autre schéma, colonnes `ComplexProperty` aux noms attendus (`price_amount`, `evidence_observed_gtin`...), index uniques présents.
- Les migrations générées passent par les analyseurs stricts. Si elles font échouer le build, ajouter un `.editorconfig` dans `Database/Migrations/` avec `generated_code = true` plutôt que de désactiver des règles globalement.

## Appliquer

```bash
dotnet ef database update -p src/Modules/<M>/Pricely.Modules.<M>.Infrastructure -s src/API/Pricely.Api -c <M>DbContext
```

Localement, l'AppHost Aspire démarre SQL Server (`pricely-db`). L'application automatique au démarrage (`MigrationExtensions` dans `Pricely.Sdk`) est à vérifier avant d'être utilisée.

## Requêtes Dapper

Après une migration qui renomme une colonne ou une table, mettre à jour le SQL des query handlers Dapper du module (recherche du nom snake_case) : le compilateur ne le verra pas.
