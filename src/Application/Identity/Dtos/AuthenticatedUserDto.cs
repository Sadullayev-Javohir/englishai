using Domain.Common;
using Domain.Identity;
using Domain.Learning;

namespace Application.Identity.Dtos;

/// <summary>
/// The signed-in user as exposed to the SPA. The <see cref="Id"/> is also the learner id
/// the client passes to every learner-scoped endpoint. <see cref="HasOnboarded"/> tells the
/// SPA whether the account already has a learner profile (placement done or a starting level
/// chosen) so it can route a brand-new user into onboarding rather than the dashboard.
/// </summary>
public sealed record AuthenticatedUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? Username,
    string? PictureUrl,
    bool HasOnboarded,
    // The name the AI tutor uses; null until the learner answers the one-time prompt. The SPA
    // shows the "what should I call you?" modal exactly when this is null.
    string? PreferredName,
    // The learner's onboarding goal (goal-based onboarding), read from the learner profile. Unspecified
    // until chosen - the SPA shows the goal screen exactly when this is Unspecified (and the learner has
    // already onboarded). Null profile ⇒ Unspecified, since the goal is asked only after onboarding.
    LearningGoal LearningGoal,
    DateOnly? BirthDate,
    Gender? Gender,
    AcquisitionSource? AcquisitionSource,
    string? AcquisitionSourceOther,
    bool HasCompletedDemographics)
{
    /// <summary>
    /// Projects an account plus its learner profile (or <c>null</c> when none exists yet). Both the
    /// onboarding flag and the learning goal are derived from the profile, so the goal lives on the
    /// same aggregate as the rest of the learner's progress.
    /// </summary>
    public static AuthenticatedUserDto From(UserAccount account, LearnerProfile? profile) =>
        new(
            account.Id,
            account.Email,
            account.DisplayName,
            account.Username,
            account.PictureUrl,
            profile is not null,
            account.PreferredName,
            profile?.LearningGoal ?? LearningGoal.Unspecified,
            account.BirthDate,
            account.Gender,
            account.AcquisitionSource,
            account.AcquisitionSourceOther,
            account.HasCompletedDemographics);
}
