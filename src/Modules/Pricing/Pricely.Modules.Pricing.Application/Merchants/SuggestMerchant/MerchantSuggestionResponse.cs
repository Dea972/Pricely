namespace Pricely.Modules.Pricing.Application.Merchants.SuggestMerchant;

/// <param name="Website">The home page of the site, after its redirects.</param>
/// <param name="SearchTemplate">The proposed search page (<c>{query}</c>), if one was found.</param>
/// <param name="SearchCandidates">The search pages the markup of the site declares.</param>
/// <param name="SearchCheck">The test of the proposed search page, if there is one.</param>
/// <param name="Rationale">Why the model proposes this, for the user.</param>
/// <param name="IsAiAnswer">False when the model could not answer: the proposal comes from the site alone.</param>
public sealed record MerchantSuggestionResponse(
    string Name,
    Uri Website,
    string? SearchTemplate,
    IReadOnlyList<string> SearchCandidates,
    SearchCheckResponse? SearchCheck,
    IReadOnlyList<SuggestedCategoryResponse> Categories,
    string? Rationale,
    bool IsAiAnswer);

/// <param name="TestedAddress">The search page read with <see cref="Query"/>.</param>
/// <param name="IsReachable">The page answered, on the site of the merchant.</param>
/// <param name="ShowsQuery">The searched text appears in the page (a result list usually repeats it).</param>
/// <param name="TextLength">Length of the text of the page.</param>
public sealed record SearchCheckResponse(string Query, Uri? TestedAddress, bool IsReachable, bool ShowsQuery, int TextLength);

public sealed record SuggestedCategoryResponse(Guid ProductCategoryId, string Name, string? Reason);
