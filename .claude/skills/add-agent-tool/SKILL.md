---
name: add-agent-tool
description: Add a tool usable by AI agents (MCP clients such as Claude, and the internal Pricely agent) on top of an existing query or command of a module. Use when exposing a capability to agents or MCP.
---

# Nouveau tool d'agent

Références : `Catalog.Presentation/Tools/CatalogTools.cs`, `Pricing.Presentation/Tools/PricingTools.cs`, test `test/Pricely.ArchitectureTests/McpToolTests.cs`. Décisions : ADR-008, ADR-009.

Un tool = **une tâche pour le LLM**, pas un endpoint recopié. Il est fin : une requête ou une commande via `ISender`, aucune logique métier. S'il manque une requête, la créer d'abord (skill `new-use-case`).

## Écrire le tool

Dans `Pricely.Modules.<M>.Presentation/Tools/<M>Tools.cs` (classe `public static`, `[McpServerToolType]`, découverte automatiquement via `<M>Module.ToolAssembly`) :

```csharp
[McpServerTool(Name = "compare_prices", Title = "Comparatif de prix", ReadOnly = true, Idempotent = true, OpenWorld = false)]
[Description("""
    Ce que fait le tool, ce qu'il retourne, quand l'utiliser et avec quel autre tool l'enchaîner.
    """)]
public static async Task<IReadOnlyCollection<MerchantPriceResponse>> ComparePrices(
    ISender sender,
    [Description("Identifiant du produit (voir search_products).")] Guid productId,
    CancellationToken cancellationToken = default) =>
    (await sender.Send(new GetProductPriceComparisonQuery(productId), cancellationToken)).ValueOrToolError();
```

- **Nom** en `snake_case`, verbe d'abord (`list_`, `get_`, `search_`, `compare_`, `submit_`, `confirm_`).
- **Descriptions en français**, orientées agent : ce que contient la réponse et le tool suivant à appeler. Chaque paramètre a sa `[Description]` (unités, bornes, UTC).
- **Annotations** : lecture → `ReadOnly = true, Idempotent = true`. Écriture → `ReadOnly = false`, `Destructive = true` si l'action est irréversible ou change une décision (confirmer / rejeter une offre). Les tools d'écriture seront soumis à approbation humaine par l'agent interne (ADR-008) ; ne jamais contourner une règle du domaine (une offre n'est jamais confirmée par l'agent seul).
- **Services injectés** : `ISender`, `CancellationToken` (et tout service DI) ne sont pas des arguments du tool. Ne pas injecter de repository ni de `DbContext`.
- **Bornes** : `Math.Clamp` sur les limites, valeurs par défaut raisonnables ; les requêtes ont leur propre validateur.
- **Erreurs** : `result.ValueOrToolError()` transforme un `Result` en erreur de tool lisible (`code: description`). Pas d'exception métier.
- **Retour** : types de réponse existants (records / classes), sérialisés en JSON. Éviter les gros volumes : paginer ou limiter.

## Vérifier

- Ajouter le nom du tool à `ExpectedTools` dans `McpToolTests` (et adapter l'assertion `ReadOnlyHint` si c'est un tool d'écriture).
- `dotnet test test/Pricely.ArchitectureTests`.
- Mettre à jour la liste des tools dans `README.md`.
