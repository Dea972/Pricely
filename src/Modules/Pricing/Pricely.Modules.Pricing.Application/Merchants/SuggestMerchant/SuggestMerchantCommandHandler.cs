using System.Globalization;
using System.Text;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Application.Web;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Domain.Catalog;
using Pricely.Modules.Pricing.Domain.Merchants;

namespace Pricely.Modules.Pricing.Application.Merchants.SuggestMerchant;

/// <summary>
/// Reads the home page of the site, lets the model of <see cref="PricingAiFeatures.MerchantSetup"/> propose the name,
/// the search page and the typologies, then tests the search page with a real search. What the model proposes is checked:
/// a search page outside the site is dropped, an unknown typology too. Without the model (failure, no key), the proposal
/// comes from the site alone (title, first declared search page).
/// </summary>
internal sealed class SuggestMerchantCommandHandler(
    IWebPageReader webPageReader,
    ILlmGateway llmGateway,
    ICatalogCategoryRepository catalogCategoryRepository)
    : ICommandHandler<SuggestMerchantCommand, MerchantSuggestionResponse>
{
    private const int HomePageTextLength = 15_000;
    private const int MinResultPageLength = 200;
    private const string DefaultTestQuery = "produit";

    private const string Instructions = """
        Tu aides à configurer une enseigne dans Pricely, un comparateur de prix multi-enseignes. On te donne la page
        d'accueil de son site, les pages de recherche que le site déclare et les typologies de produits de Pricely.
        Réponds uniquement par un objet JSON :
        {"name": "nom commercial de l'enseigne",
         "searchTemplate": "adresse absolue de la page de recherche du site avec {query} à la place du texte cherché, ou null",
         "testQuery": "un mot ou deux qu'on trouverait certainement dans les résultats de recherche de ce site",
         "categoryIds": ["identifiants des typologies de Pricely que l'enseigne semble vendre"],
         "categoryReasons": {"identifiant": "raison courte"},
         "rationale": "deux phrases au plus pour l'utilisateur : ce que vend l'enseigne et d'où vient la page de recherche"}
        Règles : la page de recherche est sur le site de l'enseigne ; préfère une page de recherche déclarée par le site ;
        n'invente pas d'adresse que rien n'indique ; ne cite que des identifiants de typologie fournis.
        """;

    public async Task<Result<MerchantSuggestionResponse>> Handle(SuggestMerchantCommand request, CancellationToken cancellationToken)
    {
        if (ParseSite(request.Website) is not Uri requested)
        {
            return Result.Failure<MerchantSuggestionResponse>(MerchantErrors.InvalidWebsite(request.Website));
        }

        Result<WebPage> home = await webPageReader.ReadFollowingAsync(requested, IsWeb, cancellationToken);
        if (home.IsFailure)
        {
            return Result.Failure<MerchantSuggestionResponse>(home.Error);
        }

        var website = new Uri(home.Value.Address.GetLeftPart(UriPartial.Authority));
        string[] candidates = [.. home.Value.SearchTemplates.Where(t => IsOnSite(website, t))];
        IReadOnlyCollection<CatalogCategory> categories = await catalogCategoryRepository.GetAllAsync(cancellationToken);

        AdvisorReply? reply = await AskAsync(request, website, home.Value, candidates, categories, cancellationToken);

        string? template = reply?.SearchTemplate is string proposed && IsOnSite(website, proposed) ? proposed : candidates.FirstOrDefault();
        string query = string.IsNullOrWhiteSpace(reply?.TestQuery) ? DefaultTestQuery : reply.TestQuery.Trim();
        SearchCheckResponse? check = template is null ? null : await CheckSearchAsync(website, template, query, cancellationToken);

        return new MerchantSuggestionResponse(
            ChooseName(request.Name, reply?.Name, home.Value.Title, website),
            website,
            template,
            candidates,
            check,
            Categories(reply, categories),
            reply?.Rationale?.Trim(),
            IsAiAnswer: reply is not null);
    }

    private async Task<AdvisorReply?> AskAsync(
        SuggestMerchantCommand request,
        Uri website,
        WebPage home,
        IReadOnlyCollection<string> candidates,
        IReadOnlyCollection<CatalogCategory> categories,
        CancellationToken cancellationToken)
    {
        var message = new StringBuilder();
        message.AppendLine(CultureInfo.InvariantCulture, $"Site : {website}")
            .AppendLine(CultureInfo.InvariantCulture, $"Nom donné par l'utilisateur : {request.Name ?? "aucun"}")
            .AppendLine(CultureInfo.InvariantCulture, $"Titre de la page d'accueil : {home.Title ?? "aucun"}")
            .AppendLine("Pages de recherche déclarées par le site :");
        foreach (string candidate in candidates)
        {
            message.AppendLine(CultureInfo.InvariantCulture, $"- {candidate}");
        }

        message.AppendLine("Typologies de Pricely (identifiant : chemin) :");
        foreach (CatalogCategory category in categories)
        {
            message.AppendLine(CultureInfo.InvariantCulture, $"- {category.Id} : {Path(category, categories)}");
        }

        message.AppendLine().AppendLine("Texte de la page d'accueil :")
            .AppendLine(home.Text.Length > HomePageTextLength ? home.Text[..HomePageTextLength] : home.Text);

        LlmClient llm = await llmGateway.ForFeatureAsync(PricingAiFeatures.MerchantSetup, cancellationToken);
        Result<LlmResponse> response = await llm.SendAsync(
            new LlmPrompt
            {
                SystemPrompt = Instructions,
                Message = message.ToString(),
                RequireJsonResponse = true,
                SessionId = $"merchant-setup:{Guid.CreateVersion7()}"
            },
            cancellationToken);

        return response.IsSuccess ? LlmJson.Read<AdvisorReply>(response.Value.Content) : null;
    }

    /// <summary>A real search: the page must answer on the site; the searched text in it is a good sign of a result list.</summary>
    private async Task<SearchCheckResponse> CheckSearchAsync(Uri website, string template, string query, CancellationToken cancellationToken)
    {
        string address = template.Replace(Merchant.QueryPlaceholder, Uri.EscapeDataString(query), StringComparison.Ordinal);
        if (!Uri.TryCreate(address, UriKind.Absolute, out Uri? tested) || !Merchant.BelongsToSite(website, tested))
        {
            return new SearchCheckResponse(query, null, IsReachable: false, ShowsQuery: false, TextLength: 0);
        }

        Result<WebPage> page = await webPageReader.ReadFollowingAsync(tested, next => Merchant.BelongsToSite(website, next), cancellationToken);
        int length = page.IsSuccess ? page.Value.Text.Length : 0;

        return new SearchCheckResponse(
            query,
            tested,
            IsReachable: length >= MinResultPageLength,
            ShowsQuery: page.IsSuccess && page.Value.Text.Contains(query, StringComparison.OrdinalIgnoreCase),
            length);
    }

    private static List<SuggestedCategoryResponse> Categories(AdvisorReply? reply, IReadOnlyCollection<CatalogCategory> categories)
    {
        var known = categories.ToDictionary(c => c.Id);
        var suggested = new List<SuggestedCategoryResponse>();

        foreach (string id in reply?.CategoryIds ?? [])
        {
            if (Guid.TryParse(id, out Guid categoryId) && known.TryGetValue(categoryId, out CatalogCategory? category)
                && suggested.TrueForAll(s => s.ProductCategoryId != categoryId))
            {
                suggested.Add(new SuggestedCategoryResponse(categoryId, Path(category, categories), reply!.CategoryReasons?.GetValueOrDefault(id)));
            }
        }

        return suggested;
    }

    private static string ChooseName(string? requested, string? proposed, string? title, Uri website)
    {
        string name = new[] { requested, proposed, title }.FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))?.Trim() ?? website.Host;

        return name.Length > Merchant.NameMaxLength ? name[..Merchant.NameMaxLength] : name;
    }

    /// <summary>« Gros œuvre › Ciment », from the root.</summary>
    private static string Path(CatalogCategory category, IReadOnlyCollection<CatalogCategory> categories)
    {
        var names = new List<string> { category.Name };
        CatalogCategory? current = category;

        // Three levels in Catalog; the bound only guards against a cycle in bad data.
        while (current?.ParentId is Guid parentId && names.Count <= categories.Count)
        {
            current = categories.FirstOrDefault(c => c.Id == parentId);
            if (current is not null)
            {
                names.Insert(0, current.Name);
            }
        }

        return string.Join(" › ", names);
    }

    private static Uri? ParseSite(string website)
    {
        string trimmed = website.Trim();
        string absolute = trimmed.Contains("://", StringComparison.Ordinal) ? trimmed : "https://" + trimmed;

        return Uri.TryCreate(absolute, UriKind.Absolute, out Uri? site) && IsWeb(site) && site.Host.Contains('.', StringComparison.Ordinal)
            ? site
            : null;
    }

    private static bool IsWeb(Uri address) => address.Scheme is "http" or "https";

    private static bool IsOnSite(Uri website, string template) =>
        template.Contains(Merchant.QueryPlaceholder, StringComparison.Ordinal)
        && Uri.TryCreate(template.Replace(Merchant.QueryPlaceholder, "x", StringComparison.Ordinal), UriKind.Absolute, out Uri? sample)
        && Merchant.BelongsToSite(website, sample);
}
