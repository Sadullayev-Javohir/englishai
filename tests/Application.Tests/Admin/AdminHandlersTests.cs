using Application.Admin.GetAdminUsers;
using Application.Admin.SetUserAdmin;
using Application.Common;
using Application.Grammar.Admin.GetGrammarLesson;
using Application.Grammar.Ports;
using Application.Identity.Dtos;
using Application.Identity.Ports;
using Application.Learning.Ports;
using Application.Subscription.Ports;
using Domain.Assessment;
using Domain.Identity;
using Domain.Grammar;
using Domain.Learning;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Admin;

/// <summary>
/// Covers the admin panel handler guards: only admins may list users, only the super-admin may
/// promote/demote, a super-admin's grant is immutable, and the user rows are labelled with the right
/// role. <see cref="IAdminAuthorization"/> is substituted (its own allowlist logic is covered by an
/// Infrastructure test) so these stay focused on the handler behaviour.
/// </summary>
public class AdminHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 27, 9, 0, 0, TimeSpan.Zero);

    private readonly IAdminAuthorization _admin = Substitute.For<IAdminAuthorization>();
    private readonly IUserAccountStore _accounts = Substitute.For<IUserAccountStore>();
    private readonly ILearnerProfileRepository _profiles = Substitute.For<ILearnerProfileRepository>();
    private readonly ISubscriptionRepository _subscriptions = Substitute.For<ISubscriptionRepository>();
    private readonly IGrammarRepository _grammarLessons = Substitute.For<IGrammarRepository>();

    public AdminHandlersTests()
    {
        _profiles.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Domain.Learning.LearnerProfile>());
        _subscriptions.GetPaidAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Domain.Subscription.Subscription>());
        _accounts.CountAsync(Arg.Any<CancellationToken>()).Returns(0);
        // Default: no email is a super-admin unless a test says so.
        _admin.IsSuperAdminEmail(Arg.Any<string>()).Returns(false);
    }

    private UserAccount Given(string email, AdminRole role = AdminRole.None)
    {
        var account = UserAccount.Register($"sub-{email}", email, "User", null, Now);
        if (role is AdminRole.Admin) account.SetAdmin(true);
        _accounts.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _admin.GetRoleAsync(account.Id, Arg.Any<CancellationToken>()).Returns(role);
        if (role is AdminRole.SuperAdmin) _admin.IsSuperAdminEmail(email).Returns(true);
        return account;
    }

    private GetAdminUsersQueryHandler UsersHandler() =>
        new(_admin, _accounts, _profiles, _subscriptions);

    private SetUserAdminCommandHandler SetHandler() => new(_admin, _accounts);

    private GetGrammarLessonAdminQueryHandler GrammarLessonHandler() => new(_admin, _grammarLessons);

    [Fact]
    public async Task Listing_users_as_a_non_admin_is_forbidden()
    {
        var learner = Given("learner@example.com");

        var act = () => UsersHandler().Handle(new GetAdminUsersQuery(learner.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Listing_users_labels_each_role_and_counts_admins()
    {
        var superAdmin = Given("super@example.com", AdminRole.SuperAdmin);
        var admin = Given("admin@example.com", AdminRole.Admin);
        var learner = Given("learner@example.com");
        _accounts.GetPageAsync(null, null, 51, Arg.Any<CancellationToken>())
            .Returns(new[] { superAdmin, admin, learner });
        _accounts.CountAsync(Arg.Any<CancellationToken>()).Returns(3);

        var result = await UsersHandler().Handle(
            new GetAdminUsersQuery(superAdmin.Id), CancellationToken.None);

        result.ViewerRole.Should().Be(AdminRole.SuperAdmin);
        result.TotalUsers.Should().Be(3);
        result.AdminCount.Should().Be(2); // super-admin + admin, not the plain learner
        result.Users.Single(u => u.Email == "super@example.com").Role.Should().Be(AdminRole.SuperAdmin);
        result.Users.Single(u => u.Email == "admin@example.com").Role.Should().Be(AdminRole.Admin);
        result.Users.Single(u => u.Email == "learner@example.com").Role.Should().Be(AdminRole.None);
    }

    [Fact]
    public async Task Listing_users_reports_the_full_count_when_the_page_is_bounded()
    {
        var superAdmin = Given("super@example.com", AdminRole.SuperAdmin);
        var page = Enumerable.Range(0, 50)
            .Select(index => Given($"learner-{index}@example.com"))
            .ToArray();
        _accounts.GetPageAsync(null, null, 51, Arg.Any<CancellationToken>())
            .Returns(page);
        _accounts.CountAsync(Arg.Any<CancellationToken>()).Returns(59);

        var result = await UsersHandler().Handle(
            new GetAdminUsersQuery(superAdmin.Id), CancellationToken.None);

        result.Users.Should().HaveCount(50);
        result.TotalUsers.Should().Be(59);
    }

    [Fact]
    public async Task An_ordinary_admin_cannot_promote_other_users()
    {
        var admin = Given("admin@example.com", AdminRole.Admin);
        var target = Given("target@example.com");

        var act = () => SetHandler().Handle(
            new SetUserAdminCommand(admin.Id, target.Id, IsAdmin: true), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        await _accounts.DidNotReceive().UpdateAsync(target, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_super_admin_promotes_a_learner_and_persists()
    {
        var superAdmin = Given("super@example.com", AdminRole.SuperAdmin);
        var target = Given("target@example.com");

        var result = await SetHandler().Handle(
            new SetUserAdminCommand(superAdmin.Id, target.Id, IsAdmin: true), CancellationToken.None);

        result.Role.Should().Be(AdminRole.Admin);
        target.IsAdmin.Should().BeTrue();
        await _accounts.Received(1).UpdateAsync(target, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_super_admin_can_demote_an_admin_back_to_a_learner()
    {
        var superAdmin = Given("super@example.com", AdminRole.SuperAdmin);
        var target = Given("target@example.com", AdminRole.Admin);

        var result = await SetHandler().Handle(
            new SetUserAdminCommand(superAdmin.Id, target.Id, IsAdmin: false), CancellationToken.None);

        result.Role.Should().Be(AdminRole.None);
        target.IsAdmin.Should().BeFalse();
    }

    [Fact]
    public async Task A_super_admins_access_cannot_be_changed()
    {
        var superAdmin = Given("super@example.com", AdminRole.SuperAdmin);
        var otherSuper = Given("other-super@example.com", AdminRole.SuperAdmin);

        var act = () => SetHandler().Handle(
            new SetUserAdminCommand(superAdmin.Id, otherSuper.Id, IsAdmin: false), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Grammar_lesson_detail_returns_partial_content_without_writing_on_read()
    {
        var admin = Given("admin@example.com", AdminRole.Admin);
        var topicId = Guid.NewGuid();
        var lesson = GrammarLesson.ForTopic(
            topicId, "The Museum", ErrorCategory.Other, CefrLevel.A1, "articles", Now);
        _grammarLessons.GetByIdAsync(lesson.Id, Arg.Any<CancellationToken>()).Returns(lesson);

        var result = await GrammarLessonHandler().Handle(
            new GetGrammarLessonAdminQuery(admin.Id, lesson.Id), CancellationToken.None);

        result.Id.Should().Be(lesson.Id);
        result.Status.Should().Be("Pending");
        result.Examples.Should().BeEmpty();
        result.CommonMistakes.Should().BeEmpty();
        await _grammarLessons.DidNotReceive().SaveAsync(
            Arg.Any<GrammarLesson>(), Arg.Any<CancellationToken>());
    }
}
