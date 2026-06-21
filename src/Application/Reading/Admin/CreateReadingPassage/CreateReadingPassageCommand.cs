using MediatR;

namespace Application.Reading.Admin.CreateReadingPassage;

/// <summary>
/// Curates a new reading passage (metadata only; the body, glossary and questions are lazily
/// filled later). <c>RequestingUserId</c> is the acting admin (from the session); the handler
/// authorizes it as an admin and rejects everyone else with a 403. <c>Level</c> is the CEFR band
/// name (e.g. "B1").
/// </summary>
public sealed record CreateReadingPassageCommand(
    Guid RequestingUserId,
    string Title,
    string Topic,
    string Level)
    : IRequest<ReadingPassageAdminDto>;
