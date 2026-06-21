using System.Security.Cryptography;
using Application.Common;
using Application.Identity.Ports;
using Application.Identity.Avatar;
using Infrastructure.Identity.Avatar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Identity;

/// <summary>
/// Registers the Google-sign-in adapters and binds their options. Kept separate from the
/// main <see cref="DependencyInjection"/> so the composition root (Web) can also read the
/// resolved <see cref="JwtOptions"/> back to configure JWT bearer validation - both the
/// issuer and the validator must share the exact same signing key.
/// </summary>
public static class AuthDependencyInjection
{
    /// <summary>
    /// Registers the auth adapters/options and returns the resolved <see cref="JwtOptions"/>
    /// (with a generated dev signing key when none is configured) for the Web layer to wire
    /// JWT bearer validation against.
    /// </summary>
    public static JwtOptions AddAuthInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var google = configuration.GetSection(GoogleAuthOptions.SectionName).Get<GoogleAuthOptions>()
                     ?? new GoogleAuthOptions();

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        // Rule 13: never hardcode a signing key. In dev (no secret set) generate a random
        // one so the app still runs - sessions simply do not survive a restart.
        if (string.IsNullOrWhiteSpace(jwt.SigningKey))
            jwt.SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

        services.AddSingleton(google);
        services.AddSingleton(jwt);

        services.AddSingleton<IGoogleTokenValidator, GoogleIdTokenValidator>();
        services.AddSingleton<IAuthTokenIssuer, JwtAuthTokenIssuer>();

        // Complimentary full access for an allowlist of emails (built-in + optional config).
        // Scoped because it resolves the learner's account through the (scoped) account store.
        var complimentary = configuration.GetSection(ComplimentaryAccessOptions.SectionName)
                                .Get<ComplimentaryAccessOptions>()
                            ?? new ComplimentaryAccessOptions();
        services.AddSingleton(complimentary);
        services.AddScoped<IComplimentaryAccess, ComplimentaryAccess>();

        // Super-admin allowlist (built-in + optional config) backing the operator admin panel. Scoped
        // like complimentary access because it resolves the account through the (scoped) account store.
        var superAdmin = configuration.GetSection(SuperAdminOptions.SectionName).Get<SuperAdminOptions>()
                         ?? new SuperAdminOptions();
        services.AddSingleton(superAdmin);
        services.AddScoped<IAdminAuthorization, AdminAuthorization>();

        // Durable EF persistence when a database is configured; in-memory otherwise so the
        // app stays runnable/testable without a database (mirrors the other stores).
        var hasDatabase = !string.IsNullOrWhiteSpace(configuration.GetConnectionString("Postgres"));
        if (hasDatabase)
        {
            services.AddScoped<IUserAccountStore, EfUserAccountStore>();
            services.AddScoped<IUserAvatarStore, EfUserAvatarStore>();
            services.AddScoped<IUserPreferencesStore, EfUserPreferencesStore>();
            // Full durable wipe of all learner-scoped tables on account deletion.
            services.AddScoped<IAccountEraser, EfAccountEraser>();
        }
        else
        {
            services.AddSingleton<IUserAccountStore, InMemoryUserAccountStore>();
            services.AddSingleton<IUserAvatarStore, InMemoryUserAvatarStore>();
            services.AddSingleton<IUserPreferencesStore, InMemoryUserPreferencesStore>();
            services.AddSingleton<IAccountEraser, InMemoryAccountEraser>();
        }

        return jwt;
    }
}
