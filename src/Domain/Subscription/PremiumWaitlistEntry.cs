using System.Text.RegularExpressions;
using Domain.Common;

namespace Domain.Subscription;

public enum WaitlistContactKind
{
    Email = 0,
    Phone = 1,
}

/// <summary>
/// Someone who asked to be told when Premium can actually be bought.
///
/// While no payment provider is connected there is nothing to sell, but the interest is the only
/// real demand signal available: a learner who leaves a contact after seeing the price has told us
/// more than any pageview can. When checkout opens, this list is the first thing to email.
/// </summary>
public sealed class PremiumWaitlistEntry
{
    /// <summary>Uzbek mobile numbers in E.164, which is what Click and Payme both expect.</summary>
    private static readonly Regex PhonePattern = new(@"^\+998\d{9}$", RegexOptions.Compiled);

    /// <summary>Deliberately permissive - the confirmation email is the real validator.</summary>
    private static readonly Regex EmailPattern =
        new(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$", RegexOptions.Compiled);

    private PremiumWaitlistEntry(
        Guid id,
        string contact,
        WaitlistContactKind contactKind,
        SubscriptionPlan? interestedPlan,
        Guid? learnerId,
        string? source,
        DateTimeOffset createdAt)
    {
        Id = id;
        Contact = contact;
        ContactKind = contactKind;
        InterestedPlan = interestedPlan;
        LearnerId = learnerId;
        Source = source;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    /// <summary>Normalized, and unique, so a re-submit updates rather than duplicates.</summary>
    public string Contact { get; private set; } = string.Empty;

    public WaitlistContactKind ContactKind { get; private set; }

    /// <summary>Which plan they were looking at - the closest thing to a price signal we get.</summary>
    public SubscriptionPlan? InterestedPlan { get; private set; }

    /// <summary>Set when a signed-in learner left the interest; null for an anonymous visitor.</summary>
    public Guid? LearnerId { get; private set; }

    /// <summary>Where it came from (pricing page, paywall), for attribution.</summary>
    public string? Source { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static PremiumWaitlistEntry Create(
        string contact,
        SubscriptionPlan? interestedPlan,
        Guid? learnerId,
        string? source,
        DateTimeOffset now)
    {
        var (normalized, kind) = Normalize(contact);
        return new PremiumWaitlistEntry(
            Guid.NewGuid(),
            normalized,
            kind,
            interestedPlan,
            learnerId == Guid.Empty ? null : learnerId,
            string.IsNullOrWhiteSpace(source) ? null : source.Trim()[..Math.Min(source.Trim().Length, 64)],
            now);
    }

    /// <summary>
    /// Accepts an email or an Uzbek mobile number and returns the canonical form used as the unique
    /// key. Phone numbers are written half a dozen ways locally (spaces, dashes, a leading 998 or a
    /// leading 0), so they are reduced to E.164 before storage - otherwise the same person joins the
    /// list four times and gets four emails.
    /// </summary>
    public static (string Contact, WaitlistContactKind Kind) Normalize(string contact)
    {
        var trimmed = (contact ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            throw new DomainException("A contact is required.");
        if (trimmed.Length > 128)
            throw new DomainException("The contact is too long.");

        if (trimmed.Contains('@'))
        {
            var email = trimmed.ToLowerInvariant();
            if (!EmailPattern.IsMatch(email))
                throw new DomainException("The email address is not valid.");
            return (email, WaitlistContactKind.Email);
        }

        var digits = new string(trimmed.Where(char.IsAsciiDigit).ToArray());
        var phone = digits.Length switch
        {
            9 => $"+998{digits}",                                    // 901234567
            12 when digits.StartsWith("998") => $"+{digits}",         // 998901234567
            10 when digits.StartsWith('0') => $"+998{digits[1..]}",   // 0901234567
            _ => $"+{digits}",
        };

        if (!PhonePattern.IsMatch(phone))
            throw new DomainException("The phone number is not a valid Uzbek mobile number.");

        return (phone, WaitlistContactKind.Phone);
    }
}
