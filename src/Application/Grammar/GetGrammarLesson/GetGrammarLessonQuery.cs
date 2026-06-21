using Application.Grammar.Dtos;
using MediatR;

namespace Application.Grammar.GetGrammarLesson;

/// <summary>
/// Returns the grammar lesson for a learning-spine topic (all five G.2 steps): the context intro,
/// the generated English rule explanation, the (answerless) exercises and the application tasks.
/// Generated and cached on first open; while pending, the detail comes back with <c>IsReady=false</c>.
/// </summary>
public sealed record GetGrammarLessonQuery(Guid TopicId) : IRequest<GrammarLessonDto>;
