using Application.Video.Dtos;
using MediatR;

namespace Application.Video.OpenVideoByUrl;

/// <summary>
/// Opens an arbitrary YouTube video pasted by the learner (its 11-char video id, already
/// extracted from the URL client-side). If a lesson for that id already exists it is returned
/// as-is; otherwise a playable lesson is created from the video's real metadata with an empty
/// transcript/quiz - the interactive transcript is filled lazily from the video's real captions
/// when it is opened (PROJECT-SPEC B.3, Bosqich 3), never fabricated (docs/development-guide.md rules 8, 11).
/// </summary>
public sealed record OpenVideoByUrlCommand(string YouTubeVideoId) : IRequest<VideoLessonDto>;
