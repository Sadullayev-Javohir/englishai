using Domain.Common;

namespace Domain.Subscription;

/// <summary>
/// A single payment attempt for a subscription plan (PROJECT-SPEC H.3). Created Pending
/// when checkout starts; the provider's confirmation (webhook) completes it, which is what
/// activates Premium. The <see cref="TransactionId"/> is the idempotency key the provider
/// callback references.
/// </summary>
public sealed class Payment
{
    private Payment()
    {
        TransactionId = null!;
    }

    private Payment(
        Guid learnerId,
        SubscriptionPlan plan,
        int amountUzs,
        PaymentProvider provider,
        string transactionId,
        DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        LearnerId = learnerId;
        Plan = plan;
        AmountUzs = amountUzs;
        Provider = provider;
        TransactionId = transactionId;
        Status = PaymentStatus.Pending;
        CreatedAt = now;
    }

    public Guid Id { get; private set; }
    public Guid LearnerId { get; private set; }
    public SubscriptionPlan Plan { get; private set; }
    public int AmountUzs { get; private set; }
    public PaymentProvider Provider { get; private set; }

    /// <summary>Provider-scoped transaction identifier; unique idempotency key for confirmation.</summary>
    public string TransactionId { get; private set; }

    public PaymentStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public long? ProviderTransactionId { get; private set; }
    public long? ProviderPaymentId { get; private set; }
    public int? ProviderPrepareId { get; private set; }

    public static Payment Start(
        Guid learnerId,
        SubscriptionPlan plan,
        PaymentProvider provider,
        string transactionId,
        DateTimeOffset now,
        int? amountOverrideUzs = null)
    {
        if (learnerId == Guid.Empty)
            throw new DomainException("Learner id must not be empty.");
        if (string.IsNullOrWhiteSpace(transactionId))
            throw new DomainException("Transaction id must not be empty.");
        if (amountOverrideUzs is < 0)
            throw new DomainException("Amount override must not be negative.");

        var amount = amountOverrideUzs ?? SubscriptionPricing.PriceUzs(plan);
        return new Payment(learnerId, plan, amount, provider, transactionId, now);
    }

    /// <summary>Marks the payment completed. Idempotent: completing an already-completed payment is a no-op.</summary>
    public void Complete(DateTimeOffset now)
    {
        if (Status == PaymentStatus.Completed)
            return;
        if (Status == PaymentStatus.Failed)
            throw new DomainException("A failed payment cannot be completed.");

        Status = PaymentStatus.Completed;
        CompletedAt = now;
    }

    public void Prepare(long providerTransactionId, long providerPaymentId)
    {
        if (providerTransactionId <= 0)
            throw new DomainException("Provider transaction id must be positive.");
        if (providerPaymentId <= 0)
            throw new DomainException("Provider payment id must be positive.");
        if (Status == PaymentStatus.Failed)
            throw new DomainException("A failed payment cannot be prepared.");
        if (ProviderTransactionId.HasValue && ProviderTransactionId != providerTransactionId)
            throw new DomainException("Payment is already linked to another provider transaction.");
        if (ProviderPaymentId.HasValue && ProviderPaymentId != providerPaymentId)
            throw new DomainException("Payment is already linked to another provider payment.");

        ProviderTransactionId = providerTransactionId;
        ProviderPaymentId = providerPaymentId;
        ProviderPrepareId ??= CreateProviderPrepareId(Id);
    }

    public void Fail(DateTimeOffset now)
    {
        if (Status == PaymentStatus.Completed)
            throw new DomainException("A completed payment cannot be failed.");

        Status = PaymentStatus.Failed;
        CompletedAt = now;
    }

    private static int CreateProviderPrepareId(Guid paymentId)
    {
        var value = BitConverter.ToInt32(paymentId.ToByteArray(), 0) & int.MaxValue;
        return value == 0 ? 1 : value;
    }
}
