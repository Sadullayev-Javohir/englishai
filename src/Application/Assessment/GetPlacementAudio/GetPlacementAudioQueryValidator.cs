using FluentValidation;

namespace Application.Assessment.GetPlacementAudio;

public sealed class GetPlacementAudioQueryValidator : AbstractValidator<GetPlacementAudioQuery>
{
    public GetPlacementAudioQueryValidator()
    {
        RuleFor(x => x.QuestionId).NotEmpty();
    }
}
