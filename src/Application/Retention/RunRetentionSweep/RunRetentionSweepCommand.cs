using Application.Retention.Dtos;
using MediatR;

namespace Application.Retention.RunRetentionSweep;

/// <summary>
/// The daily win-back sweep (PROJECT-SPEC I.2), run by Hangfire: scans every learner,
/// works out which inactivity stage (3/7/14/30/60+ days) they have reached, and sends the
/// matching templated win-back message - but only when the learner has just crossed into a
/// new stage, so a long-dormant learner is never messaged daily. Uzbek text comes only
/// from templates (docs/development-guide.md rule 11).
/// </summary>
public sealed record RunRetentionSweepCommand : IRequest<RetentionSweepResultDto>;
