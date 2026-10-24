using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Domain.Merchants;

/// <summary>
/// A merchant (enseigne) whose prices are collected and compared.
/// </summary>
public sealed class Merchant : Entity
{
    public const int NameMaxLength = 150;

    private Merchant()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public Uri Website { get; private set; }

    public bool IsActive { get; private set; }

    public static Result<Merchant> Register(string name, string website)
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

        merchant.Raise(new MerchantRegisteredDomainEvent(merchant.Id));

        return merchant;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
