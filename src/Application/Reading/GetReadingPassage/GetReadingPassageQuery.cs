using Application.Reading.Dtos;
using MediatR;

namespace Application.Reading.GetReadingPassage;

/// <summary>
/// Returns the reading lesson for a learning-spine topic: body, interactive glossary, and the
/// comprehension quiz (answers withheld for server-side grading). Generated and cached on first
/// open; while pending, the detail comes back with <c>IsReady=false</c>.
/// </summary>
public sealed record GetReadingPassageQuery(Guid TopicId) : IRequest<ReadingPassageDto>;
