using Infrastructure.Speaking;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Ai;

/// <summary>
/// Announces cost-governance configuration that is legal but easy to be wrong about, so a
/// mis-set rate shows up in the startup log rather than in a surprising invoice. Hard errors are
/// raised at registration time by <c>ValidatePricing</c>/<c>Validate</c>; this only warns.
/// </summary>
public sealed class AiPricingStartupCheck : IHostedService
{
    private readonly AiAdmissionOptions _admission;
    private readonly AzureVoiceLiveOptions _voiceLive;
    private readonly ILogger<AiPricingStartupCheck> _logger;

    public AiPricingStartupCheck(
        AiAdmissionOptions admission,
        AzureVoiceLiveOptions voiceLive,
        ILogger<AiPricingStartupCheck> logger)
    {
        _admission = admission;
        _voiceLive = voiceLive;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_admission.HasZeroPricing)
        {
            _logger.LogWarning(
                "AI token pricing is declared configured but both rates are zero, so LLM usage adds "
                + "nothing to the ${Budget}/day budget and the free tier will never shed on LLM cost "
                + "alone. Expected while the LLM backend is free (self-hosted Hermes); set "
                + "AiAdmission:EstimatedInputCostPerMillionTokensUsd and "
                + "AiAdmission:EstimatedOutputCostPerMillionTokensUsd the moment a paid model is enabled.",
                _admission.DailyBudgetUsd);
        }

        if (!_voiceLive.PricingConfigured)
        {
            _logger.LogWarning(
                "Voice Live pricing is not configured; falling back to ${Rate}/audio-hour, which is "
                + "deliberately high rather than accurate. Set AzureVoiceLive:CostPerAudioHourUsd from "
                + "the Azure price sheet so the daily budget reflects real spend.",
                AzureVoiceLiveOptions.DefaultCostPerAudioHourUsd);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
