using Application.Common;
using Application.Identity.Dtos;
using Application.Vocabulary.Admin;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Vocabulary;
using MediatR;

namespace Application.Vocabulary.Admin.CreateVocabularyTopic;

public sealed class CreateVocabularyTopicCommandHandler
    : IRequestHandler<CreateVocabularyTopicCommand, VocabularyTopicAdminDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly IVocabularyTopicRepository _topics;
    private readonly TimeProvider _clock;

    public CreateVocabularyTopicCommandHandler(
        IAdminAuthorization admin, IVocabularyTopicRepository topics, TimeProvider clock)
    {
        _admin = admin;
        _topics = topics;
        _clock = clock;
    }

    public async Task<VocabularyTopicAdminDto> Handle(
        CreateVocabularyTopicCommand request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var level = Enum.Parse<CefrLevel>(request.Level, ignoreCase: true);
        var now = _clock.GetUtcNow();

        var topic = VocabularyTopic.Curate(
            request.Slug, request.Title, request.TitleUz, request.Category, request.GrammarFocusCode,
            level, now, request.Sequence);

        await _topics.SaveAsync(topic, cancellationToken);

        return VocabularyTopicAdminDto.FromDomain(topic);
    }
}
