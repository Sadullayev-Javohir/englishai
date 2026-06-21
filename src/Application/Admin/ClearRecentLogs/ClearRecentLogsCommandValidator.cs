using FluentValidation;

namespace Application.Admin.ClearRecentLogs;

public sealed class ClearRecentLogsCommandValidator : AbstractValidator<ClearRecentLogsCommand>
{
    public ClearRecentLogsCommandValidator()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}
