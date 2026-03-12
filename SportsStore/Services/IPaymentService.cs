namespace SportsStore.Services {

    public interface IPaymentService {
        Task<string> CreateCheckoutSessionAsync(decimal amount, string orderId, string baseUrl);
        Task<bool> VerifyPaymentAsync(string sessionId);
        Task<string?> GetPaymentIntentIdAsync(string sessionId);
    }
}
