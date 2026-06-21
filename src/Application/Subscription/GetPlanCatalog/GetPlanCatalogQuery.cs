using Domain.Subscription;
using MediatR;

namespace Application.Subscription.GetPlanCatalog;

/// <summary>
/// The plans and prices the client shows. Anonymous: the landing page and the pricing page are both
/// public.
///
/// This endpoint exists because the prices used to live in four hand-maintained copies across the
/// frontend, with nothing keeping them in step with the domain - a pricing page that quietly
/// disagrees with checkout is worse than no pricing page.
/// </summary>
public sealed record GetPlanCatalogQuery : IRequest<PlanCatalogDto>;

/// <param name="PaymentsEnabled">
/// Whether checkout can actually be completed. False while no payment provider is connected: the
/// client then shows the real prices with a "coming soon" state and collects interest instead of
/// opening a checkout that would only fail.
/// </param>
public sealed record PlanCatalogDto(
    IReadOnlyList<SubscriptionPlanDto> Plans,
    bool PaymentsEnabled,
    string Currency = "UZS");

/// <param name="PricePerMonthUzs">Effective monthly rate, derived - never hand-written.</param>
/// <param name="SavePercent">How much cheaper per month than paying monthly.</param>
public sealed record SubscriptionPlanDto(
    SubscriptionPlan Plan,
    string Code,
    int PriceUzs,
    int DurationDays,
    int PricePerMonthUzs,
    int SavePercent,
    bool IsBestValue);
