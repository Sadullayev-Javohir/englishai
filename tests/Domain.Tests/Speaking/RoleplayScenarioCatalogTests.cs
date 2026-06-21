using Domain.Assessment;
using Domain.Common;
using Domain.Speaking;
using FluentAssertions;

namespace Domain.Tests.Speaking;

public class RoleplayScenarioCatalogTests
{
    [Fact]
    public void Catalog_holds_exactly_20_scenarios_for_every_level_120_in_total()
    {
        RoleplayScenarioCatalog.All.Should().HaveCount(RoleplayScenarioCatalog.TotalScenarios);
        RoleplayScenarioCatalog.TotalScenarios.Should().Be(120);

        foreach (var level in Enum.GetValues<CefrLevel>())
        {
            RoleplayScenarioCatalog.ForLevel(level)
                .Should().HaveCount(
                    RoleplayScenarioCatalog.ScenariosPerLevel,
                    $"level {level} must offer exactly 20 roleplay scenarios");
        }
    }

    [Fact]
    public void All_is_ordered_by_level_easiest_first()
    {
        RoleplayScenarioCatalog.All.Select(s => (int)s.Level)
            .Should().BeInAscendingOrder();
    }

    [Fact]
    public void Every_scenario_has_complete_persona_data()
    {
        foreach (var scenario in RoleplayScenarioCatalog.All)
        {
            scenario.Code.Should().NotBeNullOrWhiteSpace();
            scenario.Code.Should().MatchRegex(
                "^[a-z0-9_]+$", "codes are stable snake_case keys for the Uzbek content store");
            scenario.EnglishTitle.Should().NotBeNullOrWhiteSpace();
            scenario.PersonaRole.Should().NotBeNullOrWhiteSpace();
            scenario.Setting.Should().NotBeNullOrWhiteSpace();
            scenario.LearnerObjective.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Scenario_codes_are_unique()
    {
        RoleplayScenarioCatalog.All.Select(s => s.Code)
            .Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Image_ids_are_unique_and_deterministic()
    {
        RoleplayScenarioCatalog.All.Select(s => s.ImageId)
            .Should().OnlyHaveUniqueItems()
            .And.NotContain(Guid.Empty);

        // Deterministic: the same code always derives the same image id (galleries survive reseeds).
        var a = new RoleplayScenarioDefinition(
            "airport", "T", "p", "s", "o", CefrLevel.A1);
        var b = new RoleplayScenarioDefinition(
            "airport", "Other title", "other persona", "other setting", "other objective", CefrLevel.B2);
        a.ImageId.Should().Be(b.ImageId);
    }

    [Fact]
    public void Get_resolves_a_known_code_case_insensitively()
    {
        RoleplayScenarioCatalog.Get("job_interview").EnglishTitle.Should().Be("Job Interview");
        RoleplayScenarioCatalog.Get("JOB_INTERVIEW").Code.Should().Be("job_interview");
    }

    [Fact]
    public void Get_throws_for_an_unknown_code()
    {
        var act = () => RoleplayScenarioCatalog.Get("no_such_scene");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Contains_accepts_known_codes_and_rejects_unknown_or_blank()
    {
        RoleplayScenarioCatalog.Contains("restaurant").Should().BeTrue();
        RoleplayScenarioCatalog.Contains("no_such_scene").Should().BeFalse();
        RoleplayScenarioCatalog.Contains("").Should().BeFalse();
        RoleplayScenarioCatalog.Contains(null).Should().BeFalse();
    }
}

public class ConversationSessionRoleplayTests
{
    [Fact]
    public void A_session_started_with_a_scenario_is_a_roleplay()
    {
        var session = ConversationSession.Start(
            Guid.NewGuid(), CefrLevel.A2, scenarioCode: "restaurant");

        session.IsRoleplay.Should().BeTrue();
        session.ScenarioCode.Should().Be("restaurant");
    }

    [Fact]
    public void A_session_without_a_scenario_is_not_a_roleplay()
    {
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2);

        session.IsRoleplay.Should().BeFalse();
        session.ScenarioCode.Should().BeNull();
    }

    [Fact]
    public void A_blank_scenario_code_is_normalized_to_no_roleplay()
    {
        var session = ConversationSession.Start(
            Guid.NewGuid(), CefrLevel.A2, scenarioCode: "   ");

        session.IsRoleplay.Should().BeFalse();
    }

    [Fact]
    public void LearnerTurnCount_counts_only_learner_turns()
    {
        var session = ConversationSession.Start(
            Guid.NewGuid(), CefrLevel.A2, scenarioCode: "airport");
        session.AddTutorTurn("May I see your passport?");
        session.AddLearnerTurn("Here it is.");
        session.AddTutorTurn("Thank you.");
        session.AddLearnerTurn("Can I have a window seat?");

        session.LearnerTurnCount.Should().Be(2);
    }
}
