using Domain.Developer;
using FluentValidation;

namespace Application.Developer.CreateApiKey;

public sealed class CreateDeveloperApiKeyCommandValidator : AbstractValidator<CreateDeveloperApiKeyCommand>
{
    public CreateDeveloperApiKeyCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(DeveloperApiKey.MaxNameLength);
    }
}
