using Application.Speaking.Dtos;
using MediatR;

namespace Application.Speaking.GetIdeaCards;

/// <summary>
/// Fetches idea cards for an in-progress conversation so a learner who has run out of things to say
/// can pull up concrete, answerable talking points (the cure for the blank-page freeze). Keyed by the
/// live session so the cards match the exact topic, level and recent turns of THIS conversation.
/// </summary>
public sealed record GetIdeaCardsQuery(Guid SessionId) : IRequest<IdeaCardsResult>;
