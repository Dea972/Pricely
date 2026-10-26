using FluentAssertions;
using Pricely.Modules.AiEngine.Domain.AgentActivities;

namespace Pricely.Modules.AiEngine.UnitTests.AgentActivities;

public class AgentActivityTests
{
    private static readonly DateTime Now = new(2026, 10, 26, 10, 0, 0, DateTimeKind.Utc);

    private static AgentActivityDetails Details => new(Guid.NewGuid(), Now, "conversation-1", "agents.assistant", "assistant", "collect_product_offers", "user-1");

    [Fact]
    public void ToolInvocation_ShouldRecordItsOutcome()
    {
        var failed = AgentActivity.RecordToolInvocation(Details, isReadOnly: false, wasApproved: true, 120, "boom");

        failed.Kind.Should().Be(AgentActivityKind.ToolInvoked);
        failed.IsSuccess.Should().BeFalse();
        failed.WasApproved.Should().BeTrue();
        failed.DurationMs.Should().Be(120);
        failed.ErrorMessage.Should().Be("boom");
    }

    [Theory]
    [InlineData(AgentActionDecision.Executed, true, true)]
    [InlineData(AgentActionDecision.Failed, true, false)]
    [InlineData(AgentActionDecision.Rejected, false, true)]
    public void Decision_ShouldRecordTheApprovalAndTheDelay(AgentActionDecision decision, bool approved, bool success)
    {
        var actionId = Guid.NewGuid();

        var activity = AgentActivity.RecordDecision(Details, actionId, decision, Now.AddMinutes(-2));

        activity.Kind.Should().Be(AgentActivityKind.ActionDecided);
        activity.ActionId.Should().Be(actionId);
        activity.WasApproved.Should().Be(approved);
        activity.IsSuccess.Should().Be(success);
        activity.DecisionDelayMs.Should().Be(120_000);
    }

    [Fact]
    public void LongValues_ShouldBeCut_NotLost()
    {
        var activity = AgentActivity.RecordToolInvocation(Details, true, false, 1, new string('x', 5000));

        activity.ErrorMessage.Should().HaveLength(AgentActivity.ErrorMessageMaxLength);
    }
}
