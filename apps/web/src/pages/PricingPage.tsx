import { useEffect, useState } from "react";
import { api } from "@/api/client";
import type { PlanCatalogDto, SubscriptionPlanDto } from "@/api/types";
import { PremiumWaitlistForm } from "@/components/PremiumWaitlistForm";
import { formatUzs } from "@/content/pricing";
import { uz } from "@/content/uz";
import { PublicContentPage } from "./PublicContentPage";
import "./PricingPage.css";

const PLAN_LABELS: Record<string, string> = {
  monthly: "1 oy",
  quarterly: "3 oy",
  semiannual: "6 oy",
  yearly: "1 yil",
};

/**
 * The public pricing page.
 *
 * It used to be an editorial article that deliberately avoided naming a price, which meant a visitor
 * could not find out what the product costs anywhere on the site. Prices now come from
 * `GET /api/subscription/plans` - the same constants billing uses - and while `paymentsEnabled` is
 * false the call to action collects interest instead of opening a checkout that would only fail.
 * The original SEO article is kept below the grid so the page keeps its search value.
 */
export function PricingPage() {
  const [catalog, setCatalog] = useState<PlanCatalogDto | null>(null);

  useEffect(() => {
    let active = true;
    api.subscription.plans()
      .then((result) => {
        if (active) setCatalog(result);
      })
      .catch(() => undefined);

    return () => { active = false; };
  }, []);

  return (
    <div className="pricing-page">
      <section className="pricing-page__grid-wrap" aria-label="EnglishAI Premium rejalari">
        <div className="pricing-page__intro">
          <h1>{uz.marketing.premiumPlansTitle}</h1>
          <p>{uz.marketing.premiumPlansLead}</p>
        </div>

        <div className="pricing-page__grid">
          {(catalog?.plans ?? []).map((plan) => (
            <PlanCard key={plan.code} plan={plan} paymentsEnabled={catalog?.paymentsEnabled ?? false} />
          ))}
        </div>

        {catalog !== null && !catalog.paymentsEnabled && (
          <aside className="pricing-page__waitlist" aria-label="Premium haqida xabar olish">
            <h2>{uz.waitlist.title}</h2>
            <p>{uz.waitlist.lead}</p>
            <PremiumWaitlistForm source="pricing-page" />
          </aside>
        )}
      </section>

      {/* The editorial article keeps its own header, footer and SEO metadata. */}
      <PublicContentPage />
    </div>
  );
}

function PlanCard({ plan, paymentsEnabled }: { plan: SubscriptionPlanDto; paymentsEnabled: boolean }) {
  return (
    <article className={`pricing-card${plan.isBestValue ? " pricing-card--best" : ""}`}>
      {plan.isBestValue && <span className="pricing-card__badge">{uz.paywall.bestLabel}</span>}
      <h2 className="pricing-card__label">{PLAN_LABELS[plan.code] ?? plan.code}</h2>
      <p className="pricing-card__price">
        <strong>{formatUzs(plan.priceUzs)}</strong> <span>so‘m</span>
      </p>
      <p className="pricing-card__per-month">
        oyiga ≈ {formatUzs(plan.pricePerMonthUzs)} so‘m
      </p>
      {plan.savePercent > 0 && (
        <p className="pricing-card__save">{plan.savePercent}% tejaysiz</p>
      )}
      <p className="pricing-card__state">
        {paymentsEnabled ? uz.marketing.pricingAvailable : uz.marketing.pricingComingSoon}
      </p>
    </article>
  );
}
