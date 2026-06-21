using FluentValidation;

namespace Application.Video.FindFullVideoPlaylist;

public sealed class FindFullVideoPlaylistQueryValidator : AbstractValidator<FindFullVideoPlaylistQuery>
{
    public FindFullVideoPlaylistQueryValidator()
    {
        RuleFor(query => query.Query).NotEmpty().MaximumLength(160);
    }
}
