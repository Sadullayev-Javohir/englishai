using FluentValidation;

namespace Application.Admin.DeleteRecentLog;

public sealed class DeleteRecentLogCommandValidator : AbstractValidator<DeleteRecentLogCommand>
{
    public DeleteRecentLogCommandValidator()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x.LogId).NotEmpty();
    }
}
