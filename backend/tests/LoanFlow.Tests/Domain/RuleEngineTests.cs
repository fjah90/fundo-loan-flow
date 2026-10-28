using LoanFlow.Domain;
using Xunit;

namespace LoanFlow.Tests.Domain;

public class RuleEngineTests
{
    private const string BlacklistedSsn = "111-11-1111";
    private const string ApprovedSsn = "555-12-3456";

    [Theory]
    [InlineData("ny")]
    [InlineData("NY")]
    [InlineData(" Ny ")]
    public void Evaluate_NewYorkInAnyCase_DeniesWithStateNotServed(string state)
    {
        // Arrange
        var engine = CreateEngine();
        var applicant = Applicant(state, ApprovedSsn);

        // Act
        var decision = engine.Evaluate(applicant);

        // Assert
        Assert.False(decision.IsApproved);
        Assert.Equal([DenialReason.StateNotServed], decision.Reasons);
    }

    [Fact]
    public void Evaluate_BlacklistedSsn_DeniesWithNotEligible()
    {
        // Arrange
        var engine = CreateEngine();
        var applicant = Applicant("CA", BlacklistedSsn);

        // Act
        var decision = engine.Evaluate(applicant);

        // Assert
        Assert.False(decision.IsApproved);
        Assert.Equal([DenialReason.NotEligible], decision.Reasons);
    }

    [Fact]
    public void Evaluate_NewYorkAndBlacklistedSsn_ReturnsBothReasonsInRegistrationOrder()
    {
        // Arrange
        var engine = CreateEngine();
        var applicant = Applicant("NY", BlacklistedSsn);

        // Act
        var decision = engine.Evaluate(applicant);

        // Assert
        Assert.False(decision.IsApproved);
        Assert.Equal([DenialReason.StateNotServed, DenialReason.NotEligible], decision.Reasons);
    }

    [Fact]
    public void Evaluate_NoRuleMatches_ReturnsApprovedWithNoReasons()
    {
        // Arrange
        var engine = CreateEngine();
        var applicant = Applicant("CA", ApprovedSsn);

        // Act
        var decision = engine.Evaluate(applicant);

        // Assert
        Assert.True(decision.IsApproved);
        Assert.Empty(decision.Reasons);
    }

    [Fact]
    public void Evaluate_NewRuleRegistered_AddsItsReasonWithoutTouchingExistingRules()
    {
        // Arrange
        var extra = new DenialReason("TEST_EXTRA");
        var engine = new RuleEngine(
        [
            new NewYorkStateRule(),
            new BlacklistedSsnRule(new FakeSsnBlacklist()),
            new FixedRule(extra),
        ]);
        var applicant = Applicant("NY", ApprovedSsn);

        // Act
        var decision = engine.Evaluate(applicant);

        // Assert
        Assert.False(decision.IsApproved);
        Assert.Equal([DenialReason.StateNotServed, extra], decision.Reasons);
    }

    [Fact]
    public void Evaluate_EarlierRuleAlreadyDenied_StillCallsEveryRule()
    {
        // Arrange
        var first = new SpyRule(new DenialReason("FIRST"));
        var second = new SpyRule(null);
        var third = new SpyRule(new DenialReason("THIRD"));
        var engine = new RuleEngine([first, second, third]);
        var applicant = Applicant("CA", ApprovedSsn);

        // Act
        var decision = engine.Evaluate(applicant);

        // Assert
        Assert.Equal([1, 1, 1], [first.Calls, second.Calls, third.Calls]);
        Assert.Equal([new DenialReason("FIRST"), new DenialReason("THIRD")], decision.Reasons);
    }

    [Fact]
    public void Denied_EmptyReasons_ThrowsArgumentException()
    {
        // Arrange
        IReadOnlyList<DenialReason> reasons = [];

        // Act
        var act = () => Decision.Denied(reasons);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Denied_SourceListMutatedAfterwards_ReasonsDoNotChange()
    {
        // Arrange
        var list = new List<DenialReason> { DenialReason.StateNotServed };
        var decision = Decision.Denied(list);

        // Act
        list.Add(DenialReason.NotEligible);

        // Assert
        Assert.Equal([DenialReason.StateNotServed], decision.Reasons);
    }

    [Fact]
    public void Constructor_NoRules_ThrowsArgumentException()
    {
        // Arrange
        IDenyRule[] rules = [];

        // Act
        var act = () => new RuleEngine(rules);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    private static RuleEngine CreateEngine() => new(
    [
        new NewYorkStateRule(),
        new BlacklistedSsnRule(new FakeSsnBlacklist(Ssn.Parse(BlacklistedSsn))),
    ]);

    private static LoanApplicant Applicant(string state, string ssn) =>
        new(UsState.Parse(state), Ssn.Parse(ssn));

    private sealed class FakeSsnBlacklist(params Ssn[] blocked) : ISsnBlacklist
    {
        private readonly HashSet<Ssn> _blocked = [.. blocked];

        public bool Contains(Ssn ssn) => _blocked.Contains(ssn);
    }

    private sealed class FixedRule(DenialReason reason) : IDenyRule
    {
        public DenialReason? Evaluate(LoanApplicant applicant) => reason;
    }

    private sealed class SpyRule(DenialReason? reason) : IDenyRule
    {
        public int Calls { get; private set; }

        public DenialReason? Evaluate(LoanApplicant applicant)
        {
            Calls++;
            return reason;
        }
    }
}
