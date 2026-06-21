using Application.Admin.Dtos;
using Application.Common;
using Application.Identity.Dtos;
using Application.Identity.Ports;
using Application.Learning.Ports;
using Application.Subscription.Ports;
using Domain.Identity;
using Domain.Learning;
using Domain.Subscription;
using MediatR;

namespace Application.Admin.GetAdminUsers;

/// <summary>
/// Assembles the admin panel's user list. Authorizes the viewer as an admin (403 otherwise), then
/// joins each account with its learner profile (onboarding, CEFR level, last activity) and its
/// subscription standing. The joins are bulk reads (all profiles / all paid subscriptions) keyed into
/// dictionaries, so the list is built without a per-user round-trip (no N+1).
/// </summary>
public sealed class GetAdminUsersQueryHandler : IRequestHandler<GetAdminUsersQuery, AdminUsersDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly IUserAccountStore _accounts;
    private readonly ILearnerProfileRepository _profiles;
    private readonly ISubscriptionRepository _subscriptions;

    public GetAdminUsersQueryHandler(
        IAdminAuthorization admin,
        IUserAccountStore accounts,
        ILearnerProfileRepository profiles,
        ISubscriptionRepository subscriptions)
    {
        _admin = admin;
        _accounts = accounts;
        _profiles = profiles;
        _subscriptions = subscriptions;
    }

    public async Task<AdminUsersDto> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken)
    {
        var viewerRole = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (viewerRole is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var totalUsers = await _accounts.CountAsync(cancellationToken);
        var cursor = StableCursor.Decode(request.Cursor);
        var accounts = await _accounts.GetPageAsync(
            cursor?.Timestamp, cursor?.Id, request.PageSize + 1, cancellationToken);
        var hasMore = accounts.Count > request.PageSize;
        var pageAccounts = accounts.Take(request.PageSize).ToArray();
        var accountIds = pageAccounts.Select(account => account.Id).ToHashSet();

        var profiles = (await _profiles.GetAllAsync(cancellationToken))
            .Where(p => accountIds.Contains(p.LearnerId))
            .ToDictionary(p => p.LearnerId);

        // Only paid subscriptions are bulk-readable; everyone else is on the free tier by default.
        var paidSubscriptions = (await _subscriptions.GetPaidAsync(cancellationToken))
            .Where(s => accountIds.Contains(s.LearnerId))
            .GroupBy(s => s.LearnerId)
            .ToDictionary(g => g.Key, g => g.First());

        var users = pageAccounts
            .Select(account => ToDto(
                account,
                profiles.GetValueOrDefault(account.Id),
                paidSubscriptions.GetValueOrDefault(account.Id)))
            .ToList();

        return new AdminUsersDto(
            ViewerRole: viewerRole,
            TotalUsers: totalUsers,
            AdminCount: users.Count(u => u.Role is not AdminRole.None),
            OnboardedCount: users.Count(u => u.HasOnboarded),
            PremiumCount: users.Count(u => u.SubscriptionStatus == nameof(SubscriptionStatus.Premium)),
            Users: users,
            NextCursor: hasMore
                ? new StableCursor(pageAccounts[^1].CreatedAt, pageAccounts[^1].Id).Encode()
                : null);
    }

    private AdminUserDto ToDto(
        UserAccount account, LearnerProfile? profile, Domain.Subscription.Subscription? subscription)
    {
        var role = _admin.IsSuperAdminEmail(account.Email)
            ? AdminRole.SuperAdmin
            : account.IsAdmin ? AdminRole.Admin : AdminRole.None;

        return new AdminUserDto(
            Id: account.Id,
            Email: account.Email,
            DisplayName: account.DisplayName,
            Username: account.Username,
            PictureUrl: account.PictureUrl,
            Role: role,
            RegisteredAt: account.CreatedAt,
            LastLoginAt: account.LastLoginAt,
            HasOnboarded: profile is not null,
            Level: profile?.OverallLevel.ToString(),
            LastActivityAt: profile?.LastActivityAt,
            SubscriptionStatus: (subscription?.Status ?? SubscriptionStatus.Free).ToString(),
            SubscriptionPlan: subscription?.Plan?.ToString(),
            SubscriptionExpiresAt: subscription?.ExpiresAt);
    }
}
