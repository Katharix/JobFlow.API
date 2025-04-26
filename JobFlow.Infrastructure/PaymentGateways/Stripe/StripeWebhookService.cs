using JobFlow.Business.DI;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Enums;
using JobFlow.Infrastructure.PaymentGateways.Stripe.StripeModels;
using Stripe;
using Stripe.Checkout;

namespace JobFlow.Infrastructure.PaymentGateways.Stripe
{
    [ScopedService]
    public class StripeWebhookService : IStripeWebhookService
    {
        private readonly IPaymentProfileService _paymentProfileService;
        private readonly ISubscriptionRecordService _subscriptionRecordService;

        public StripeWebhookService(
            IPaymentProfileService paymentProfileService,
            ISubscriptionRecordService subscriptionRecordService)
        {
            _paymentProfileService = paymentProfileService;
            _subscriptionRecordService = subscriptionRecordService;
        }
        public async Task HandleEventAsync(Event stripeEvent)
        {
            switch (stripeEvent.Type)
            {
                case StripeEvents.CheckoutSessionCompleted:
                    {
                        var session = stripeEvent.Data.Object as Session;
                        await HandleCheckoutSessionAsync(session);
                        break;
                    }
                case StripeEvents.PaymentIntentSucceeded:
                    {
                        var intent = stripeEvent.Data.Object as PaymentIntent;
                        await HandlePaymentIntentAsync(intent);
                        break;
                    }
                case StripeEvents.InvoicePaymentSucceeded:
                    {
                        var invoice = stripeEvent.Data.Object as Invoice;
                        // Optional: Log renewal payment, notify user, etc.
                        break;
                    }

                case StripeEvents.InvoicePaymentFailed:
                    {
                        var invoice = stripeEvent.Data.Object as Invoice;
                        var customerId = invoice.CustomerId;
                        // Optional: Notify user or flag account
                        break;
                    }

                case StripeEvents.CustomerSubscriptionUpdated:
                    {
                        var updated = stripeEvent.Data.Object as Subscription;
                        // Optional: Track status changes or plan upgrades
                        break;
                    }

                case StripeEvents.CustomerSubscriptionDeleted:
                    {
                        var deletedSub = stripeEvent.Data.Object as Subscription;
                        await _subscriptionRecordService.CancelAsync(
                            deletedSub.Id,
                            DateTime.UtcNow
                        );
                        break;
                    }
            }
        }
        private async Task HandleCheckoutSessionAsync(Session session)
        {
            var subscriptionService = new SubscriptionService();
            var subscription = await subscriptionService.GetAsync(session.SubscriptionId);

            var ownerId = subscription.Metadata["ownerId"];
            var ownerType = subscription.Metadata["ownerType"];
            var paymentCustomerId = subscription.Metadata["customerId"];

            var paymentProfileResult = await _paymentProfileService.CreateAsync(
                Guid.Parse(ownerId),
                Enum.Parse<PaymentEntityType>(ownerType),
                PaymentProvider.Stripe,
                paymentCustomerId
            );

            await _subscriptionRecordService.CreateAsync(
                paymentProfileResult.Value.Id,
                subscription.Id,
                subscription.Items.Data.First().Price.Id,
                subscription.Status
            );
        }

        private async Task HandlePaymentIntentAsync(PaymentIntent intent)
        {

        }
    }
    public interface IStripeWebhookService
    {
        Task HandleEventAsync(Event stripeEvent);
    }
}
