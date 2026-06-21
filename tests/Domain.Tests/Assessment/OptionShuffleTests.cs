using Domain.Assessment;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Assessment;

public class OptionShuffleTests
{
    [Fact]
    public void Order_is_a_permutation_of_all_indices()
    {
        var order = OptionShuffle.Order(Guid.NewGuid(), Guid.NewGuid(), 4);

        order.Should().BeEquivalentTo(new[] { 0, 1, 2, 3 });
    }

    [Fact]
    public void Order_is_reproducible_for_the_same_ids()
    {
        var session = Guid.NewGuid();
        var question = Guid.NewGuid();

        OptionShuffle.Order(session, question, 4)
            .Should().Equal(OptionShuffle.Order(session, question, 4));
    }

    [Fact]
    public void Different_sessions_can_get_a_different_order()
    {
        var question = Guid.NewGuid();

        // Across many sessions, at least one order should differ from the identity,
        // i.e. the options really do get shuffled.
        var anyShuffled = Enumerable.Range(0, 20)
            .Select(_ => OptionShuffle.Order(Guid.NewGuid(), question, 4))
            .Any(order => !order.SequenceEqual(new[] { 0, 1, 2, 3 }));

        anyShuffled.Should().BeTrue();
    }

    [Fact]
    public void ToOriginalIndex_inverts_the_displayed_position()
    {
        var session = Guid.NewGuid();
        var question = Guid.NewGuid();
        var order = OptionShuffle.Order(session, question, 4);

        for (var displayPos = 0; displayPos < 4; displayPos++)
        {
            OptionShuffle.ToOriginalIndex(session, question, 4, displayPos)
                .Should().Be(order[displayPos]);
        }
    }
}
