import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import {
  MONTHLY_DURATION_DAYS,
  MONTHLY_PRICE_UZS,
  QUARTERLY_DURATION_DAYS,
  QUARTERLY_PRICE_UZS,
  SEMI_ANNUAL_DURATION_DAYS,
  SEMI_ANNUAL_PRICE_UZS,
  YEARLY_DURATION_DAYS,
  YEARLY_PRICE_UZS,
  formatUzs,
  pricePerMonthUzs,
  savePercent,
} from "./pricing";

/**
 * Pins the frontend prices to the backend constants. Without this the two drift, and the first
 * anyone notices is a learner seeing one price on the pricing page and another at checkout.
 */
// Vitest runs with apps/web as the working directory.
const DOMAIN_PRICING = readFileSync(
  resolve(process.cwd(), "../../src/Domain/Subscription/SubscriptionPricing.cs"),
  "utf8",
);

function domainConstant(name: string): number {
  const match = DOMAIN_PRICING.match(new RegExp(`${name}\\s*=\\s*([0-9_]+)`));
  if (!match) throw new Error(`SubscriptionPricing.cs no longer declares ${name}`);
  return Number(match[1].replace(/_/g, ""));
}

describe("pricing", () => {
  it.each([
    ["MonthlyPriceUzs", MONTHLY_PRICE_UZS],
    ["QuarterlyPriceUzs", QUARTERLY_PRICE_UZS],
    ["SemiAnnualPriceUzs", SEMI_ANNUAL_PRICE_UZS],
    ["YearlyPriceUzs", YEARLY_PRICE_UZS],
    ["MonthlyDurationDays", MONTHLY_DURATION_DAYS],
    ["QuarterlyDurationDays", QUARTERLY_DURATION_DAYS],
    ["SemiAnnualDurationDays", SEMI_ANNUAL_DURATION_DAYS],
    ["YearlyDurationDays", YEARLY_DURATION_DAYS],
  ])("matches the domain constant %s", (name, expected) => {
    expect(domainConstant(name)).toBe(expected);
  });

  it("formats amounts with the Uzbek thousands separator", () => {
    expect(formatUzs(99_000)).toBe("99 000");
    expect(formatUzs(799_000)).toBe("799 000");
  });

  it("derives the effective monthly rate rather than hard-coding it", () => {
    expect(pricePerMonthUzs(MONTHLY_PRICE_UZS, MONTHLY_DURATION_DAYS)).toBe(MONTHLY_PRICE_UZS);
    expect(pricePerMonthUzs(YEARLY_PRICE_UZS, YEARLY_DURATION_DAYS)).toBe(65_671);
  });

  it("reports a bigger saving for a longer commitment", () => {
    const quarterly = savePercent(QUARTERLY_PRICE_UZS, QUARTERLY_DURATION_DAYS);
    const semiAnnual = savePercent(SEMI_ANNUAL_PRICE_UZS, SEMI_ANNUAL_DURATION_DAYS);
    const yearly = savePercent(YEARLY_PRICE_UZS, YEARLY_DURATION_DAYS);

    expect(quarterly).toBeGreaterThan(0);
    expect(semiAnnual).toBeGreaterThan(quarterly);
    expect(yearly).toBeGreaterThan(semiAnnual);
  });
});
