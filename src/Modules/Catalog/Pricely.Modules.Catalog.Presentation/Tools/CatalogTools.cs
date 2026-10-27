using System.ComponentModel;
using MediatR;
using ModelContextProtocol.Server;
using Pricely.Common.Presentation.Mcp;
using Pricely.Modules.Catalog.Application.Categories.GetProductCategories;
using Pricely.Modules.Catalog.Application.Products.FindOrRegisterProduct;
using Pricely.Modules.Catalog.Application.Products.GetProduct;
using Pricely.Modules.Catalog.Application.Products.SearchProducts;

namespace Pricely.Modules.Catalog.Presentation.Tools;

/// <summary>
/// Catalog tools for AI agents (MCP and internal agent). Thin: one query or command each, no business logic. Write tools
/// become, for the internal agent, actions the user approves, in one batch per turn (ADR-028).
/// </summary>
[McpServerToolType]
public static class CatalogTools
{
    [McpServerTool(Name = "get_category_tree", Title = "Typologies de produits", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("""
        Liste les typologies de produits (arbre de 3 niveaux maximum : racine, sous-typologie, sous-sous-typologie).
        Chaque typologie a un identifiant, un nom, son parent (null pour une racine) et sa profondeur (1 à 3).
        Utilisez ces identifiants pour filtrer search_products ou consulter le plan de collecte d'une typologie.
        """)]
    public static async Task<IReadOnlyCollection<ProductCategoryResponse>> GetCategoryTree(
        ISender sender,
        CancellationToken cancellationToken) =>
        (await sender.Send(new GetProductCategoriesQuery(), cancellationToken)).ValueOrToolError();

    [McpServerTool(Name = "search_products", Title = "Recherche de produits", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("""
        Recherche des produits de référence par texte (nom, marque, référence fabricant ; GTIN exact) et/ou typologie.
        Retourne identifiant, nom, marque, GTIN, référence fabricant et typologie. Utilisez l'identifiant avec
        get_product, compare_prices ou get_price_history.
        """)]
    public static async Task<IReadOnlyCollection<ProductResponse>> SearchProducts(
        ISender sender,
        [Description("Texte recherché : nom, marque, référence fabricant ou GTIN exact.")] string? text = null,
        [Description("Identifiant d'une typologie (ses sous-typologies ne sont pas incluses).")] Guid? productCategoryId = null,
        [Description("Nombre maximum de résultats (1 à 50, défaut 20).")] int limit = 20,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(
            new SearchProductsQuery(text, productCategoryId, Math.Clamp(limit, 1, SearchProductsQuery.MaxLimit)),
            cancellationToken)).ValueOrToolError();

    [McpServerTool(Name = "get_product", Title = "Fiche produit", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Retourne un produit de référence : nom, marque, GTIN, référence fabricant et typologie.")]
    public static async Task<ProductResponse> GetProduct(
        ISender sender,
        [Description("Identifiant du produit.")] Guid productId,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(new GetProductQuery(productId), cancellationToken)).ValueOrToolError();

    [McpServerTool(Name = "find_or_add_product", Title = "Trouver ou ajouter un produit", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("""
        Trouve le produit de référence par GTIN (ou marque et référence fabricant) ; sinon l'ajoute au catalogue dans la
        typologie donnée par son chemin (1 à 3 noms depuis la racine, les niveaux manquants sont créés). Un produit par
        conditionnement : un lot ou un autre format est un autre produit. Utiliser les informations lues sur une page
        (read_web_page), jamais inventées. Toutes les actions proposées dans une même réponse forment un lot que
        l'utilisateur valide en une fois.
        """)]
    public static async Task<FoundProductResponse> FindOrAddProduct(
        ISender sender,
        [Description("Nom du produit, conditionnement compris (ex. « Ciment CEM II 35 kg »).")] string name,
        [Description("Marque.")] string brand,
        [Description("Chemin de la typologie depuis la racine, 1 à 3 noms (ex. [\"Gros œuvre\", \"Ciment\"]). Reprendre les typologies existantes (get_category_tree) quand elles conviennent.")] string[] categoryPath,
        [Description("GTIN (EAN) lu sur la page, s'il y en a un.")] string? gtin = null,
        [Description("Référence fabricant lue sur la page, s'il y en a une.")] string? manufacturerReference = null,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(new FindOrRegisterProductCommand(name, brand, gtin, manufacturerReference, categoryPath), cancellationToken)).ValueOrToolError();
}
