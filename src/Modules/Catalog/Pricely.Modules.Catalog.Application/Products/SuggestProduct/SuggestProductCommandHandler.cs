using System.Globalization;
using System.Text;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Application.Web;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Domain.Categories;
using Pricely.Modules.Catalog.Domain.Products;

namespace Pricely.Modules.Catalog.Application.Products.SuggestProduct;

/// <summary>
/// Reads the product page and lets the model of <see cref="CatalogAiFeatures.ProductEntry"/> fill the fields. What it
/// proposes is checked: a GTIN must be valid and shown by the page, a manufacturer reference shown by the page, a typology
/// known. A product with the same GTIN is pointed out instead of a duplicate.
/// </summary>
internal sealed class SuggestProductCommandHandler(
    IWebPageReader webPageReader,
    ILlmGateway llmGateway,
    IProductCategoryRepository productCategoryRepository,
    IProductRepository productRepository)
    : ICommandHandler<SuggestProductCommand, ProductSuggestionResponse>
{
    private const int PageTextLength = 15_000;

    private static readonly int[] GtinLengths = [8, 12, 13, 14];

    private const string Instructions = """
        Tu aides à saisir un produit de référence dans Pricely, un comparateur de prix. On te donne le texte d'une page
        produit (fabricant ou enseigne) et les typologies de Pricely. Réponds uniquement par un objet JSON :
        {"name": "désignation du produit, conditionnement compris (ex. « Ciment CEM II 32,5 R — sac 35 kg »)",
         "brand": "marque du produit (pas l'enseigne qui le vend)",
         "gtin": "GTIN / EAN tel qu'affiché, ou null",
         "manufacturerReference": "référence du fabricant telle qu'affichée, ou null (pas la référence de l'enseigne)",
         "categoryId": "identifiant de la typologie de Pricely la plus précise qui convient, ou null",
         "packaging": "conditionnement (sac 35 kg, lot de 10, 2,5 L...), ou null",
         "rationale": "une phrase pour l'utilisateur : ce qui est sûr, ce qui est à vérifier"}
        Règles : recopie le GTIN et la référence tels que la page les montre, n'en invente jamais ; un produit par
        conditionnement ; ne cite qu'un identifiant de typologie fourni.
        """;

    public async Task<Result<ProductSuggestionResponse>> Handle(SuggestProductCommand request, CancellationToken cancellationToken)
    {
        Result<WebPage> page = await webPageReader.ReadFollowingAsync(
            request.ProductPage,
            next => next.Scheme is "http" or "https",
            cancellationToken);
        if (page.IsFailure)
        {
            return Result.Failure<ProductSuggestionResponse>(page.Error);
        }

        IReadOnlyCollection<ProductCategory> categories = await productCategoryRepository.GetAllAsync(cancellationToken);
        ProductEntryReply? reply = await AskAsync(page.Value, categories, cancellationToken);

        string? gtin = ShownGtin(reply?.Gtin, page.Value.Text);
        Product? existing = gtin is null ? null : await FindByGtinAsync(gtin, cancellationToken);
        Guid? categoryId = Guid.TryParse(reply?.CategoryId, out Guid id) && categories.Any(c => c.Id == id) ? id : null;

        return new ProductSuggestionResponse(
            page.Value.Address,
            Cut(reply?.Name ?? page.Value.Title, Product.NameMaxLength),
            Cut(reply?.Brand, Product.BrandMaxLength),
            gtin,
            IsShown(reply?.ManufacturerReference, page.Value.Text) ? Cut(reply!.ManufacturerReference, Product.ManufacturerReferenceMaxLength) : null,
            categoryId,
            Cut(reply?.Packaging, 100),
            reply?.Rationale?.Trim(),
            existing?.Id,
            existing?.Name,
            IsAiAnswer: reply is not null);
    }

    private async Task<ProductEntryReply?> AskAsync(WebPage page, IReadOnlyCollection<ProductCategory> categories, CancellationToken cancellationToken)
    {
        var message = new StringBuilder();
        message.AppendLine(CultureInfo.InvariantCulture, $"Page : {page.Address}")
            .AppendLine(CultureInfo.InvariantCulture, $"Titre : {page.Title ?? "aucun"}")
            .AppendLine("Typologies de Pricely (identifiant : chemin) :");
        foreach (ProductCategory category in categories)
        {
            message.AppendLine(CultureInfo.InvariantCulture, $"- {category.Id} : {Path(category, categories)}");
        }

        message.AppendLine().AppendLine("Texte de la page :")
            .AppendLine(page.Text.Length > PageTextLength ? page.Text[..PageTextLength] : page.Text);

        LlmClient llm = await llmGateway.ForFeatureAsync(CatalogAiFeatures.ProductEntry, cancellationToken);
        Result<LlmResponse> response = await llm.SendAsync(
            new LlmPrompt
            {
                SystemPrompt = Instructions,
                Message = message.ToString(),
                RequireJsonResponse = true,
                SessionId = $"product-entry:{Guid.CreateVersion7()}"
            },
            cancellationToken);

        return response.IsSuccess ? LlmJson.Read<ProductEntryReply>(response.Value.Content) : null;
    }

    /// <summary>The same number written on 8, 12, 13 or 14 digits (leading zeros) is the same GTIN.</summary>
    private async Task<Product?> FindByGtinAsync(string gtin, CancellationToken cancellationToken)
    {
        string significant = gtin.TrimStart('0');
        foreach (int length in GtinLengths.Where(l => l >= significant.Length))
        {
            Result<Gtin> written = Gtin.Create(significant.PadLeft(length, '0'));
            if (written.IsSuccess && await productRepository.FindByGtinAsync(written.Value, cancellationToken) is Product product)
            {
                return product;
            }
        }

        return null;
    }

    /// <summary>A valid GTIN whose digits the page shows (with or without its leading zeros).</summary>
    private static string? ShownGtin(string? proposed, string pageText)
    {
        string digits = string.Concat((proposed ?? string.Empty).Where(char.IsAsciiDigit));
        if (digits.Length == 0 || Gtin.Create(digits).IsFailure)
        {
            return null;
        }

        string pageDigits = string.Concat(pageText.Where(c => char.IsAsciiDigit(c) || c == '\n'));

        return pageDigits.Contains(digits.TrimStart('0'), StringComparison.Ordinal) ? digits : null;
    }

    /// <summary>Compared letters and digits only, upper-cased (« CR-325R » is shown by « cr 325 r »).</summary>
    private static bool IsShown(string? value, string pageText)
    {
        string expected = Normalize(value);

        return expected.Length > 0 && Normalize(pageText).Contains(expected, StringComparison.Ordinal);
    }

    private static string Normalize(string? value) =>
        string.Concat((value ?? string.Empty).Where(char.IsAsciiLetterOrDigit).Select(char.ToUpperInvariant));

    private static string? Cut(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();

        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    private static string Path(ProductCategory category, IReadOnlyCollection<ProductCategory> categories)
    {
        var names = new List<string> { category.Name };
        ProductCategory? current = category;

        while (current?.ParentId is Guid parentId && names.Count < ProductCategory.MaxDepth)
        {
            current = categories.FirstOrDefault(c => c.Id == parentId);
            if (current is not null)
            {
                names.Insert(0, current.Name);
            }
        }

        return string.Join(" › ", names);
    }
}
