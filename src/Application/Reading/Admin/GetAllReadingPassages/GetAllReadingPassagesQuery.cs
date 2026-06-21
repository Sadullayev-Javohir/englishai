using MediatR;

namespace Application.Reading.Admin.GetAllReadingPassages;

/// <summary>
/// Returns every curated reading passage for the admin catalog, ordered by CEFR level
/// (A1→C2) then most-recently created. <c>RequestingUserId</c> is the viewer (from the session);
/// the handler authorizes it as an admin and rejects everyone else with a 403.
/// </summary>
public sealed record GetAllReadingPassagesQuery(Guid RequestingUserId)
    : IRequest<IReadOnlyList<ReadingPassageAdminDto>>;
