using FluentAssertions;
using Pricely.Modules.Agentic.Domain.Actions;
using Pricely.Modules.Agentic.Domain.Conversations;
using Pricely.Common.Domain;

namespace Pricely.Modules.Agentic.UnitTests;

public sealed class DomainTests
{
    private static readonly DateTime Now = new(2026, 10, 26, 9, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Start_ShouldRefuseAnEmptyMessage(string message) =>
        AgentConversation.Start("assistant", Guid.NewGuid(), message, Now).Error.Should().Be(AgentConversationErrors.EmptyMessage);

    [Fact]
    public void Start_ShouldTitleTheConversationWithTheFirstMessage_Truncated()
    {
        Result<AgentConversation> conversation = AgentConversation.Start("assistant", Guid.NewGuid(), new string('a', 300), Now);

        conversation.Value.Title.Should().HaveLength(AgentConversation.TitleMaxLength).And.EndWith("…");
    }

    [Fact]
    public void Reject_ShouldOnlyBeAllowedToTheOwner_AndOnce()
    {
        var owner = Guid.NewGuid();
        var action = PendingAction.Propose(Guid.NewGuid(), Guid.NewGuid(), 0, owner, "assistant", new ProposedCall("collect_product_offers", "{}", "pricely.write"), Now);

        action.Reject(Guid.NewGuid(), Now).IsFailure.Should().BeTrue();
        action.Reject(owner, Now).IsSuccess.Should().BeTrue();
        action.Status.Should().Be(PendingActionStatus.Rejected);
        action.Reject(owner, Now).Error.Code.Should().Be("Actions.AlreadyDecided");
    }
}
