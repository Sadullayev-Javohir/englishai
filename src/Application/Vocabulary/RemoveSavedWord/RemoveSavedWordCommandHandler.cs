using Application.Common;
using Application.Vocabulary.Ports;
using Domain.Vocabulary;
using MediatR;

namespace Application.Vocabulary.RemoveSavedWord;

public sealed class RemoveSavedWordCommandHandler(IVocabularyRepository vocabulary)
    : IRequestHandler<RemoveSavedWordCommand, Unit>
{
    public async Task<Unit> Handle(RemoveSavedWordCommand request, CancellationToken cancellationToken)
    {
        var item = await vocabulary.GetByIdAsync(request.VocabularyItemId, cancellationToken)
                   ?? throw new NotFoundException(nameof(VocabularyItem), request.VocabularyItemId);

        if (item.LearnerId != request.LearnerId)
            throw new ForbiddenException("You can only remove words from your own vocabulary.");

        await vocabulary.DeleteAsync(item.Id, cancellationToken);
        return Unit.Value;
    }
}
