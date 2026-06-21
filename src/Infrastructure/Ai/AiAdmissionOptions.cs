namespace Infrastructure.Ai;

public sealed class AiAdmissionOptions
{
    public const string SectionName = "AiAdmission";

    public int GlobalConcurrency { get; set; } = 32;
    public int ProReservedConcurrency { get; set; } = 8;
    public int MaxConcurrentPerCaller { get; set; } = 2;
    public int MaxQueueLength { get; set; } = 128;
    public int QueueTimeoutSeconds { get; set; } = 15;
    public int FreeRequestsPerMinute { get; set; } = 15;
    public int ProRequestsPerMinute { get; set; } = 30;
    public int AssistantFreeRequestsPerMinute { get; set; } = 20;
    public int AssistantProRequestsPerMinute { get; set; } = 40;
    public double DailyBudgetUsd { get; set; } = 25;
    public double BudgetWarningRatio { get; set; } = 0.8;

    /// <summary>
    /// Per-learner daily spend ceiling for free accounts. The global
    /// <see cref="DailyBudgetUsd"/> alone is not a safety net: it is all-or-nothing, so one heavy
    /// account can drain it and every other free learner is shed for the rest of the day. This
    /// bounds the individual before the group is affected. Zero disables the per-learner check.
    /// </summary>
    public double FreeDailyBudgetPerLearnerUsd { get; set; } = 0.60;

    /// <summary>
    /// The same ceiling for paying accounts, set well above what a subscription's worth of usage
    /// costs so it only ever catches abuse or a runaway client - not a heavy learner.
    /// </summary>
    public double ProDailyBudgetPerLearnerUsd { get; set; } = 3.00;

    public double DailyBudgetPerLearnerUsd(Application.Ai.AiSubscriptionTier tier) =>
        tier == Application.Ai.AiSubscriptionTier.Pro
            ? ProDailyBudgetPerLearnerUsd
            : FreeDailyBudgetPerLearnerUsd;
    public double EstimatedInputCostPerMillionTokensUsd { get; set; }
    public double EstimatedOutputCostPerMillionTokensUsd { get; set; }
    public bool PricingConfigured { get; set; }

    /// <summary>
    /// Pricing is declared configured but every token rate is zero, so LLM usage contributes
    /// nothing to the daily budget. This is a legitimate production state while the LLM backend is
    /// free (self-hosted Hermes plus a free proxy) — which is exactly why it must be announced
    /// loudly at startup rather than left to be discovered from a surprising invoice.
    /// </summary>
    public bool HasZeroPricing =>
        PricingConfigured
        && EstimatedInputCostPerMillionTokensUsd <= 0
        && EstimatedOutputCostPerMillionTokensUsd <= 0;

    /// <summary>
    /// Rejects configurations where the budget would silently ignore real money. Note the
    /// asymmetry with <see cref="HasZeroPricing"/>: zero rates are survivable (nothing is being
    /// spent), whereas real rates with pricing switched off means spend is happening and the
    /// budget cannot see it.
    /// </summary>
    public void ValidatePricing()
    {
        if (!PricingConfigured
            && (EstimatedInputCostPerMillionTokensUsd > 0 || EstimatedOutputCostPerMillionTokensUsd > 0))
        {
            throw new InvalidOperationException(
                $"{SectionName} token rates are set but {SectionName}:PricingConfigured is false, "
                + "so the daily budget would ignore real LLM spend.");
        }

        if (DailyBudgetUsd <= 0)
            throw new InvalidOperationException($"{SectionName}:DailyBudgetUsd must be greater than zero.");
    }
}
