using Application.Listening.Dtos;
using MediatR;

namespace Application.Listening.GetListeningExercise;

/// <summary>
/// Returns full exercise detail for the player, keyed by the learning-spine topic id: the
/// comprehension quiz (answers withheld for server-side grading) and the transcript (revealed by the
/// UI after answering). The exercise is generated and cached on first open.
/// </summary>
public sealed record GetListeningExerciseQuery(Guid TopicId) : IRequest<ListeningExerciseDto>;
