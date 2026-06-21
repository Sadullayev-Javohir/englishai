using Application.Competition.Dtos;

namespace Application.Competition;

/// <summary>
/// Static mapping helpers between the <see cref="Domain.Competition.Competition"/> domain aggregate and the
/// transport DTOs. The raw access code is NEVER persisted (stored only as a salted hash,
/// docs/development-guide.md rule 13) so it is surfaced only transiently, via the <paramref name="accessCode"/>
/// parameter, at creation / host-only reads.
/// </summary>
public static class CompetitionMapper
{
    /// <summary>Maps the aggregate to a <see cref="CompetitionDto"/>.</summary>
    public static CompetitionDto ToDto(Domain.Competition.Competition competition, string? accessCode = null)
    {
        var participants = competition.Participants
            .Select(p => new ParticipantDto(
                p.Id, p.LearnerId, p.DisplayName, p.IsHost, p.Status, p.Score))
            .ToList();

        return new CompetitionDto(
            competition.Id,
            competition.Title,
            competition.Status,
            CompetitionSettingsDto.FromDomain(competition.Settings),
            competition.CurrentSlideIndex,
            competition.Slides.Count,
            participants,
            accessCode);
    }

    /// <summary>Maps a single participant to a <see cref="RankingRowDto"/> at the given rank.</summary>
    public static RankingRowDto ToRankingRow(Domain.Competition.Participant participant, int rank) => new(
        participant.Id, participant.DisplayName, participant.IsHost,
        participant.Score, participant.Answers.Count, rank);
}
