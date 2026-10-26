using Microsoft.AspNetCore.Components;
using Pricely.Web.Api;
using Pricely.Web.Assistant;
using Pricely.Web.Components.Shared;

namespace Pricely.Web.Components.Pages;

/// <summary>
/// The assistant (agent <c>assistant</c>): sends the message, shows the turn as its events arrive (tools, typed results,
/// proposed actions, text), and reopens a stored conversation.
/// </summary>
public partial class AssistantPage : IDisposable
{
    private const string AgentName = "assistant";

    private static readonly string[] DefaultSuggestions = ["Historique sur 3 mois", "Offres à valider", "Plan de collecte"];

    private ConversationView _view = new();
    private IReadOnlyList<ConversationSummaryDto>? _conversations;
    private AgentUsageDto? _usage;
    private CancellationTokenSource? _turn;
    private bool _showConversations;
    private bool _loading;

    [Parameter]
    public Guid? ConversationId { get; set; }

    /// <summary>A message proposed by another screen (« Demander à l'assistant »), not sent until the user does.</summary>
    [SupplyParameterFromQuery]
    public string? Prompt { get; set; }

    [Inject]
    private PricelyApiClient Api { get; set; } = default!;

    [Inject]
    private AgentStreamClient Agent { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private TimeProvider Clock { get; set; } = default!;

    private IReadOnlyList<string> Suggestions => _view.Entries.Count == 0 ? [] : DefaultSuggestions;

    protected override Task OnInitializedAsync() => LoadUsageAsync();

    protected override async Task OnParametersSetAsync()
    {
        // The address follows the conversation started by the running turn: nothing to load.
        if (ConversationId == _view.ConversationId)
        {
            return;
        }

        CancelTurn();
        _showConversations = false;

        if (ConversationId is not Guid conversationId)
        {
            _view = new ConversationView();

            return;
        }

        _loading = true;
        ConversationDto? conversation = await CallAsync(() => Api.GetConversationAsync(conversationId, CancellationToken.None));
        _view = conversation is null ? new ConversationView() : ConversationView.FromHistory(conversation);
        _loading = false;
    }

    public void Dispose()
    {
        CancelTurn();
        GC.SuppressFinalize(this);
    }

    private async Task SendAsync(string message)
    {
        if (_view.IsRunning)
        {
            return;
        }

        ConversationView view = _view;
        view.Send(message);
        Problem = null;

        using var turn = new CancellationTokenSource();
        _turn = turn;

        try
        {
            await foreach (AgentStreamEvent agentEvent in Agent.SendMessageAsync(AgentName, view.ConversationId, message, turn.Token))
            {
                view.Apply(agentEvent);

                if (agentEvent is ConversationStarted started && ConversationId != started.ConversationId)
                {
                    Navigation.NavigateTo($"/assistant/{started.ConversationId}", new NavigationOptions { ReplaceHistoryEntry = true });
                }

                StateHasChanged();
            }
        }
        catch (OperationCanceledException) when (turn.IsCancellationRequested)
        {
            // The user left the conversation: the API keeps what the turn did.
        }
        catch (Exception exception) when (ApiProblem.From(exception, LoginAddress) is ApiProblem problem)
        {
            view.Interrupt(problem.Message);
            Problem = problem.IsSessionExpired ? problem : null;
        }
        finally
        {
            _turn = ReferenceEquals(_turn, turn) ? null : _turn;
        }

        view.Interrupt("Le tour s'est arrêté avant la fin.");
        await LoadUsageAsync();
    }

    private async Task ToggleConversationsAsync()
    {
        _showConversations = !_showConversations;
        if (_showConversations)
        {
            _conversations = null;
            _conversations = await CallAsync(() => Api.GetConversationsAsync(AgentName, CancellationToken.None)) ?? [];
        }
    }

    private void OnActionDecided(ProposedAction action) => StateHasChanged();

    private async Task LoadUsageAsync()
    {
        DateTime today = Clock.GetUtcNow().UtcDateTime.Date;
        ApiProblem? previous = Problem;
        IReadOnlyList<AgentUsageDto>? usage = await CallAsync(() => Api.GetUsageByAgentAsync(today, CancellationToken.None));

        // The usage is a side panel: only an expired session is worth replacing the message of the conversation.
        Problem = Problem?.IsSessionExpired == true ? Problem : previous;
        _usage = usage?.FirstOrDefault(u => string.Equals(u.AgentName, AgentName, StringComparison.Ordinal));
    }

    private void CancelTurn()
    {
        try
        {
            _turn?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The turn has just ended.
        }

        _turn = null;
    }
}
