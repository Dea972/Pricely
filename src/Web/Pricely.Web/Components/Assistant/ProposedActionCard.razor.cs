using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Pricely.Web.Api;
using Pricely.Web.Assistant;
using Pricely.Web.Components.Shared;

namespace Pricely.Web.Components.Assistant;

/// <summary>
/// A write tool call of the agent, waiting for the user: « Valider » sends the approval to the API, which runs the tool
/// with the proposed arguments without going back to the model (ADR-008). After the decision, the card keeps its trace.
/// </summary>
public partial class ProposedActionCard
{
    private CollectionPlanDto? _plan;
    private PendingOfferDto? _offer;
    private Guid? _offerId;
    private string? _productName;
    private Guid? _planProductId;
    private bool _busy;
    private string? _message;

    [Parameter, EditorRequired]
    public ProposedAction Action { get; set; } = default!;

    [Parameter]
    public EventCallback<ProposedAction> OnDecided { get; set; }

    [Inject]
    private PricelyApiClient Api { get; set; } = default!;

    [Inject]
    private ProductLookup Products { get; set; } = default!;

    [Inject]
    private ReviewQueue Reviews { get; set; } = default!;

    private IEnumerable<JsonProperty> Arguments =>
        Action.Arguments.ValueKind == JsonValueKind.Object ? Action.Arguments.EnumerateObject() : [];

    private string ExecutedSummary
    {
        get
        {
            if (Action.Result is not JsonElement result)
            {
                return "L'action a été exécutée.";
            }

            if (string.Equals(Action.ToolName, ToolCatalog.CollectProductOffers, StringComparison.Ordinal)
                && ToolCatalog.Read<CollectResultDto>(result) is { } collected)
            {
                return $"{collected.CollectedCount} offre(s) collectée(s) · {collected.ProposedCount} à valider · {collected.ObservedCount} relevé(s) · {collected.AnomalyCount} prix signalé(s)";
            }

            // Other write tools answer with a sentence (« Offre confirmée. »).
            return result.ValueKind == JsonValueKind.String ? result.GetString() ?? "L'action a été exécutée." : "L'action a été exécutée.";
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        // What a collection would do: the product and its merchants, read from the collection plan (display only).
        Guid? productId = string.Equals(Action.ToolName, ToolCatalog.CollectProductOffers, StringComparison.Ordinal)
            ? ToolCatalog.ProductIdOf(Action.Arguments)
            : null;

        if (productId is Guid id && _planProductId != id)
        {
            _planProductId = id;
            _plan = await CallAsync(() => Api.GetCollectionPlanAsync(id, CancellationToken.None));

            // Without a plan (no preferred merchant yet), the product alone: it is not an error of the action itself.
            Problem = Problem?.IsSessionExpired == true ? Problem : null;
            _productName = _plan is null ? (await Products.FindAsync(id))?.Name : null;
        }

        // The offer to confirm or reject, while it waits for a review (display only).
        Guid? offerId = ToolCatalog.IsOfferReview(Action.ToolName) ? ToolCatalog.OfferIdOf(Action.Arguments) : null;
        if (offerId is Guid offer && _offerId != offer)
        {
            _offerId = offer;
            IReadOnlyList<PendingOfferDto>? pending = await CallAsync(() => Api.GetOffersPendingReviewAsync(CancellationToken.None));
            Problem = Problem?.IsSessionExpired == true ? Problem : null;
            _offer = pending?.FirstOrDefault(o => o.OfferId == offer);
        }
    }

    private Task ApproveAsync() => DecideAsync(() => Api.ApproveActionAsync(Action.ActionId, CancellationToken.None));

    private Task RejectAsync() => DecideAsync(() => Api.RejectActionAsync(Action.ActionId, CancellationToken.None));

    private async Task DecideAsync(Func<Task<PendingActionDto>> decide)
    {
        // A second click that arrives before the buttons are disabled.
        if (_busy)
        {
            return;
        }

        _busy = true;
        _message = null;
        Problem = null;

        try
        {
            PendingActionDto? decided = await CallAsync(decide);
            if (decided is not null)
            {
                Action.Update(decided);
                await OnDecided.InvokeAsync(Action);

                if (ToolCatalog.IsOfferReview(Action.ToolName))
                {
                    await Reviews.RefreshAsync();
                }
            }
            else if (Problem?.Exception is PricelyApiException refused)
            {
                _message = MessageFor(refused);
                Problem = _message is null ? Problem : null;
            }
            else if (Problem?.IsTimeout == true)
            {
                // The API may still have run it: its outcome shows when the conversation is opened again.
                _message = "La validation n'a pas répondu à temps. Rouvre la conversation pour voir si l'action a été exécutée.";
                Problem = null;
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private string? MessageFor(PricelyApiException refused) =>
        refused.StatusCode switch
        {
            HttpStatusCode.Forbidden => $"Il te manque le droit de valider cette action (scope {Action.RequiredScope}).",
            HttpStatusCode.Conflict => "Cette action a déjà été validée ou refusée.",
            HttpStatusCode.NotFound => "Cette action n'existe plus.",
            _ => null
        };
}
