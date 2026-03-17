using JobFlow.Business.Models.DTOs;

namespace JobFlow.API.Validators;

public sealed class SetDefaultPaymentMethodRequestValidator : SafeRequestValidator<SetDefaultPaymentMethodRequest>
{
    public SetDefaultPaymentMethodRequestValidator() : base("PaymentMethodId") { }
}

public sealed class LinkConnectedAccountRequestValidator : SafeRequestValidator<LinkConnectedAccountRequest>
{
    public LinkConnectedAccountRequestValidator() : base("Code") { }
}

public sealed class DepositPaymentRequestDtoValidator : SafeRequestValidator<DepositPaymentRequestDto>
{
    public DepositPaymentRequestDtoValidator() : base("PaymentMethodId") { }
}

public sealed class CreatePaymentProfileRequestValidator : SafeRequestValidator<CreatePaymentProfileRequest>
{
    public CreatePaymentProfileRequestValidator() : base("Token") { }
}

public sealed class PaymentAdjustmentRequestDtoValidator : SafeRequestValidator<PaymentAdjustmentRequestDto>
{
    public PaymentAdjustmentRequestDtoValidator() : base("Reason") { }
}

public sealed class PaymentRefundRequestDtoValidator : SafeRequestValidator<PaymentRefundRequestDto>
{
    public PaymentRefundRequestDtoValidator() : base("Reason") { }
}

public sealed class CreateSubscriptionRequestValidator : SafeRequestValidator<CreateSubscriptionRequest>
{
    public CreateSubscriptionRequestValidator() : base("PriceId") { }
}

public sealed class CancelSubscriptionRequestValidator : SafeRequestValidator<CancelSubscriptionRequest>
{
    public CancelSubscriptionRequestValidator() : base("SubscriptionId") { }
}
