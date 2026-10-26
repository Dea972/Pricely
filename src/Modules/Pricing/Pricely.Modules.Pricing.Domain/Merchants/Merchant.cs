using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Domain.Merchants;

/// <summary>
/// A merchant (enseigne) whose prices are collected and compared.
/// </summary>
public sealed class Merchant : Entity
{
    public const int NameMaxLength = 150;

    public const int SearchTemplateMaxLength = 500;

    /// <summary>Placeholder of the search address template, replaced by the escaped search text.</summary>
    public const string QueryPlaceholder = "{query}";

    private Merchant()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public Uri Website { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>
    /// Search page of the merchant's own site, with <see cref="QueryPlaceholder"/> (e.g.
    /// <c>https://www.enseigne.fr/recherche?q={query}</c>): the collection agent starts there (ADR-017).
    /// </summary>
    public string? SearchTemplate { get; private set; }

    public static Result<Merchant> Register(string name, string website, string? searchTemplate = null)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > NameMaxLength)
        {
            return Result.Failure<Merchant>(MerchantErrors.InvalidName);
        }

        if (!Uri.TryCreate(website.Trim(), UriKind.Absolute, out Uri? websiteUri) ||
            websiteUri.Scheme is not ("http" or "https"))
        {
            return Result.Failure<Merchant>(MerchantErrors.InvalidWebsite(website));
        }

        Merchant merchant = new()
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            Website = websiteUri,
            IsActive = true
        };

        Result search = merchant.ConfigureSearch(searchTemplate);
        if (search.IsFailure)
        {
            return Result.Failure<Merchant>(search.Error);
        }

        merchant.Raise(new MerchantRegisteredDomainEvent(merchant.Id));

        return merchant;
    }

    /// <summary>
    /// Sets (or removes, when empty) the search address template: an http(s) address of the merchant's own site that
    /// contains <see cref="QueryPlaceholder"/>.
    /// </summary>
    public Result ConfigureSearch(string? searchTemplate)
    {
        if (string.IsNullOrWhiteSpace(searchTemplate))
        {
            SearchTemplate = null;

            return Result.Success();
        }

        string template = searchTemplate.Trim();
        bool isValid = template.Length <= SearchTemplateMaxLength
            && template.Contains(QueryPlaceholder, StringComparison.Ordinal)
            && Uri.TryCreate(template.Replace(QueryPlaceholder, "x", StringComparison.Ordinal), UriKind.Absolute, out Uri? sample)
            && Owns(sample);

        if (!isValid)
        {
            return Result.Failure(MerchantErrors.InvalidSearchTemplate(template));
        }

        SearchTemplate = template;

        return Result.Success();
    }

    /// <summary>The search page for a text, or null when the merchant has no search template.</summary>
    public Uri? SearchAddress(string text) =>
        SearchTemplate is null
            ? null
            : new Uri(SearchTemplate.Replace(QueryPlaceholder, Uri.EscapeDataString(text.Trim()), StringComparison.Ordinal));

    /// <summary>
    /// Whether an address belongs to the merchant's site: http(s), same host as the website or one of its sub-domains
    /// (<c>www.</c> aside). The collection agent never reads a page elsewhere (ADR-009).
    /// </summary>
    public bool Owns(Uri address)
    {
        if (!address.IsAbsoluteUri || address.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(address.UserInfo))
        {
            return false;
        }

        string siteHost = WithoutWww(Website.IdnHost).ToUpperInvariant();
        string host = address.IdnHost.ToUpperInvariant();

        return host == siteHost || host.EndsWith("." + siteHost, StringComparison.Ordinal);
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    private static string WithoutWww(string host) =>
        host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;
}
