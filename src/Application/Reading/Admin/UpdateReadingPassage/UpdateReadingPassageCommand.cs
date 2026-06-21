using MediatR;

namespace Application.Reading.Admin.UpdateReadingPassage;

/// <summary>
/// Updates a curated reading passage's metadata (title, topic, CEFR level). <c>RequestingUserId</c>
/// is the acting admin (from the session); the handler authorizes it as an admin and rejects everyone
/// else with a 403. <c>Level</c> is the CEFR band name (e.g. "B1").
/// </summary>
public sealed record UpdateReadingPassageCommand(
    Guid RequestingUserId,
    Guid Id,
    string Title,
    string Topic,
    string Level)
    : IRequest<ReadingPassageAdminDto>;
