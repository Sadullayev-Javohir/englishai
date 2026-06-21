using FluentValidation;

namespace Application.Subscription.ConfirmPayment;

public sealed class ConfirmPaymentCommandValidator : AbstractValidator<ConfirmPaymentCommand>
{
    public ConfirmPaymentCommandValidator()
    {
        RuleFor(x => x.TransactionId).NotEmpty();
    }
}
