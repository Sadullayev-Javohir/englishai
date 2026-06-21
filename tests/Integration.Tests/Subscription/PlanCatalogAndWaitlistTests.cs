using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Subscription;
using FluentAssertions;
using Xunit;

namespace Integration.Tests.Subscription;

/// <summary>
/// The plan catalog is the only place the client is allowed to read prices from, so its agreement
/// with the domain constants is a contract, not an implementation detail. The waitlist is a public
/// unauthenticated write, so its idempotency and its silence about existing contacts are asserted too.
/// </summary>
public class PlanCatalogAndWaitlistTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public PlanCatalogAndWaitlistTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task The_catalog_reports_exactly_the_domain_prices()
    {
        var catalog = await GetCatalogAsync();
        var plans = catalog.GetProperty("plans").EnumerateArray().ToList();

        plans.Should().HaveCount(4);
        foreach (var plan in plans)
        {
            var parsed = Enum.Parse<SubscriptionPlan>(plan.GetProperty("plan").GetInt32().ToString());
            plan.GetProperty("priceUzs").GetInt32().Should().Be(SubscriptionPricing.PriceUzs(parsed));
            plan.GetProperty("durationDays").GetInt32().Should().Be(SubscriptionPricing.DurationDays(parsed));
            plan.GetProperty("pricePerMonthUzs").GetInt32()
                .Should().Be(SubscriptionPricing.PricePerMonthUzs(parsed));
        }
    }

    [Fact]
    public async Task The_catalog_is_public_and_reports_whether_checkout_is_open()
    {
        // The landing page is anonymous, and this flag is the single source for the "coming soon"
        // state - the UI must not decide it for itself.
        var catalog = await GetCatalogAsync();

        catalog.GetProperty("paymentsEnabled").ValueKind
            .Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
        catalog.GetProperty("currency").GetString().Should().Be("UZS");
    }

    [Fact]
    public async Task A_longer_commitment_always_costs_less_per_month()
    {
        var catalog = await GetCatalogAsync();
        var perMonth = catalog.GetProperty("plans").EnumerateArray()
            .Select(plan => (
                Days: plan.GetProperty("durationDays").GetInt32(),
                PerMonth: plan.GetProperty("pricePerMonthUzs").GetInt32()))
            .OrderBy(plan => plan.Days)
            .ToList();

        perMonth.Should().BeInDescendingOrder(plan => plan.PerMonth);
    }

    [Fact]
    public async Task Joining_the_waitlist_is_accepted_and_idempotent()
    {
        var client = _factory.CreateClient();
        var contact = $"waitlist-{Guid.NewGuid():N}@example.com";

        var first = await client.PostAsJsonAsync("/api/subscription/waitlist", new { contact });
        var second = await client.PostAsJsonAsync("/api/subscription/waitlist", new { contact });

        // Both accepted, and the second says nothing about the first: a public endpoint that reveals
        // "already registered" is an enumeration oracle.
        first.StatusCode.Should().Be(HttpStatusCode.Accepted);
        second.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Theory]
    [InlineData("901234567")]
    [InlineData("+998 90 123 45 67")]
    [InlineData("0901234567")]
    public async Task Uzbek_phone_numbers_are_accepted_however_they_are_written(string contact)
    {
        // Locally the same number is written half a dozen ways; rejecting the format the learner
        // happens to use would lose the signal for no reason.
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/subscription/waitlist", new { contact });

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-contact")]
    [InlineData("+1 555 0100")]
    public async Task A_contact_that_cannot_be_reached_is_rejected(string contact)
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/subscription/waitlist", new { contact });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    private async Task<JsonElement> GetCatalogAsync()
    {
        var response = await _factory.CreateClient().GetAsync("/api/subscription/plans");
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }
}
