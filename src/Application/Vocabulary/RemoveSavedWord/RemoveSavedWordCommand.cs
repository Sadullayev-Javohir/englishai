using MediatR;

namespace Application.Vocabulary.RemoveSavedWord;

public sealed record RemoveSavedWordCommand(Guid LearnerId, Guid VocabularyItemId) : IRequest<Unit>;
