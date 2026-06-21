/**
 * The single frontend source for subscription prices.
 *
 * These numbers previously lived in four hand-maintained copies across the UI, with nothing keeping
 * them in step with the backend - which is how a pricing page starts quietly disagreeing with
 * checkout. `pricing.test.ts` pins every value here to the C# constants in
 * `src/Domain/Subscription/SubscriptionPricing.cs`, so a price change that is not made in both
 * places fails the build.
 *
 * A build-time copy is kept (rather than only fetching `GET /api/subscription/plans`) because the
 * landing page is prerendered: an empty price while a request is in flight would be what search
 * engines and first-paint visitors see.
 */

export const MONTHLY_PRICE_UZS = 99_000;
export const QUARTERLY_PRICE_UZS = 249_000;
export const SEMI_ANNUAL_PRICE_UZS = 449_000;
export const YEARLY_PRICE_UZS = 799_000;

export const MONTHLY_DURATION_DAYS = 30;
export const QUARTERLY_DURATION_DAYS = 90;
export const SEMI_ANNUAL_DURATION_DAYS = 180;
export const YEARLY_DURATION_DAYS = 365;

/**
 * `99000` -> `"99 000"`. Uzbek uses a space as the thousands separator.
 *
 * Grouped by hand rather than through `toLocaleString`, whose separator for these locales is a
 * non-breaking or narrow no-break space depending on the runtime - invisible in review and a
 * different string in every snapshot test.
 */
export function formatUzs(amount: number): string {
  return Math.round(amount)
    .toString()
    .replace(/\B(?=(\d{3})+(?!\d))/g, " ");
}

/** Effective monthly rate — derived, so it can never drift from the headline price. */
export function pricePerMonthUzs(priceUzs: number, durationDays: number): number {
  return Math.round((priceUzs * MONTHLY_DURATION_DAYS) / durationDays);
}

/** How much cheaper per month than paying monthly, as a whole percent. */
export function savePercent(priceUzs: number, durationDays: number): number {
  return Math.round((1 - pricePerMonthUzs(priceUzs, durationDays) / MONTHLY_PRICE_UZS) * 100);
}
