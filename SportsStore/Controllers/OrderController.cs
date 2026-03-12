using Microsoft.AspNetCore.Mvc;
using SportsStore.Models;
using SportsStore.Services;

namespace SportsStore.Controllers {

    public class OrderController : Controller {
        private IOrderRepository repository;
        private Cart cart;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderRepository repoService, Cart cartService,
                IPaymentService paymentService, ILogger<OrderController> logger) {
            repository = repoService;
            cart = cartService;
            _paymentService = paymentService;
            _logger = logger;
        }

        public ViewResult Checkout() {
            _logger.LogInformation("Checkout page requested with {CartItemCount} items in cart",
                cart.Lines.Count());
            return View(new Order());
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(Order order) {
            if (cart.Lines.Count() == 0) {
                _logger.LogWarning("Checkout attempted with empty cart");
                ModelState.AddModelError("", "Sorry, your cart is empty!");
            }
            if (ModelState.IsValid) {
                order.Lines = cart.Lines.ToArray();
                repository.SaveOrder(order);

                _logger.LogInformation("Order {OrderId} created for {CustomerName} with {ItemCount} items, total {OrderTotal:C}",
                    order.OrderID, order.Name, order.Lines.Count,
                    cart.ComputeTotalValue());

                try {
                    var baseUrl = $"{Request.Scheme}://{Request.Host}";
                    var totalAmount = cart.ComputeTotalValue();
                    var redirectUrl = await _paymentService.CreateCheckoutSessionAsync(
                        totalAmount, order.OrderID.ToString(), baseUrl);

                    // Store order ID in TempData for payment callback
                    TempData["OrderId"] = order.OrderID;

                    _logger.LogInformation("Redirecting to Stripe checkout for order {OrderId}", order.OrderID);
                    return Redirect(redirectUrl);
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Stripe payment failed for order {OrderId}", order.OrderID);
                    TempData["PaymentError"] = "Payment processing failed. Please try again.";
                    return RedirectToAction("PaymentFailed");
                }
            } else {
                _logger.LogWarning("Checkout validation failed for {CustomerName}: {Errors}",
                    order.Name,
                    string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)));
                return View();
            }
        }

        public async Task<IActionResult> PaymentSuccess(string sessionId) {
            _logger.LogInformation("Payment success callback received for session {SessionId}", sessionId);

            try {
                var isPaid = await _paymentService.VerifyPaymentAsync(sessionId);

                if (isPaid) {
                    var paymentIntentId = await _paymentService.GetPaymentIntentIdAsync(sessionId);
                    var orderId = TempData["OrderId"] as int?;

                    if (orderId.HasValue) {
                        var order = repository.Orders.FirstOrDefault(o => o.OrderID == orderId.Value);
                        if (order != null) {
                            order.PaymentIntentId = paymentIntentId;
                            order.PaymentComplete = true;
                            repository.SaveOrder(order);

                            _logger.LogInformation("Payment confirmed for order {OrderId}, PaymentIntent {PaymentIntentId}",
                                orderId, paymentIntentId);
                        }
                    }

                    cart.Clear();
                    return RedirectToPage("/Completed", new { orderId });
                }

                _logger.LogWarning("Payment verification failed for session {SessionId}", sessionId);
                return RedirectToAction("PaymentFailed");
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error verifying payment for session {SessionId}", sessionId);
                return RedirectToAction("PaymentFailed");
            }
        }

        public IActionResult PaymentCancelled() {
            _logger.LogWarning("Payment was cancelled by the user");
            return View();
        }

        public IActionResult PaymentFailed() {
            _logger.LogWarning("Displaying payment failed page");
            return View();
        }
    }
}
