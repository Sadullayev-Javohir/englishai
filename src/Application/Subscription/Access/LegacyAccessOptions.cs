namespace Application.Subscription.Access;

/// <summary>
/// Keeps the learners who joined before there was anything to buy on full access until buying is
/// actually possible.
///
/// They were given a thirty-day trial and no checkout. Cutting them to the free tier before a
/// payment provider is connected would take away what they signed up for and offer them no way to
/// keep it - the fastest possible way to lose the only cohort the product has.
///
/// The grant is deliberately conditional on payments being OFF, so it retires itself the day
/// checkout opens rather than becoming a permanent free tier nobody remembers granting.
/// </summary>
public sealed class LegacyAccessOptions
{
    public const string SectionName = "LegacyAccess";

    /// <summary>
    /// Accounts created strictly before this instant keep full access while payments are disabled.
    /// Null disables the grant entirely.
    /// </summary>
    public DateTimeOffset? GrantedForAccountsCreatedBefore { get; set; }

    public bool Applies(DateTimeOffset accountCreatedAt) =>
        GrantedForAccountsCreatedBefore is { } cutoff && accountCreatedAt < cutoff;
}
