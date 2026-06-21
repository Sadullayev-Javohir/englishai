using Domain.Identity;
using FluentAssertions;
using Infrastructure.Identity;
using Xunit;

namespace Integration.Tests.Identity;

/// <summary>
/// Verifies the email-allowlist grant of full access (<see cref="ComplimentaryAccess"/>). The
/// grant must apply only to the exact allowlisted email, resolved from the persisted account -
/// no other learner (however they craft their request) can unlock anything (docs/development-guide.md §13).
/// </summary>
public class ComplimentaryAccessTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 26, 9, 0, 0, TimeSpan.Zero);
    private const string CompedEmail = "javohirsadullayev836@gmail.com";

    private static async Task<Guid> SeedAsync(InMemoryUserAccountStore store, string email)
    {
        var account = UserAccount.Register($"sub-{email}", email, "Test", pictureUrl: null, Now);
        await store.AddAsync(account, CancellationToken.None);
        return account.Id;
    }

    [Fact]
    public async Task The_allowlisted_email_is_granted_full_access()
    {
        var store = new InMemoryUserAccountStore();
        var learnerId = await SeedAsync(store, CompedEmail);
        var sut = new ComplimentaryAccess(store, new ComplimentaryAccessOptions());

        (await sut.HasFullAccessAsync(learnerId, CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task The_match_is_case_and_whitespace_insensitive()
    {
        var store = new InMemoryUserAccountStore();
        var learnerId = await SeedAsync(store, $"  {CompedEmail.ToUpperInvariant()}  ");
        var sut = new ComplimentaryAccess(store, new ComplimentaryAccessOptions());

        (await sut.HasFullAccessAsync(learnerId, CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task Any_other_email_is_denied()
    {
        var store = new InMemoryUserAccountStore();
        var learnerId = await SeedAsync(store, "someone.else@gmail.com");
        var sut = new ComplimentaryAccess(store, new ComplimentaryAccessOptions());

        (await sut.HasFullAccessAsync(learnerId, CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task An_unknown_account_is_denied()
    {
        var sut = new ComplimentaryAccess(new InMemoryUserAccountStore(), new ComplimentaryAccessOptions());

        (await sut.HasFullAccessAsync(Guid.NewGuid(), CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task Extra_emails_from_configuration_are_also_granted()
    {
        var store = new InMemoryUserAccountStore();
        var learnerId = await SeedAsync(store, "configured@example.com");
        var options = new ComplimentaryAccessOptions { Emails = new[] { "configured@example.com" } };
        var sut = new ComplimentaryAccess(store, options);

        (await sut.HasFullAccessAsync(learnerId, CancellationToken.None)).Should().BeTrue();
    }
}
