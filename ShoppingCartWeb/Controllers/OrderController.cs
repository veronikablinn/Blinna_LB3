using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShoppingCart.DataAccess.Repositories;
using ShoppingCart.DataAccess.ViewModels;
using ShoppingCart.Utility;
using Stripe;
using Stripe.Checkout;
using Moq;
using Xunit;
using ShoppingCart.Models;
using System.Linq.Expressions;

namespace ShoppingCart.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private IUnitOfWork _unitOfWork;
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public OrderVM OrderDetails(int id)
        {
            OrderVM orderVM = new OrderVM()
            {
                OrderHeader = _unitOfWork.OrderHeader.GetT(x => x.Id == id, includeProperties: "ApplicationUser"),
                OrderDetails = _unitOfWork.OrderDetail.GetAll(includeProperties: "Product").Where(x => x.OrderHeaderId == id)
            };

            return orderVM;
        }


        [Authorize(Roles = WebSiteRole.Role_Admin + "," + WebSiteRole.Role_Employee)]
        public void SetToInProcess(OrderVM vm)
        {
            _unitOfWork.OrderHeader.UpdateStatus(vm.OrderHeader.Id, OrderStatus.StatusInProcess);
            _unitOfWork.Save();
        }

        [Authorize(Roles = WebSiteRole.Role_Admin + "," + WebSiteRole.Role_Employee)]
        public void SetToShipped(OrderVM vm)
        {
            var orderHeader = _unitOfWork.OrderHeader.GetT(x => x.Id == vm.OrderHeader.Id);
            orderHeader.Carrier = vm.OrderHeader.Carrier;
            orderHeader.TrackingNumber = vm.OrderHeader.TrackingNumber;
            orderHeader.OrderStatus = OrderStatus.StatusShipped;
            orderHeader.DateOfShipping = DateTime.Now;

            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.Save();
        }

        [Authorize(Roles = WebSiteRole.Role_Admin + "," + WebSiteRole.Role_Employee)]
        public void SetToCancelOrder(OrderVM vm)
        {
            var orderHeader = _unitOfWork.OrderHeader.GetT(x => x.Id == vm.OrderHeader.Id);
            if (orderHeader.PaymentStatus == PaymentStatus.StatusApproved)
            {
                var refundOptions = new RefundCreateOptions()
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.PaymentIntentId
                };
                var service = new RefundService();
                Refund refund = service.Create(refundOptions);
                _unitOfWork.OrderHeader.UpdateStatus(vm.OrderHeader.Id, OrderStatus.StatusCancelled);
            }
            else
            {
                _unitOfWork.OrderHeader.UpdateStatus(vm.OrderHeader.Id, OrderStatus.StatusCancelled);
            }
            _unitOfWork.Save();
        }


    }


    public class OrderControllerTests
    {
        [Fact]

        // 1-test
        public void Order_details_returns_valid_view_model()
        {
            // Arrange
            var unit_of_work_mock = new Mock<IUnitOfWork>();
            var order_header = new OrderHeader { Id = 1, ApplicationUser = new ApplicationUser() };
            var order_details = new List<OrderDetail> { new OrderDetail { OrderHeaderId = 1 } };
            unit_of_work_mock.Setup(uow => uow.OrderHeader.GetT(It.IsAny<Expression<Func<OrderHeader, bool>>>(), It.IsAny<string>())).Returns(order_header);
            unit_of_work_mock.Setup(uow => uow.OrderDetail.GetAll(It.IsAny<string>())).Returns(order_details.AsQueryable());
            var controller = new OrderController(unit_of_work_mock.Object);

            // Act
            var result = controller.OrderDetails(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(order_header, result.OrderHeader);
            Assert.Equal(order_details, result.OrderDetails.ToList());
        }

        [Fact]
        // 2-test
        public void SetToInProcess_UpdatesOrderStatus()
        {
            // Arrange
            var unit_of_work_mock = new Mock<IUnitOfWork>();
            var order_header = new OrderHeader { Id = 1 };
            var orderVM = new OrderVM { OrderHeader = order_header };
            var orderHeaderRepositoryMock = new Mock<IOrderHeaderRepository>();
            var controller = new OrderController(unit_of_work_mock.Object);

            unit_of_work_mock.SetupGet(uow => uow.OrderHeader).Returns(orderHeaderRepositoryMock.Object);

            // Act
            controller.SetToInProcess(orderVM);

            // Assert
            orderHeaderRepositoryMock.Verify(repo => repo.UpdateStatus(order_header.Id, OrderStatus.StatusInProcess, null), Times.Once);
            unit_of_work_mock.Verify(uow => uow.Save(), Times.Once);
        }

        [Fact]
        // 3-test
        public void SetToShipped_UpdatesOrderStatusToShipped()
        {
            // Arrange
            var unit_of_work_mock = new Mock<IUnitOfWork>();
            var orderHeaderRepositoryMock = new Mock<IOrderHeaderRepository>();
            var controller = new OrderController(unit_of_work_mock.Object);

            var order_header = new OrderHeader { Id = 1, Carrier = "TestCarrier", TrackingNumber = "TestTrackingNumber", OrderStatus = OrderStatus.StatusShipped, DateOfShipping = DateTime.Now };

            unit_of_work_mock.Setup(uow => uow.OrderHeader.GetT(It.IsAny<Expression<Func<OrderHeader, bool>>>(), It.IsAny<string>())).Returns(order_header);
            // Act
            controller.SetToShipped(new OrderVM { OrderHeader = order_header });

            // Assert
            unit_of_work_mock.Verify(uow => uow.OrderHeader.GetT(It.IsAny<Expression<Func<OrderHeader, bool>>>(), null), Times.Once);
            unit_of_work_mock.Verify(uow => uow.OrderHeader.Update(It.IsAny<OrderHeader>()), Times.Once);
            unit_of_work_mock.Verify(uow => uow.Save(), Times.Once);
        }

    }

}
