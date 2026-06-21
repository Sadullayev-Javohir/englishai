using Domain.Learning;
using MediatR;

namespace Application.Analytics.RecordStudyTime;

/// <summary>
/// Records one heartbeat of study time the learner spent on a skill on their local calendar day
/// (PROJECT-SPEC Faza 2 analitika). Sent periodically by the active learning page; the day comes
/// from the client so week/month/year boundaries match the learner's own clock, not UTC.
/// </summary>
public sealed record RecordStudyTimeCommand(
    Guid LearnerId, SkillType Skill, int Seconds, DateOnly LocalDate) : IRequest;
