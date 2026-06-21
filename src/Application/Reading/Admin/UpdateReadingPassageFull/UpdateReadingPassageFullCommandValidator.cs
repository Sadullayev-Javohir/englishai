using Domain.Assessment;
using Domain.Reading;
using FluentValidation;

namespace Application.Reading.Admin.UpdateReadingPassageFull;

public sealed class UpdateReadingPassageFullCommandValidator : AbstractValidator<UpdateReadingPassageFullCommand>
{
    public UpdateReadingPassageFullCommandValidator()
    {
        RuleFor(command => command.RequestingUserId).NotEmpty();
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Payload.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Payload.Topic).NotEmpty().MaximumLength(60);
        RuleFor(command => command.Payload.Category).NotEmpty().MaximumLength(80);
        RuleFor(command => command.Payload.Level)
            .Must(level => Enum.TryParse<CefrLevel>(level, true, out _))
            .WithMessage("Level must be a valid CEFR level.");
        RuleFor(command => command.Payload.Status)
            .Must(status => Enum.TryParse<ReadingPassageStatus>(status, true, out _))
            .WithMessage("Status must be Pending or Filled.");
        RuleFor(command => command.Payload.Sections).NotNull();
        RuleFor(command => command.Payload.Vocabulary).NotNull();
        RuleFor(command => command.Payload.Questions).NotNull();
        RuleForEach(command => command.Payload.Vocabulary).ChildRules(entry =>
        {
            entry.RuleFor(item => item.Word).NotEmpty().MaximumLength(120);
            entry.RuleFor(item => item.Translation).NotEmpty().MaximumLength(200);
        });
        RuleForEach(command => command.Payload.Questions).ChildRules(question =>
        {
            question.RuleFor(item => item.Prompt).NotEmpty().MaximumLength(2000);
            question.RuleFor(item => item.Options).NotNull().Must(options => options.Count >= ReadingQuestion.MinOptions);
            question.RuleFor(item => item).Must(item =>
                item.Options is not null && item.CorrectOptionIndex >= 0 && item.CorrectOptionIndex < item.Options.Count)
                .WithMessage("Correct answer must reference an existing option.");
        });
    }
}
