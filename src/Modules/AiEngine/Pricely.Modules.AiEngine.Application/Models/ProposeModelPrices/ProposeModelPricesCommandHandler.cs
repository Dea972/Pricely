using System.Globalization;
using System.Text;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Application.Web;
using Pricely.Common.Domain;
using Pricely.Modules.AiEngine.Application.Abstractions.Prices;
using Pricely.Modules.AiEngine.Domain.Models;

namespace Pricely.Modules.AiEngine.Application.Models.ProposeModelPrices;

/// <summary>
/// LiteLLM first (a maintained list, read as is); for the models it does not know, the model of
/// <see cref="AiEngineAiFeatures.PriceReading"/> reads the pricing page of their provider. A price the AI gives is kept
/// only if the page shows it (« $3 », « $3.75 »...), and is marked « to check ».
/// </summary>
internal sealed class ProposeModelPricesCommandHandler(
    IModelRepository modelRepository,
    IPublishedPriceList publishedPriceList,
    IWebPageReader webPageReader,
    ILlmGateway llmGateway,
    ModelPriceOptions options,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ProposeModelPricesCommand, IReadOnlyCollection<ModelPriceProposalResponse>>
{
    public const string NotFound = "NotFound";
    public const string PriceListUnavailable = "PriceListUnavailable";
    public const string CachePricesMissing = "CachePricesMissing";
    public const string ToCheck = "ToCheck";

    private const string LiteLlmSource = "LiteLlm";
    private const string AiReadingSource = "AiReading";
    private const int PricingTextLength = 30_000;

    private static readonly string[] AmountFormats = ["0.##", "0.00", "0.###"];

    private const string Instructions = """
        Tu lis la page de tarifs d'un fournisseur de modèles de langage. Pour chaque modèle demandé, donne ses tarifs
        en dollars US par million de jetons, tels que la page les affiche. Réponds uniquement par un objet JSON :
        {"prices": [{"model": "identifiant demandé", "input": 3, "cachedInput": 0.3, "cacheWrite": 3.75, "output": 15}]}
        Règles : tarifs standard (pas de traitement par lots, pas de remise) ; cacheWrite = écriture du cache la plus
        courte (5 minutes) ; null pour un tarif que la page n'affiche pas ; n'inclus pas un modèle absent de la page.
        """;

    public async Task<Result<IReadOnlyCollection<ModelPriceProposalResponse>>> Handle(
        ProposeModelPricesCommand request,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Model> models = await modelRepository.GetAllAsync(cancellationToken);
        if (models.Count == 0)
        {
            return Result.Success<IReadOnlyCollection<ModelPriceProposalResponse>>([]);
        }

        Result<IReadOnlyCollection<PublishedPrice>> published = await publishedPriceList.FindAsync(
            [.. models.Select(m => new PublishedPriceQuery(m.Provider, m.ProviderModelId))],
            cancellationToken);
        IReadOnlyCollection<PublishedPrice> fromLiteLlm = published.IsSuccess ? published.Value : [];

        Model[] unknown = [.. models.Where(m => Find(fromLiteLlm, m) is null)];
        Dictionary<Guid, ReadPrice> fromAi = await ReadPricingPagesAsync(unknown, cancellationToken);

        DateTime now = dateTimeProvider.UtcNow;
        var proposals = new List<ModelPriceProposalResponse>();
        foreach (Model model in models)
        {
            List<string> notes = published.IsFailure ? [PriceListUnavailable] : [];
            (PriceResponse? proposed, string? source) = Propose(model, fromLiteLlm, fromAi, notes);
            PriceResponse? current = Current(model.PriceAt(now));

            proposals.Add(new ModelPriceProposalResponse(
                model.Id,
                model.Provider.ToString(),
                model.ProviderModelId,
                current,
                proposed,
                source,
                IsChanged: proposed is not null && (current is null || !SameValues(current, proposed)),
                notes));
        }

        return Result.Success<IReadOnlyCollection<ModelPriceProposalResponse>>(proposals);
    }

    private static (PriceResponse? Proposed, string? Source) Propose(
        Model model,
        IReadOnlyCollection<PublishedPrice> fromLiteLlm,
        Dictionary<Guid, ReadPrice> fromAi,
        List<string> notes)
    {
        if (Find(fromLiteLlm, model) is PublishedPrice price)
        {
            if (price.CachedInputPerMillion is null || price.CacheWritePerMillion is null)
            {
                notes.Add(CachePricesMissing);
            }

            return (
                new PriceResponse(
                    price.InputPerMillion,
                    price.CachedInputPerMillion ?? price.InputPerMillion,
                    price.CacheWritePerMillion ?? price.InputPerMillion,
                    price.OutputPerMillion),
                LiteLlmSource);
        }

        if (fromAi.TryGetValue(model.Id, out ReadPrice? read))
        {
            notes.Add(ToCheck);
            if (read.CachedInput is null || read.CacheWrite is null)
            {
                notes.Add(CachePricesMissing);
            }

            return (new PriceResponse(read.Input!.Value, read.CachedInput ?? read.Input.Value, read.CacheWrite ?? read.Input.Value, read.Output!.Value), AiReadingSource);
        }

        notes.Add(NotFound);

        return (null, null);
    }

    /// <summary>One page and one call per provider, for its models that LiteLLM does not know.</summary>
    private async Task<Dictionary<Guid, ReadPrice>> ReadPricingPagesAsync(Model[] models, CancellationToken cancellationToken)
    {
        var read = new Dictionary<Guid, ReadPrice>();

        foreach (IGrouping<Provider, Model> provider in models.GroupBy(m => m.Provider))
        {
            if (!options.PricingPages.TryGetValue(provider.Key.ToString(), out Uri? pricingPage))
            {
                continue;
            }

            Result<WebPage> page = await webPageReader.ReadFollowingAsync(pricingPage, next => next.Scheme == Uri.UriSchemeHttps, cancellationToken);
            if (page.IsFailure)
            {
                continue;
            }

            string text = PricingLines(page.Value.Text);
            PriceReadingReply? reply = await AskAsync(provider, text, cancellationToken);

            foreach (Model model in provider)
            {
                ReadPrice? price = reply?.Prices?.FirstOrDefault(p => string.Equals(p.Model?.Trim(), model.ProviderModelId, StringComparison.OrdinalIgnoreCase));
                if (price is { Input: decimal input, Output: decimal output } && Shows(text, input) && Shows(text, output))
                {
                    read[model.Id] = price with
                    {
                        CachedInput = price.CachedInput is decimal cached && Shows(text, cached) ? cached : null,
                        CacheWrite = price.CacheWrite is decimal write && Shows(text, write) ? write : null
                    };
                }
            }
        }

        return read;
    }

    private async Task<PriceReadingReply?> AskAsync(IEnumerable<Model> models, string pricingText, CancellationToken cancellationToken)
    {
        var message = new StringBuilder("Modèles demandés :\n");
        foreach (Model model in models)
        {
            message.AppendLine(CultureInfo.InvariantCulture, $"- {model.ProviderModelId} ({model.Label})");
        }

        message.AppendLine().AppendLine("Page de tarifs :").AppendLine(pricingText);

        LlmClient llm = await llmGateway.ForFeatureAsync(AiEngineAiFeatures.PriceReading, cancellationToken);
        Result<LlmResponse> response = await llm.SendAsync(
            new LlmPrompt
            {
                SystemPrompt = Instructions,
                Message = message.ToString(),
                RequireJsonResponse = true,
                SessionId = $"price-reading:{Guid.CreateVersion7()}"
            },
            cancellationToken);

        return response.IsSuccess ? LlmJson.Read<PriceReadingReply>(response.Value.Content) : null;
    }

    /// <summary>The lines of the page that carry a price or name a model: a pricing page is long, its tables are not.</summary>
    private static string PricingLines(string text)
    {
        var lines = new StringBuilder();
        foreach (string line in text.Split('\n'))
        {
            if (lines.Length >= PricingTextLength)
            {
                break;
            }

            if (line.Contains('$', StringComparison.Ordinal) || line.Contains("Claude", StringComparison.OrdinalIgnoreCase) || line.Contains("MTok", StringComparison.OrdinalIgnoreCase))
            {
                lines.AppendLine(line);
            }
        }

        return lines.ToString();
    }

    /// <summary>« $3 », « $3.00 », « $0.30 »: the page shows this amount in dollars.</summary>
    private static bool Shows(string text, decimal amount) =>
        amount == 0
        || AmountFormats
            .Select(format => "$" + amount.ToString(format, CultureInfo.InvariantCulture))
            .Any(written => text.Contains(written, StringComparison.Ordinal));

    private static PublishedPrice? Find(IReadOnlyCollection<PublishedPrice> prices, Model model) =>
        prices.FirstOrDefault(p => p.Provider == model.Provider && string.Equals(p.ProviderModelId, model.ProviderModelId, StringComparison.OrdinalIgnoreCase));

    private static PriceResponse? Current(ModelPrice? price) =>
        price is null
            ? null
            : new PriceResponse(price.InputPerMillion, price.CachedInputPerMillion, price.CacheWritePerMillion, price.OutputPerMillion, price.ValidFrom, price.Source.ToString());

    private static bool SameValues(PriceResponse left, PriceResponse right) =>
        left.InputPerMillion == right.InputPerMillion
        && left.CachedInputPerMillion == right.CachedInputPerMillion
        && left.CacheWritePerMillion == right.CacheWritePerMillion
        && left.OutputPerMillion == right.OutputPerMillion;
}
