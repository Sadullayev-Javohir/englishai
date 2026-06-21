using FluentValidation;

namespace Application.Retention.GetFeatureAssignments;

public sealed class GetFeatureAssignmentsQueryValidator : AbstractValidator<GetFeatureAssignmentsQuery>
{
    public GetFeatureAssignmentsQueryValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
