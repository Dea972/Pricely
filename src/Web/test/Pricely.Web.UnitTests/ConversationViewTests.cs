using System.Text.Json;
using FluentAssertions;
using Pricely.Web.Api;
using Pricely.Web.Assistant;

namespace Pricely.Web.UnitTests;

public sealed class ConversationViewTests
{
    private static readonly Guid ConversationId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid ActionId = Guid.NewGuid();

    [Fact]
    public void ATurn_ShouldShowTools_TypedResults_ProposedActions_AndText()
    {
        var view = new ConversationView();
        AgentEntry answer = view.Send("Compare le ciment");

        view.Apply(new ConversationStarted(ConversationId, "Compare le ciment"));
        view.IsRunning.Should().BeTrue();
        view.Apply(new ToolCalled("c1", "compare_prices", true, Json($$"""{"productId":"{{ProductId}}"}""")));
        view.Apply(new ToolCalled("c2", "collect_product_offers", false, Json($$"""{"productId":"{{ProductId}}"}""")));
        view.Apply(new ToolResulted("c1", "compare_prices", Json("[]"), null));
        view.Apply(new ActionProposedEvent(ActionId, "c2", "collect_product_offers", Json($$"""{"productId":"{{ProductId}}"}"""), "pricely.write"));
        view.Apply(new TextReceived("Voici le comparatif."));
        view.Apply(new TurnDone(ConversationId));

        view.ConversationId.Should().Be(ConversationId);
        view.IsRunning.Should().BeFalse();
        answer.Tools.Select(t => t.State).Should().Equal(ToolUseState.Succeeded, ToolUseState.AwaitingApproval);
        answer.Outputs.Should().ContainSingle().Which.ToolName.Should().Be("compare_prices");
        answer.Outputs[0].Arguments.Should().NotBeNull("the card needs the product of the call");
        ProposedAction action = answer.Actions.Should().ContainSingle().Subject;
        action.IsPending.Should().BeTrue();
        action.RequiredScope.Should().Be("pricely.write");
        answer.Text.Should().Be("Voici le comparatif.");
        view.CurrentProductId.Should().Be(ProductId);
    }

    [Fact]
    public void AFailedTool_ShouldShowNoResult()
    {
        var view = new ConversationView();
        AgentEntry answer = view.Send("Historique");

        view.Apply(new ToolCalled("c1", "get_price_history", true, null));
        view.Apply(new ToolResulted("c1", "get_price_history", null, "Produit inconnu"));

        answer.Tools[0].State.Should().Be(ToolUseState.Failed);
        answer.Outputs.Should().BeEmpty();
    }

    [Fact]
    public void TextAfterTools_ShouldStartANewParagraph_ButNotTheChunksOfOneAnswer()
    {
        var view = new ConversationView();
        AgentEntry answer = view.Send("Bonjour");

        view.Apply(new TextReceived("Je cherche"));
        view.Apply(new TextReceived(" le produit."));
        view.Apply(new ToolCalled("c1", "search_products", true, null));
        view.Apply(new TextReceived("Trouvé."));

        answer.Text.Should().Be("Je cherche le produit.\n\nTrouvé.");
    }

    [Fact]
    public void ChunksOfSpaces_ShouldBeKept_OnceTheModelStreamsTokens()
    {
        var view = new ConversationView();
        AgentEntry answer = view.Send("Bonjour");

        view.Apply(new TextReceived("Bonjour"));
        view.Apply(new TextReceived(" "));
        view.Apply(new TextReceived("Dominique"));

        answer.Text.Should().Be("Bonjour Dominique");
    }

    [Fact]
    public void AnErrorOfTheApi_ShouldNotBeReplacedByTheInterruption()
    {
        // The API sends « error » then closes the stream without « done » (unknown conversation, invalid message).
        var view = new ConversationView();
        AgentEntry answer = view.Send("Bonjour");

        view.Apply(new TurnFailed("Conversations.NotFound", "Conversation introuvable."));
        view.Interrupt("Le tour s'est arrêté avant la fin.");

        answer.Error.Should().Be("Conversation introuvable.");
        view.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void AStreamThatStops_ShouldEndTheTurnWithAMessage()
    {
        var view = new ConversationView();
        AgentEntry answer = view.Send("Bonjour");

        view.Interrupt("Ta session a expiré.");

        view.IsRunning.Should().BeFalse();
        answer.Error.Should().Be("Ta session a expiré.");
    }

    [Fact]
    public void AStoredConversation_ShouldGroupTheModelMessages_AndFindItsActions()
    {
        var conversation = new ConversationDto(
            ConversationId,
            "assistant",
            "Collecte",
            DateTime.UtcNow,
            [
                Message(1, "User", "Collecte le ciment"),
                Message(2, "Assistant", string.Empty, calls: [new ToolCallDto("c1", "collect_product_offers", false, Json($$"""{"productId":"{{ProductId}}"}"""))]),
                Message(3, "Tool", string.Empty, results: [new ToolResultDto("c1", "collect_product_offers", Json($$"""{"status":"pending_approval","actionId":"{{ActionId}}","message":"…"}"""), null)]),
                Message(4, "Assistant", "Je te propose la collecte."),
                Message(5, "User", "Merci")
            ],
            [new PendingActionDto { Id = ActionId, ToolName = "collect_product_offers", Status = ApiValues.Rejected, Arguments = Json("{}"), RequiredScope = "pricely.write" }]);

        var view = ConversationView.FromHistory(conversation);

        view.Entries.Select(e => e.GetType()).Should().Equal(typeof(UserEntry), typeof(AgentEntry), typeof(UserEntry));
        var answer = (AgentEntry)view.Entries[1];
        answer.IsComplete.Should().BeTrue();
        answer.Text.Should().Be("Je te propose la collecte.");
        answer.Outputs.Should().BeEmpty("the result of a proposal is the action, not a typed result");
        answer.Actions.Should().ContainSingle().Which.Status.Should().Be(ApiValues.Rejected);
        answer.Tools[0].State.Should().Be(ToolUseState.AwaitingApproval);
        view.IsRunning.Should().BeFalse();
        view.ToolsUsed.Select(t => t.Name).Should().Equal("collect_product_offers");
    }

    [Fact]
    public void ABatch_ShouldBeReadFromTheStream_AndFromTheHistory()
    {
        var batchId = Guid.NewGuid();
        var second = Guid.NewGuid();

        var proposed = (ActionProposedEvent)AgentStreamEvent.Parse(
            AgentEventTypes.ActionProposed,
            $$"""{"actionId":"{{ActionId}}","callId":"c1","toolName":"find_or_add_product","arguments":{},"requiredScope":"pricely.write","batchId":"{{batchId}}"}""")!;
        var conversation = new ConversationDto(
            ConversationId,
            "assistant",
            "Lot",
            DateTime.UtcNow,
            [
                Message(1, "User", "Ajoute ce ciment"),
                Message(2, "Assistant", string.Empty, calls: [new ToolCallDto("c1", "find_or_add_product", false, Json("{}")), new ToolCallDto("c2", "add_web_offer", false, Json("{}"))]),
                Message(3, "Tool", string.Empty, results:
                [
                    new ToolResultDto("c1", "find_or_add_product", Json($$"""{"status":"pending_approval","actionId":"{{ActionId}}"}"""), null),
                    new ToolResultDto("c2", "add_web_offer", Json($$"""{"status":"pending_approval","actionId":"{{second}}"}"""), null)
                ])
            ],
            [
                new PendingActionDto { Id = ActionId, ToolName = "find_or_add_product", Status = ApiValues.Pending, Arguments = Json("{}"), RequiredScope = "pricely.write", BatchId = batchId },
                new PendingActionDto { Id = second, ToolName = "add_web_offer", Status = ApiValues.Pending, Arguments = Json("{}"), RequiredScope = "pricely.write", BatchId = batchId }
            ]);

        var view = ConversationView.FromHistory(conversation);

        proposed.BatchId.Should().Be(batchId);
        ((AgentEntry)view.Entries[1]).Actions.Should().HaveCount(2).And.OnlyContain(a => a.BatchId == batchId);
    }

    private static ConversationMessageDto Message(
        int sequence,
        string role,
        string text,
        IReadOnlyList<ToolCallDto>? calls = null,
        IReadOnlyList<ToolResultDto>? results = null) =>
        new(sequence, role, text, calls ?? [], results ?? [], DateTime.UtcNow);

    private static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);

        return document.RootElement.Clone();
    }
}
