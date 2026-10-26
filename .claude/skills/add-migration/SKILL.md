---
name: add-migration
description: Create or apply an EF Core migration for one Pricely module DbContext (one schema and one migrations history table per module). Use after changing an entity configuration or adding an aggregate.
---

# Migration EF Core par module

Chaque module a son `DbContext` (`CatalogDbContext`, `PricingDbContext`, `UsersDbContext`, `AiEngineDbContext`), son schéma (`catalog`, `pricing`, `users`, `aiengine`) et sa table `__EFMigrationsHistory` dans ce schéma (`MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.X)`). Les noms SQL sont en snake_case (`UseSnakeCaseNamingConvention`).

## Prérequis

L'option A du choix initial est en place (ADR-011) : `dotnet ef` construit l'hôte `Pricely.Api` (`Program.cs` + `Microsoft.EntityFrameworkCore.Design`). Il faut définir une chaîne de connexion PostgreSQL, même factice, rien ne s'y connecte pour ajouter une migration :

```bash
export "ConnectionStrings__pricely-db=Host=localhost;Database=pricely-db;Username=postgres;Password=x"
```

Dans un shell qui refuse les tirets dans les noms de variables, passer par `env "ConnectionStrings__pricely-db=..." dotnet ef ...`.

Outil : `dotnet tool install --global dotnet-ef --version 10.0.2` (aligné sur EF Core).

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
- Chaque dossier `Database/Migrations/` contient un `.editorconfig` avec `generated_code = true` (sans lui, les analyseurs stricts font échouer le build). Pour un nouveau module, le créer **avant** la première migration.

## Appliquer

```bash
dotnet ef database update -p src/Modules/<M>/Pricely.Modules.<M>.Infrastructure -s src/API/Pricely.Api -c <M>DbContext
```

Localement, l'AppHost Aspire démarre PostgreSQL (`pricely-db`) et l'API applique elle-même les migrations des quatre modules au démarrage, en Development uniquement (`MigrationExtensions.ApplyMigrationsAsync` dans `Pricely.Sdk`). Un nouveau module doit y être ajouté.

## Requêtes Dapper

Après une migration qui renomme une colonne ou une table, mettre à jour le SQL des query handlers Dapper du module (recherche du nom snake_case) : le compilateur ne le verra pas.
