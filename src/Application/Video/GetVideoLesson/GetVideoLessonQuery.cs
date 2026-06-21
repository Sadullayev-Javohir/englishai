using Application.Video.Dtos;
using MediatR;

namespace Application.Video.GetVideoLesson;

/// <summary>
/// Returns full lesson detail for the Video Player screen: embed id, interactive
/// transcript, and the comprehension quiz (answers withheld).
/// </summary>
public sealed record GetVideoLessonQuery(Guid VideoLessonId) : IRequest<VideoLessonDto>;
