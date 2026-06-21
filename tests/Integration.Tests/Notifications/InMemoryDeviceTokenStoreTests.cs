using FluentAssertions;
using Infrastructure.Notifications;
using Xunit;

namespace Integration.Tests.Notifications;

/// <summary>
/// Upsert semantics of the device-token store: registration is keyed by the token itself, so a device
/// re-registering (or the same install signing in as a different learner) refreshes rather than
/// duplicating, and invalid tokens can be pruned.
/// </summary>
public class InMemoryDeviceTokenStoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 14, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task Re_registering_the_same_token_does_not_duplicate()
    {
        var store = new InMemoryDeviceTokenStore();
        var learner = Guid.NewGuid();

        await store.UpsertAsync(learner, "tok", "android", Now, default);
        await store.UpsertAsync(learner, "tok", "android", Now, default);

        (await store.GetAllAsync(default)).Should().ContainSingle();
    }

    [Fact]
    public async Task Same_token_signing_in_as_another_learner_is_re_pointed()
    {
        var store = new InMemoryDeviceTokenStore();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        await store.UpsertAsync(first, "tok", "android", Now, default);
        await store.UpsertAsync(second, "tok", "android", Now, default);

        (await store.GetForUsersAsync(new[] { first }, default)).Should().BeEmpty();
        (await store.GetForUsersAsync(new[] { second }, default)).Should().ContainSingle();
    }

    [Fact]
    public async Task Removed_tokens_are_gone()
    {
        var store = new InMemoryDeviceTokenStore();
        var learner = Guid.NewGuid();
        await store.UpsertAsync(learner, "tok", "android", Now, default);

        await store.RemoveAsync(new[] { "tok" }, default);

        (await store.GetAllAsync(default)).Should().BeEmpty();
    }
}
