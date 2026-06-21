using Domain.Analytics;
using FluentValidation;

namespace Application.Analytics.RecordStudyTime;

public sealed class RecordStudyTimeCommandValidator : AbstractValidator<RecordStudyTimeCommand>
{
    public RecordStudyTimeCommandValidator(TimeProvider clock)
    {
        RuleFor(c => c.LearnerId).NotEmpty();
        RuleFor(c => c.Skill).IsInEnum();

        // A single heartbeat is a small, capped increment - reject non-positive and bound the
        // upper end so a tampered client can't credit hours in one call (docs/development-guide.md rule 10).
        RuleFor(c => c.Seconds).InclusiveBetween(1, DailyStudyRecord.MaxHeartbeatSeconds);

        // The client supplies its local day; accept anything within two days of the server's UTC
        // day to allow for timezone offset (UZ is UTC+5) without trusting arbitrary dates.
        RuleFor(c => c.LocalDate).Must(date =>
        {
            var serverToday = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            return date >= serverToday.AddDays(-2) && date <= serverToday.AddDays(2);
        }).WithMessage("Study date must be close to the current date.");
    }
}
