using Stripe;
using Stripe.Checkout;

namespace SportsStore.Services {

    public class StripePaymentService : IPaymentService {
        private readonly ILogger<StripePaymentService> _logger;

        public StripePaymentService(ILogger<StripePaymentService> logger) {
            _logger = logger;
        }

        public async Task<string> CreateCheckoutSessionAsync(decimal amount, string orderId, string baseUrl) {
            var options = new SessionCreateOptions {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions> {
                    new SessionLineItemOptions {
                        PriceData = new SessionLineItemPriceDataOptions {
                            Currency = "eur",
                            ProductData = new SessionLineItemPriceDataProductDataOptions {
                                Name = $"SportsStore Order #{orderId}"
                            },
                            UnitAmountDecimal = amount * 100
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = $"{baseUrl}/Order/PaymentSuccess?sessionId={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{baseUrl}/Order/PaymentCancelled"
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("Stripe checkout session {SessionId} created for order {OrderId}, amount {Amount:C}",
                session.Id, orderId, amount);

            return session.Url!;
        }

        public async Task<bool> VerifyPaymentAsync(string sessionId) {
            var service = new SessionService();
            var session = await service.GetAsync(sessionId);

            _logger.LogInformation("Stripe session {SessionId} payment status: {Status}",
                sessionId, session.PaymentStatus);

            return session.PaymentStatus == "paid";
        }

        public async Task<string?> GetPaymentIntentIdAsync(string sessionId) {
            var service = new SessionService();
            var session = await service.GetAsync(sessionId);
            return session.PaymentIntentId;
        }
    }
}
