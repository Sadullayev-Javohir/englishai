using FluentValidation;

namespace Application.Reading.Admin.DeleteReadingPassage;

public sealed class DeleteReadingPassageCommandValidator : AbstractValidator<DeleteReadingPassageCommand>
{
    public DeleteReadingPassageCommandValidator()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
    }
}
