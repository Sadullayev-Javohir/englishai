using FluentAssertions;
using Infrastructure.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests;

public sealed class HangfireGovernanceTests
{
    [Fact]
    public void Queue_catalog_is_fixed_and_complete()
    {
        HangfireQueues.All.Should().BeEquivalentTo(
            HangfireQueues.Critical,
            HangfireQueues.Notifications,
            HangfireQueues.Analytics,
            HangfireQueues.Content,
            HangfireQueues.Maintenance);
    }

    [Fact]
    public void Critical_queue_requires_a_dedicated_worker()
    {
        var options = new HangfireOptions { Queues = "critical,notifications", WorkerCount = 2, DatabaseConnectionBudget = 4 };
        var queues = options.ParseQueues();

        var action = () => options.ValidateWorkerBudget(queues);

        action.Should().Throw<InvalidOperationException>().WithMessage("*dedicated worker host*");
    }

    [Fact]
    public void Worker_count_cannot_exceed_database_budget()
    {
        var options = new HangfireOptions { Queues = "content", WorkerCount = 9, DatabaseConnectionBudget = 8 };

        var action = () => options.ValidateWorkerBudget(options.ParseQueues());

        action.Should().Throw<InvalidOperationException>().WithMessage("*database connection budget*");
    }

    [Fact]
    public void Retry_policy_requires_one_positive_delay_per_attempt()
    {
        var options = new HangfireOptions { RetryAttempts = 3, RetryDelaysSeconds = "15,60" };

        var action = () => options.ParseRetryDelays();

        action.Should().Throw<InvalidOperationException>().WithMessage("*delay count*");
    }

    [Fact]
    public void Web_storage_registration_does_not_register_a_hangfire_server()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Hangfire:Enabled"] = "true",
            ["Hangfire:UseInMemoryStorage"] = "true",
        }).Build();

        services.AddEnglishAiHangfireStorage(configuration, requireStorage: false);

        services.Should().NotContain(descriptor =>
            descriptor.ServiceType.FullName != null &&
            descriptor.ServiceType.FullName.Contains("BackgroundJobServerHostedService"));
    }
}
