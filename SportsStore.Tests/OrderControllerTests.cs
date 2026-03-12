using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SportsStore.Controllers;
using SportsStore.Models;
using SportsStore.Services;
using Xunit;

namespace SportsStore.Tests {

    public class OrderControllerTests {

        private OrderController CreateController(Mock<IOrderRepository> mock, Cart cart) {
            var paymentMock = new Mock<IPaymentService>();
            paymentMock.Setup(p => p.CreateCheckoutSessionAsync(
                    It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("https://checkout.stripe.com/test");

            var controller = new OrderController(mock.Object, cart,
                paymentMock.Object, NullLogger<OrderController>.Instance);

            controller.ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext()
            };
            controller.TempData = new TempDataDictionary(
                controller.ControllerContext.HttpContext,
                Mock.Of<ITempDataProvider>());

            return controller;
        }

        [Fact]
        public async Task Cannot_Checkout_Empty_Cart() {
            // Arrange
            Mock<IOrderRepository> mock = new Mock<IOrderRepository>();
            Cart cart = new Cart();
            Order order = new Order();
            OrderController target = CreateController(mock, cart);

            // Act
            ViewResult? result = await target.Checkout(order) as ViewResult;

            // Assert
            mock.Verify(m => m.SaveOrder(It.IsAny<Order>()), Times.Never);
            Assert.True(string.IsNullOrEmpty(result?.ViewName));
            Assert.False(result?.ViewData.ModelState.IsValid);
        }

        [Fact]
        public async Task Cannot_Checkout_Invalid_ShippingDetails() {
            // Arrange
            Mock<IOrderRepository> mock = new Mock<IOrderRepository>();
            Cart cart = new Cart();
            cart.AddItem(new Product(), 1);
            OrderController target = CreateController(mock, cart);
            target.ModelState.AddModelError("error", "error");

            // Act
            ViewResult? result = await target.Checkout(new Order()) as ViewResult;

            // Assert
            mock.Verify(m => m.SaveOrder(It.IsAny<Order>()), Times.Never);
            Assert.True(string.IsNullOrEmpty(result?.ViewName));
            Assert.False(result?.ViewData.ModelState.IsValid);
        }

        [Fact]
        public async Task Can_Checkout_And_Submit_Order() {
            // Arrange
            Mock<IOrderRepository> mock = new Mock<IOrderRepository>();
            Cart cart = new Cart();
            cart.AddItem(new Product(), 1);
            OrderController target = CreateController(mock, cart);

            // Act
            RedirectResult? result = await target.Checkout(new Order()) as RedirectResult;

            // Assert
            mock.Verify(m => m.SaveOrder(It.IsAny<Order>()), Times.Once);
            Assert.Contains("stripe.com", result?.Url ?? "");
        }
    }
}
