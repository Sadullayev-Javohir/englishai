using Application.Admin.Dtos;
using Application.Common;
using Application.Identity.Dtos;
using Application.Identity.Ports;
using Application.Learning.Ports;
using Application.Learning.GetProgressInsight;
using Application.Learning.Dtos;
using Application.Notifications.Ports;
using Application.Retention.EvaluateChurnRisk;
using Application.Retention.Dtos;
using Application.Subscription.Ports;
using Domain.Identity;
using Domain.Subscription;
using MediatR;

namespace Application.Admin.GetAdminUserDetail;

public sealed class GetAdminUserDetailQueryHandler : IRequestHandler<GetAdminUserDetailQuery, AdminUserDetailDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly IUserAccountStore _accounts;
    private readonly ILearnerProfileRepository _profiles;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly TimeProvider _time;
    private readonly ISender _sender;
    private readonly IDeviceTokenStore _devices;

    public GetAdminUserDetailQueryHandler(IAdminAuthorization admin, IUserAccountStore accounts, ILearnerProfileRepository profiles, ISubscriptionRepository subscriptions, TimeProvider time, ISender sender, IDeviceTokenStore devices)
    {
        _admin = admin;
        _accounts = accounts;
        _profiles = profiles;
        _subscriptions = subscriptions;
        _time = time;
        _sender = sender;
        _devices = devices;
    }

    public async Task<AdminUserDetailDto> Handle(GetAdminUserDetailQuery request, CancellationToken cancellationToken)
    {
        if (await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken) is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var account = await _accounts.GetByIdAsync(request.TargetUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserAccount), request.TargetUserId);
        var profile = await _profiles.GetByLearnerIdAsync(account.Id, cancellationToken);
        var subscription = await _subscriptions.GetByLearnerIdAsync(account.Id, cancellationToken);
        var role = _admin.IsSuperAdminEmail(account.Email) ? AdminRole.SuperAdmin : account.IsAdmin ? AdminRole.Admin : AdminRole.None;
        var today = DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime);
        var progress = await BuildProgressAsync(account.Id, today, cancellationToken);
        var churn = await BuildChurnAsync(account.Id, cancellationToken);
        var devices = await _devices.GetForUsersAsync(new[] { account.Id }, cancellationToken);
        var learning = new AdminUserLearningDto(
            progress.Snapshot.StudyTime,
            progress.Snapshot.Gamification,
            progress.Snapshot.Skills,
            progress.Snapshot.ErrorsLast30Days,
            progress.Snapshot.TopicProgress,
            progress.Snapshot.Vocabulary,
            churn,
            devices.Count,
            devices.Select(device => device.Platform).Distinct().Order().ToList());

        return new AdminUserDetailDto(
            account.Id, account.Email, account.DisplayName, account.PreferredName, account.Username,
            account.PictureUrl, role, account.CreatedAt, account.LastLoginAt, account.ProTrialExpiresAt,
            account.IsProTrialActive(_time.GetUtcNow()), profile is not null, profile?.OverallLevel.ToString(),
            profile?.LearningGoal.ToString(), account.BirthDate, account.Gender?.ToString(),
            account.AcquisitionSource?.ToString(), account.AcquisitionSourceOther,
            profile?.CreatedAt, profile?.UpdatedAt, profile?.LastActivityAt,
            profile?.ConfirmationTestPassedAt, profile?.LastWinBackStage.ToString(), profile?.Seeds.Count ?? 0,
            profile?.Activities.Count ?? 0, profile?.Errors.Count ?? 0,
            (subscription?.Status ?? SubscriptionStatus.Free).ToString(), subscription?.Plan?.ToString(),
            subscription?.ExpiresAt, subscription?.CreatedAt, subscription?.UpdatedAt, learning);
    }

    private async Task<ProgressInsightDto> BuildProgressAsync(Guid learnerId, DateOnly today, CancellationToken cancellationToken)
    {
        using var scope = OwnershipBypass.Enter();
        return await _sender.Send(new GetProgressInsightQuery(learnerId, today), cancellationToken);
    }

    private async Task<ChurnAssessmentDto> BuildChurnAsync(Guid learnerId, CancellationToken cancellationToken)
    {
        using var scope = OwnershipBypass.Enter();
        return await _sender.Send(new EvaluateChurnRiskQuery(learnerId), cancellationToken);
    }
}
