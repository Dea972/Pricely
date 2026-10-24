namespace Pricely.Modules.Pricing.Domain.Offers;

public enum OfferConfirmationMethod
{
    /// <summary>Same GTIN on the merchant page and on the reference product.</summary>
    Gtin = 0,

    /// <summary>Same brand and same manufacturer reference.</summary>
    ManufacturerReference = 1,

    /// <summary>Confirmed by a reviewer.</summary>
    Manual = 2
}
