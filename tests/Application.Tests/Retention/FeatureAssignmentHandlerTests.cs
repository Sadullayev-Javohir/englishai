using Application.Retention.GetFeatureAssignment;
using Application.Retention.GetFeatureAssignments;
using Application.Retention.Ports;
using Domain.Retention;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Retention;

/// <summary>
/// Unit tests for the feature-flag assignment queries (PROJECT-SPEC I.3).
/// </summary>
public class FeatureAssignmentHandlerTests
{
    private static readonly Guid Learner = Guid.NewGuid();

    private readonly IFeatureFlagRepository _flags = Substitute.For<IFeatureFlagRepository>();

    private static FeatureFlag GoalSizeFlag() => new(
        "daily_goal_size",
        "3 vs 5",
        enabled: true,
        new[]
        {
            new FeatureVariant("control", 1, "3"),
            new FeatureVariant("variant_five", 1, "5")
        });

    [Fact]
    public async Task Single_assignment_resolves_the_learners_variant()
    {
        _flags.GetByKeyAsync("daily_goal_size", Arg.Any<CancellationToken>()).Returns(GoalSizeFlag());

        var handler = new GetFeatureAssignmentQueryHandler(_flags);
        var result = await handler.Handle(new GetFeatureAssignmentQuery(Learner, "daily_goal_size"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Key.Should().Be("daily_goal_size");
        result.Variant.Should().BeOneOf("control", "variant_five");
        result.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task An_unknown_flag_key_returns_null()
    {
        _flags.GetByKeyAsync("missing", Arg.Any<CancellationToken>()).Returns((FeatureFlag?)null);

        var handler = new GetFeatureAssignmentQueryHandler(_flags);
        var result = await handler.Handle(new GetFeatureAssignmentQuery(Learner, "missing"), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task All_assignments_returns_one_entry_per_flag()
    {
        _flags.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<FeatureFlag> { GoalSizeFlag() });

        var handler = new GetFeatureAssignmentsQueryHandler(_flags);
        var result = await handler.Handle(new GetFeatureAssignmentsQuery(Learner), CancellationToken.None);

        result.Should().ContainSingle().Which.Key.Should().Be("daily_goal_size");
    }
}
