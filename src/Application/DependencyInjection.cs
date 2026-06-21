using System.Reflection;
using Application.Common;
using Application.Gamification;
using Application.Referral;
using Application.Subscription.Access;
using Application.Subscription.Entitlements;
using Application.Vocabulary;
using Application.Vocabulary.Ports;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Application;

/// <summary>
/// Registers the Application layer: MediatR handlers, FluentValidation validators,
/// and the validation pipeline behavior.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Authorization: a learner may only act on their own data. Enforced centrally by
        // comparing each request's LearnerId to the authenticated user (the JWT sub).
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LearnerOwnershipBehavior<,>));

        // Testable time source for decay/window logic (PROJECT-SPEC G.4); never
        // DateTime.Now directly (docs/development-guide.md 17.2 principle).
        services.TryAddSingleton(TimeProvider.System);

        // Freemium gating (PROJECT-SPEC H.1) - depends on the subscription repository and
        // usage counter registered in Infrastructure.
        services.AddScoped<IProAccessService, ProAccessService>();
        services.AddScoped<IEntitlementService, EntitlementService>();
        services.AddScoped<ISpeakingMinuteAllowanceService, SpeakingMinuteAllowanceService>();

        // Topic trial paywall (PROJECT-SPEC H.1): the first N topics are free, then Premium is
        // required. Depends on the subscription repository, complimentary allowlist and topic
        // completion store registered in Infrastructure.
        services.AddScoped<ITopicAccessPolicy, TopicAccessPolicy>();
        services.AddScoped<IMandatoryReviewPolicy, MandatoryReviewPolicy>();
        services.AddScoped<ITopicVocabularyEnrollmentService, TopicVocabularyEnrollmentService>();

        // Leaderboard/points feature: awards points for module completion/daily activity/streak
        // milestones and keeps the learner's per-CEFR-level leaderboard entry in sync. Depends on
        // the points ledger and leaderboard store registered in Infrastructure.
        services.AddScoped<IPointsService, PointsService>();

        // Daily-goal/streak progress (PROJECT-SPEC Faza 5): the single path every skill
        // completion funnels through to feed the Home 6-skill plan and the streak.
        services.AddScoped<IDailyProgressRecorder, DailyProgressRecorder>();

        // Referral programme: mints each learner's code, records who referred whom, and pays the
        // capped reward bundle (+2 topics, bonus Speaking/Writing credits) when a referred learner
        // first engages. Depends on the referral store registered in Infrastructure.
        services.AddScoped<IReferralService, ReferralService>();

        return services;
    }
}
