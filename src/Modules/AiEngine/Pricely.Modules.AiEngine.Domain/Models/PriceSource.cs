namespace Pricely.Modules.AiEngine.Domain.Models;

/// <summary>Origin of a model price.</summary>
public enum PriceSource
{
    /// <summary>Entered by an administrator.</summary>
    Manual = 0,

    /// <summary>Imported from the public price list of LiteLLM, then applied by an administrator (ADR-021).</summary>
    LiteLlm = 1,

    /// <summary>Read by a model on the pricing page of the provider, then applied by an administrator (ADR-021).</summary>
    AiReading = 2
}
