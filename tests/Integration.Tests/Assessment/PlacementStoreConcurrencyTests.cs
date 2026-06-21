using Application.Common;
using Domain.Assessment;
using FluentAssertions;
using Infrastructure.Assessment;
using Xunit;

namespace Integration.Tests.Assessment;

public class PlacementStoreConcurrencyTests
{
    [Fact]
    public async Task A_stale_answer_cannot_erase_a_new_integrity_incident()
    {
        var store = new InMemoryPlacementSessionStore();
        var session = PlacementTestSession.Start(Guid.NewGuid(), true);
        var question = Guid.NewGuid();
        session.ServeItem(question);
        await store.SaveAsync(session);
        var answer = (await store.GetAsync(session.Id))!;
        var integrity = (await store.GetAsync(session.Id))!;
        integrity.RecordIntegrityViolation(Guid.NewGuid());
        await store.SaveAsync(integrity);
        answer.RecordAnswer(question, true);

        var save = () => store.SaveAsync(answer);
        await save.Should().ThrowAsync<ConflictException>();
        var current = (await store.GetAsync(session.Id))!;
        current.IntegrityViolationCount.Should().Be(1);
        current.CurrentItemId.Should().Be(question);
        current.CompletedItemCount.Should().Be(0);
    }

    [Fact]
    public async Task Two_concurrent_answers_cannot_increment_progress_twice()
    {
        var store = new InMemoryPlacementSessionStore();
        var session = PlacementTestSession.Start(Guid.NewGuid(), true);
        var question = Guid.NewGuid();
        session.ServeItem(question);
        await store.SaveAsync(session);
        var first = (await store.GetAsync(session.Id))!;
        var second = (await store.GetAsync(session.Id))!;
        first.RecordAnswer(question, true);
        second.RecordAnswer(question, false);
        await store.SaveAsync(first);

        var save = () => store.SaveAsync(second);
        await save.Should().ThrowAsync<ConflictException>();
        (await store.GetAsync(session.Id))!.CompletedItemCount.Should().Be(1);
    }

    [Fact]
    public async Task Mutating_a_read_does_not_change_persisted_state_before_save()
    {
        var store = new InMemoryPlacementSessionStore();
        var session = PlacementTestSession.Start(Guid.NewGuid(), true);
        await store.SaveAsync(session);
        var fetched = (await store.GetAsync(session.Id))!;
        fetched.RecordIntegrityViolation(Guid.NewGuid());
        (await store.GetAsync(session.Id))!.IntegrityViolationCount.Should().Be(0);
    }
}
