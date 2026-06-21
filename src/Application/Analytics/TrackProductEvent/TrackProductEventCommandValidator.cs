using Domain.Analytics;
using FluentValidation;

namespace Application.Analytics.TrackProductEvent;

public sealed class TrackProductEventCommandValidator : AbstractValidator<TrackProductEventCommand>
{
    public TrackProductEventCommandValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
        RuleFor(x => x.EventType).IsInEnum();
        RuleFor(x => x.Source).MaximumLength(ProductEvent.MaxSourceLength);
    }
}
