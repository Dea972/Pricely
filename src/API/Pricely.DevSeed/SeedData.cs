namespace Pricely.DevSeed;

/// <summary>How the scripted page of a merchant shows the product, hence how the matching policy treats the offer.</summary>
internal enum SeedMatch
{
    /// <summary>The page shows the GTIN of the product: confirmed automatically.</summary>
    Gtin,

    /// <summary>The page shows the brand and the manufacturer reference: confirmed automatically.</summary>
    BrandAndReference,

    /// <summary>The page shows the brand only: waits for a review.</summary>
    BrandOnly,

    /// <summary>The page shows another reference: waits for a review.</summary>
    OtherReference,

    /// <summary>The page shows a GTIN that differs by one digit: waits for a review.</summary>
    OtherGtin
}

internal sealed record SeedCategory(string Key, string Name, string? ParentKey);

internal sealed record SeedProduct(string Name, string Brand, string Gtin, string Reference, string CategoryKey, decimal Price);

internal sealed record SeedMerchant(string Key, string Name, string Website, string? SearchTemplate, decimal PriceFactor, bool IsActive = true);

internal sealed record SeedProfile(string CategoryKey, string[] MerchantKeys, string Instructions);

/// <summary>A merchant page that differs from the default one (GTIN shown, in stock, usual price).</summary>
internal sealed record SeedListing(string Gtin, string MerchantKey, SeedMatch Match)
{
    public string? ObservedTitle { get; init; }

    public string? ObservedValue { get; init; }

    public decimal? Similarity { get; init; }

    public string? Rationale { get; init; }
}

/// <summary>
/// The test data. Fictitious brands, GTINs and merchants (the sites are on the reserved <c>.example</c> domain): the
/// collection agent cannot read them, the observations come from <see cref="SeedOfferCollector"/>.
/// </summary>
internal static class SeedData
{
    /// <summary>Weekly observations, the last one today.</summary>
    public const int Weeks = 13;

    public static readonly SeedCategory[] Categories =
    [
        new("gros-oeuvre", "Gros œuvre", null),
        new("ciment", "Ciment et mortier", "gros-oeuvre"),
        new("blocs", "Blocs et parpaings", "gros-oeuvre"),
        new("platrerie", "Plâtrerie et isolation", null),
        new("plaques", "Plaques de plâtre", "platrerie"),
        new("isolants", "Isolants", "platrerie"),
        new("peinture", "Peinture", null),
        new("peinture-interieure", "Peinture intérieure", "peinture"),
        new("outillage", "Outillage", null),
        new("perceuses", "Perceuses et visseuses", "outillage")
    ];

    public static readonly SeedProduct[] Products =
    [
        new("Ciment CEM II/B-LL 32,5 R — sac 35 kg", "Cimenterie du Rhône", "3760000100014", "CR-325R-35", "ciment", 9.40m),
        new("Mortier colle C2TE — sac 25 kg", "Fixalis", "3760000100021", "FX-C2TE-25", "ciment", 19.90m),
        new("Parpaing creux 20 × 20 × 50 cm", "Bétonor", "3760000100038", "BT-P20", "blocs", 1.45m),
        new("Plaque de plâtre BA13 standard 2,50 × 1,20 m", "Gyplaque", "3760000100045", "GP-BA13-250", "plaques", 7.60m),
        new("Laine de verre 100 mm R = 2,5 — rouleau 6 m²", "Isoverre", "3760000100052", "IV-100-25", "isolants", 32.90m),
        new("Peinture acrylique blanc mat 10 L", "Colorine", "3760000100069", "CL-MAT-10", "peinture-interieure", 54.90m),
        new("Peinture acrylique blanc satin 2,5 L", "Colorine", "3760000100076", "CL-SAT-2.5", "peinture-interieure", 24.50m),
        new("Perceuse visseuse 18 V + 2 batteries 2 Ah", "Voltix", "3760000100083", "VX-PV18-2", "perceuses", 129.00m),
        new("Visseuse à chocs 18 V sans batterie", "Voltix", "3760000100090", "VX-VC18-0", "perceuses", 89.00m)
    ];

    public static readonly SeedMerchant[] Merchants =
    [
        new("comptoir", "Comptoir du Bâtiment", "https://www.comptoir-batiment.example/", "https://www.comptoir-batiment.example/recherche?q={query}", 1.00m),
        new("negoce", "Négoce Atlantique", "https://www.negoce-atlantique.example/", "https://www.negoce-atlantique.example/search?text={query}", 0.97m),
        new("materiaux", "MatériauxDirect", "https://www.materiauxdirect.example/", null, 0.94m),
        new("brico", "BricoPlus", "https://www.bricoplus.example/", "https://www.bricoplus.example/s?k={query}", 1.06m),
        new("quincaillerie", "Quincaillerie Martin", "https://www.quincaillerie-martin.example/", null, 1.10m, IsActive: false)
    ];

    public static readonly SeedProfile[] Profiles =
    [
        new("gros-oeuvre", ["comptoir", "negoce", "materiaux"], "Prix TTC livré si disponible, sinon prix à emporter. Noter la disponibilité et le délai."),
        new("ciment", [], "Relever le prix au sac, jamais à la palette. Vérifier la classe de résistance et le poids du sac."),
        new("platrerie", ["comptoir", "materiaux", "brico"], "Prix à l'unité (plaque, rouleau), jamais au mètre carré seul."),
        new("peinture", ["brico", "comptoir"], "Comparer à contenance égale ; ignorer les teintes faites à la machine."),
        new("outillage", ["brico", "comptoir", "negoce"], "Vérifier le contenu du coffret : batteries et chargeur inclus ou non.")
    ];

    public static readonly SeedListing[] Listings =
    [
        new("3760000100014", "materiaux", SeedMatch.BrandOnly)
        {
            ObservedTitle = "Ciment gris CEM II 32,5R 35kg Cimenterie du Rhône",
            Similarity = 0.9m,
            Rationale = "Même marque, même classe et même conditionnement ; la page ne publie ni GTIN ni référence."
        },
        new("3760000100021", "materiaux", SeedMatch.OtherReference)
        {
            ObservedTitle = "Mortier colle carrelage C2 flex 25 kg Fixalis",
            ObservedValue = "FX-C2-25",
            Similarity = 0.55m,
            Rationale = "Référence différente (C2 au lieu de C2TE) : probablement un produit voisin, sans glissement réduit."
        },
        new("3760000100045", "brico", SeedMatch.OtherGtin)
        {
            ObservedTitle = "Plaque BA13 Gyplaque 250x120",
            ObservedValue = "3760000100046",
            Similarity = 0.85m,
            Rationale = "Même désignation, mais le GTIN affiché diffère d'un chiffre : vérifier la clé de contrôle."
        },
        new("3760000100083", "negoce", SeedMatch.BrandAndReference),
        new("3760000100069", "comptoir", SeedMatch.BrandAndReference)
    ];

    /// <summary>
    /// The last week, this merchant shows the price of a pack of 10: the price guard flags it. A product with three
    /// confirmed offers, and the last merchant of its plan: a flagged price is still a reference price for the next ones.
    /// </summary>
    public static readonly (string Gtin, string MerchantKey) PackPrice = ("3760000100038", "materiaux");

    /// <summary>The last two weeks, this merchant is out of stock.</summary>
    public static readonly (string Gtin, string MerchantKey) OutOfStock = ("3760000100052", "materiaux");
}
