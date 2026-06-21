using Application.Common;
using Application.Identity.Dtos;
using MediatR;

namespace Application.Vocabulary.Admin.Images;

public sealed class ReplaceVocabularyImageAdminCommandHandler
    : IRequestHandler<ReplaceVocabularyImageAdminCommand, VocabularyImageAdminDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly IVocabularyImageReplacementService _replacement;

    public ReplaceVocabularyImageAdminCommandHandler(
        IAdminAuthorization admin,
        IVocabularyImageReplacementService replacement)
    {
        _admin = admin;
        _replacement = replacement;
    }

    public async Task<VocabularyImageAdminDto> Handle(
        ReplaceVocabularyImageAdminCommand request,
        CancellationToken cancellationToken)
    {
        if (await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken) is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        return await _replacement.ReplaceAsync(request.TopicId, request.ImageId, cancellationToken);
    }
}
