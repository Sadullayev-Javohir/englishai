using FluentValidation;

namespace Application.Developer.ListApiKeys;

public sealed class ListDeveloperApiKeysQueryValidator : AbstractValidator<ListDeveloperApiKeysQuery>
{
    public ListDeveloperApiKeysQueryValidator() => RuleFor(x => x.UserId).NotEmpty();
}
