using FluentValidation;

namespace Application.Developer.RevokeApiKey;

public sealed class RevokeDeveloperApiKeyCommandValidator : AbstractValidator<RevokeDeveloperApiKeyCommand>
{
    public RevokeDeveloperApiKeyCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ApiKeyId).NotEmpty();
    }
}
