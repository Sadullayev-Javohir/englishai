using Application.Common;
using Application.Identity.Dtos;
using Application.Vocabulary.Ports;
using MediatR;

namespace Application.Vocabulary.Admin.Images;

public sealed class GetVocabularyImagesAdminQueryHandler
    : IRequestHandler<GetVocabularyImagesAdminQuery, IReadOnlyList<VocabularyImageAdminDto>>
{
    private readonly IAdminAuthorization _admin;
    private readonly IVocabularyTopicRepository _topics;

    public GetVocabularyImagesAdminQueryHandler(
        IAdminAuthorization admin,
        IVocabularyTopicRepository topics)
    {
        _admin = admin;
        _topics = topics;
    }

    public async Task<IReadOnlyList<VocabularyImageAdminDto>> Handle(
        GetVocabularyImagesAdminQuery request,
        CancellationToken cancellationToken)
    {
        if (await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken) is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var topics = await _topics.GetAllAsync(cancellationToken);
        return topics.SelectMany(topic => topic.Words.Select(word =>
            {
                var imageId = WordImageQuery.ImageId(topic.Id, word.Word);
                return new VocabularyImageAdminDto(
                    topic.Id,
                    imageId,
                    topic.Title,
                    topic.Level.ToString(),
                    word.Word,
                    word.Translation,
                    $"/api/images/vocabulary-topics/{topic.Id}/words/{imageId}",
                    word.ImageSource,
                    word.ImageAttribution,
                    0);
            }))
            .ToArray();
    }
}
