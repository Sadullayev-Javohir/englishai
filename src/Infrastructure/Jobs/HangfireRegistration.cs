using Hangfire;
using Hangfire.InMemory;
using Hangfire.PostgreSql;
using Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Jobs;

public static class HangfireRegistration
{
    public static IServiceCollection AddEnglishAiHangfireStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        bool requireStorage)
    {
        var options = configuration.GetSection(HangfireOptions.SectionName).Get<HangfireOptions>() ?? new HangfireOptions();
        services.AddSingleton(options);

        if (!options.Enabled)
        {
            if (requireStorage)
                throw new InvalidOperationException("Hangfire must be enabled for the worker host.");
            return services;
        }

        var postgres = configuration.GetConnectionString("HangfirePostgres")
                       ?? configuration.GetConnectionString("Postgres");
        if (!options.UseInMemoryStorage && string.IsNullOrWhiteSpace(postgres))
            throw new InvalidOperationException("Hangfire PostgreSQL storage requires ConnectionStrings:Postgres.");
        if (!options.UseInMemoryStorage && configuration.GetValue<bool>("Postgres:RequireTransactionPooling"))
            PostgresConnectionPolicy.ValidateSession(postgres!, "Hangfire PostgreSQL storage");
        var boundedPostgres = options.UseInMemoryStorage
            ? postgres
            : PostgresConnectionPolicy.ApplyPoolBudget(postgres!, options.DatabaseConnectionBudget);

        services.AddHangfire(global =>
        {
            global.UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings();

            if (options.UseInMemoryStorage)
                global.UseInMemoryStorage();
            else
                global.UsePostgreSqlStorage(storage => storage.UseNpgsqlConnection(boundedPostgres!));
        });

        return services;
    }

    public static IServiceCollection AddEnglishAiHangfireWorker(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEnglishAiHangfireStorage(configuration, requireStorage: true);
        var options = configuration.GetSection(HangfireOptions.SectionName).Get<HangfireOptions>() ?? new HangfireOptions();
        var queues = options.ParseQueues();
        var workerCount = Math.Clamp(options.WorkerCount, 1, 64);
        options.ValidateWorkerBudget(queues);

        var retry = new AutomaticRetryAttribute
        {
            Attempts = Math.Clamp(options.RetryAttempts, 1, 10),
            DelaysInSeconds = options.ParseRetryDelays(),
            OnAttemptsExceeded = AttemptsExceededAction.Fail,
            LogEvents = true,
        };
        GlobalJobFilters.Filters.Add(retry);

        services.AddHangfireServer(server =>
        {
            server.WorkerCount = workerCount;
            server.Queues = queues;
            server.ServerName = $"{Environment.MachineName}:{string.Join('+', queues)}";
        });
        services.AddHostedService<RecurringJobRegistrationService>();
        return services;
    }
}
