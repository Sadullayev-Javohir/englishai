using Application.Common;
using Application.Identity.Dtos;
using Application.Vocabulary.Admin;
using Application.Vocabulary.Ports;
using Domain.Vocabulary;
using MediatR;

namespace Application.Vocabulary.Admin.GetAllVocabularyTopics;

public sealed class GetAllVocabularyTopicsQueryHandler
    : IRequestHandler<GetAllVocabularyTopicsQuery, IReadOnlyList<VocabularyTopicAdminDto>>
{
    private readonly IAdminAuthorization _admin;
    private readonly IVocabularyTopicRepository _topics;

    public GetAllVocabularyTopicsQueryHandler(IAdminAuthorization admin, IVocabularyTopicRepository topics)
    {
        _admin = admin;
        _topics = topics;
    }

    public async Task<IReadOnlyList<VocabularyTopicAdminDto>> Handle(
        GetAllVocabularyTopicsQuery request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        // GetAllAsync is provided by the infrastructure adapter (seeded catalog + saved topics).
        var all = await _topics.GetAllAsync(cancellationToken);

        return all
            .OrderBy(t => t.Level)
            .ThenBy(t => t.Sequence)
            .Select(VocabularyTopicAdminDto.FromDomain)
            .ToList();
    }
}
