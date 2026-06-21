using FluentValidation;

namespace Application.Admin.GetServerDiagnostics;

public sealed class GetServerDiagnosticsQueryValidator : AbstractValidator<GetServerDiagnosticsQuery>
{
    public GetServerDiagnosticsQueryValidator()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}
