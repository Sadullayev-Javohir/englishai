using Application.Common;
using Domain.Subscription;
using MediatR;

namespace Application.Subscription.GetPlanCatalog;

public sealed class GetPlanCatalogQueryHandler : IRequestHandler<GetPlanCatalogQuery, PlanCatalogDto>
{
    private readonly IPaymentAvailability _availability;

    public GetPlanCatalogQueryHandler(IPaymentAvailability availability)
    {
        _availability = availability;
    }

    public Task<PlanCatalogDto> Handle(GetPlanCatalogQuery request, CancellationToken cancellationToken)
    {
        var plans = new[]
        {
            SubscriptionPlan.Monthly,
            SubscriptionPlan.Quarterly,
            SubscriptionPlan.SemiAnnual,
            SubscriptionPlan.Yearly,
        };

        var catalog = plans
            .Select(plan => new SubscriptionPlanDto(
                plan,
                plan.ToString().ToLowerInvariant(),
                SubscriptionPricing.PriceUzs(plan),
                SubscriptionPricing.DurationDays(plan),
                SubscriptionPricing.PricePerMonthUzs(plan),
                SubscriptionPricing.SavePercent(plan),
                // The yearly plan is the one to push: it takes churn out of the equation for a year
                // and puts the cash in up front, which matters far more than the discount costs.
                IsBestValue: plan == SubscriptionPlan.Yearly))
            .ToArray();

        return Task.FromResult(new PlanCatalogDto(catalog, _availability.PaymentsEnabled));
    }
}
