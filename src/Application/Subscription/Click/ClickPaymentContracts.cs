namespace Application.Subscription.Click;

public sealed record ClickPrepareCommand(
    long ClickTransactionId,
    long ClickPaymentId,
    string MerchantTransactionId,
    decimal Amount,
    int Action,
    int Error,
    string ErrorNote)
    : MediatR.IRequest<ClickPrepareResult>;

public sealed record ClickPrepareResult(int MerchantPrepareId);

public sealed record ClickCompleteCommand(
    long ClickTransactionId,
    long ClickPaymentId,
    string MerchantTransactionId,
    int MerchantPrepareId,
    decimal Amount,
    int Action,
    int Error,
    string ErrorNote)
    : MediatR.IRequest<ClickCompleteResult>;

public sealed record ClickCompleteResult(int MerchantConfirmId);

public sealed class ClickPaymentException : Exception
{
    public ClickPaymentException(int errorCode, string errorNote)
        : base(errorNote)
    {
        ErrorCode = errorCode;
        ErrorNote = errorNote;
    }

    public int ErrorCode { get; }
    public string ErrorNote { get; }
}
