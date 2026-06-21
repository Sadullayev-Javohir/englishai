using Application.Identity.Dtos;
using Domain.Identity;
using FluentAssertions;
using Infrastructure.Identity;
using Xunit;

namespace Integration.Tests.Identity;

/// <summary>
/// Verifies the admin-role resolution (<see cref="AdminAuthorization"/>): the super-admin comes from
/// the email allowlist (built-in + config), an ordinary admin from the persisted account flag, and the
/// email match is resolved from the stored account so it cannot be spoofed (docs/development-guide.md §13).
/// </summary>
public class AdminAuthorizationTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 27, 9, 0, 0, TimeSpan.Zero);
    private const string BuiltInSuperAdmin = "javohirsadullayev836@gmail.com";

    private static async Task<UserAccount> SeedAsync(
        InMemoryUserAccountStore store, string email, bool isAdmin = false)
    {
        var account = UserAccount.Register($"sub-{email}", email, "Test", pictureUrl: null, Now);
        if (isAdmin) account.SetAdmin(true);
        await store.AddAsync(account, CancellationToken.None);
        return account;
    }

    [Fact]
    public async Task The_built_in_email_resolves_to_super_admin()
    {
        var store = new InMemoryUserAccountStore();
        var account = await SeedAsync(store, BuiltInSuperAdmin.ToUpperInvariant());
        var sut = new AdminAuthorization(store, new SuperAdminOptions());

        (await sut.GetRoleAsync(account.Id, CancellationToken.None)).Should().Be(AdminRole.SuperAdmin);
    }

    [Fact]
    public async Task A_flagged_account_resolves_to_admin()
    {
        var store = new InMemoryUserAccountStore();
        var account = await SeedAsync(store, "admin@example.com", isAdmin: true);
        var sut = new AdminAuthorization(store, new SuperAdminOptions());

        (await sut.GetRoleAsync(account.Id, CancellationToken.None)).Should().Be(AdminRole.Admin);
    }

    [Fact]
    public async Task An_ordinary_account_resolves_to_none()
    {
        var store = new InMemoryUserAccountStore();
        var account = await SeedAsync(store, "learner@example.com");
        var sut = new AdminAuthorization(store, new SuperAdminOptions());

        (await sut.GetRoleAsync(account.Id, CancellationToken.None)).Should().Be(AdminRole.None);
    }

    [Fact]
    public async Task A_configured_email_is_also_a_super_admin()
    {
        var store = new InMemoryUserAccountStore();
        var account = await SeedAsync(store, "configured@example.com");
        var options = new SuperAdminOptions { Emails = new[] { "configured@example.com" } };
        var sut = new AdminAuthorization(store, options);

        (await sut.GetRoleAsync(account.Id, CancellationToken.None)).Should().Be(AdminRole.SuperAdmin);
        sut.IsSuperAdminEmail("configured@example.com").Should().BeTrue();
    }

    [Fact]
    public async Task An_unknown_account_resolves_to_none()
    {
        var sut = new AdminAuthorization(new InMemoryUserAccountStore(), new SuperAdminOptions());

        (await sut.GetRoleAsync(Guid.NewGuid(), CancellationToken.None)).Should().Be(AdminRole.None);
    }
}
