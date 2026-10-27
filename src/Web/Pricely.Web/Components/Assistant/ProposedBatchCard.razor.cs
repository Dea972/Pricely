using System.Net;
using Microsoft.AspNetCore.Components;
using Pricely.Web.Api;
using Pricely.Web.Assistant;
using Pricely.Web.Components.Shared;

namespace Pricely.Web.Components.Assistant;

/// <summary>
/// The write actions an answer proposed together (a batch, ADR-028): « Valider le lot » approves every action still
/// pending and runs them in order, « Refuser le lot » changes nothing, « Retirer du lot » refuses one before deciding the
/// rest. Each line keeps its trace (outcome or error) after the decision.
/// </summary>
public partial class ProposedBatchCard
{
    private bool _busy;
    private string? _message;

    [Parameter, EditorRequired]
    public IReadOnlyList<ProposedAction> Actions { get; set; } = [];

    /// <summary>Whether the answer is over: until then the assistant may still add actions to the batch.</summary>
    [Parameter]
    public bool IsComplete { get; set; } = true;

    [Parameter]
    public EventCallback<ProposedAction> OnDecided { get; set; }

    [Inject]
    private PricelyApiClient Api { get; set; } = default!;

    [Inject]
    private ReviewQueue Reviews { get; set; } = default!;

    private bool IsPending => Actions.Any(a => a.IsPending);

    private Guid BatchId => Actions[0].BatchId;

    private string DecidedLabel
    {
        get
        {
            int executed = Actions.Count(a => a.Status == ApiValues.Executed);
            int failed = Actions.Count(a => a.Status == ApiValues.Failed);
            int rejected = Actions.Count(a => a.Status == ApiValues.Rejected);

            if (executed + failed == 0)
            {
                return "Refusé — rien n'a été modifié";
            }

            string label = $"✓ Validé par toi · {executed} exécutée(s)";
            if (failed > 0)
            {
                label += $" · {failed} en échec";
            }

            if (rejected > 0)
            {
                label += $" · {rejected} retirée(s)";
            }

            return label;
        }
    }

    private static string StatusLabel(ProposedAction action) =>
        action.Status switch
        {
            ApiValues.Pending => "● En attente",
            ApiValues.Executed => "✓ Exécutée",
            ApiValues.Failed => "Échec",
            ApiValues.Rejected => "Refusée",
            _ => "En cours…"
        };

    private static string BadgeClass(ProposedAction action) =>
        action.Status == ApiValues.Executed ? "badge-confirmed" : "badge-pending";

    private Task ApproveAsync() => DecideAsync(() => Api.ApproveBatchAsync(BatchId, CancellationToken.None));

    private Task RejectAsync() => DecideAsync(() => Api.RejectBatchAsync(BatchId, CancellationToken.None));

    private Task RemoveAsync(ProposedAction action) =>
        DecideAsync(async () => [await Api.RejectActionAsync(action.ActionId, CancellationToken.None)]);

    private async Task DecideAsync(Func<Task<IReadOnlyList<PendingActionDto>>> decide)
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
            IReadOnlyList<PendingActionDto>? decided = await CallAsync(decide);
            if (decided is not null)
            {
                await ApplyAsync(decided);
            }
            else if (Problem?.Exception is PricelyApiException refused)
            {
                _message = MessageFor(refused);
                Problem = _message is null ? Problem : null;
            }
            else if (Problem?.IsTimeout == true)
            {
                // The API may still be running the batch: its outcome shows when the conversation is opened again.
                _message = "Le lot n'a pas répondu à temps. Rouvre la conversation pour voir ce qui a été exécuté.";
                Problem = null;
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task ApplyAsync(IReadOnlyList<PendingActionDto> decided)
    {
        var byId = decided.ToDictionary(d => d.Id);
        foreach (ProposedAction action in Actions)
        {
            if (byId.TryGetValue(action.ActionId, out PendingActionDto? dto))
            {
                action.Update(dto);
                await OnDecided.InvokeAsync(action);
            }
        }

        // A web offer can join the offers to review.
        if (decided.Any(d => d.Status == ApiValues.Executed))
        {
            await Reviews.RefreshAsync();
        }
    }

    private string? MessageFor(PricelyApiException refused) =>
        refused.StatusCode switch
        {
            HttpStatusCode.Forbidden => "Il te manque le droit de valider une des actions du lot : rien n'a été validé.",
            HttpStatusCode.Conflict => "Ce lot a déjà été validé ou refusé.",
            HttpStatusCode.NotFound => "Ce lot n'existe plus.",
            _ => null
        };
}
