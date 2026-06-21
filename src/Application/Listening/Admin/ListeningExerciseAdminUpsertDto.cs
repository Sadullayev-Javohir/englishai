using Domain.Listening;

namespace Application.Listening.Admin;

/// <summary>Create/update payload for a listening exercise (metadata only).</summary>
public sealed record ListeningExerciseAdminUpsertDto(
    string Title,
    string Topic,
    string Level);
