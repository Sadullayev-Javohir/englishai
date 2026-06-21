using Application.Common;
using Application.Identity.Dtos;
using Application.Vocabulary.Ports;
using Domain.Vocabulary;
using MediatR;

namespace Application.Vocabulary.Admin.DeleteVocabularyTopic;

public sealed class DeleteVocabularyTopicCommandHandler
    : IRequestHandler<DeleteVocabularyTopicCommand, Unit>
{
    private readonly IAdminAuthorization _admin;
    private readonly IVocabularyTopicRepository _topics;

    public DeleteVocabularyTopicCommandHandler(IAdminAuthorization admin, IVocabularyTopicRepository topics)
    {
        _admin = admin;
        _topics = topics;
    }

    public async Task<Unit> Handle(
        DeleteVocabularyTopicCommand request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var topic = await _topics.GetByIdAsync(request.Id, cancellationToken)
                    ?? throw new NotFoundException(nameof(VocabularyTopic), request.Id);

        await _topics.DeleteAsync(request.Id, cancellationToken);

        return Unit.Value;
    }
}
