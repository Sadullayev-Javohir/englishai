using Application.Writing.Dtos;
using MediatR;

namespace Application.Writing.GetWritingTask;

/// <summary>
/// Returns the writing task for a learning-spine topic: the prompt, English guidance hints and the
/// expected word range. Generated and cached on first open; while pending, the detail comes back
/// with <c>IsReady=false</c>.
/// </summary>
public sealed record GetWritingTaskQuery(Guid TopicId) : IRequest<WritingTaskDto>;
