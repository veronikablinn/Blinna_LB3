
using Moq;
using ShoppingCart.DataAccess.Repositories;
using ShoppingCart.DataAccess.ViewModels;
using ShoppingCart.Models;
using ShoppingCart.Utility;
using System.Linq.Expressions;
using Xunit;

namespace ShoppingCart.Web.Areas.Admin.Controllers
{
    public class OrderControllerTests
    {
        
        // Тест 2
        [Fact]
        public void UpdatesOrderStatus_Check()
        {
            var unit_of_work_mock = new Mock<IUnitOfWork>();
            var order_header = new OrderHeader { Id = 1 };
            var orderVM = new OrderVM { OrderHeader = order_header };
            var orderHeaderRepositoryMock = new Mock<IOrderHeaderRepository>();
            var controller = new OrderController(unit_of_work_mock.Object);

            unit_of_work_mock.SetupGet(uow => uow.OrderHeader).Returns(orderHeaderRepositoryMock.Object);

            controller.SetToInProcess(orderVM);

            orderHeaderRepositoryMock.Verify(repo => repo.UpdateStatus(order_header.Id, OrderStatus.StatusInProcess, null), Times.Once);
            unit_of_work_mock.Verify(uow => uow.Save(), Times.Once);
        }

        // Тест 3
        [Fact]
        public void UpdatesShippedStatus_Check()
        {
            var unit_of_work_mock = new Mock<IUnitOfWork>();
            var orderHeaderRepositoryMock = new Mock<IOrderHeaderRepository>();
            var controller = new OrderController(unit_of_work_mock.Object);

            var order_header = new OrderHeader { Id = 1, Carrier = "TestCarrier", TrackingNumber = "TestTrackingNumber", OrderStatus = OrderStatus.StatusShipped, DateOfShipping = DateTime.Now };

            unit_of_work_mock.Setup(uow => uow.OrderHeader.GetT(It.IsAny<Expression<Func<OrderHeader, bool>>>(), It.IsAny<string>())).Returns(order_header);
            controller.SetToShipped(new OrderVM { OrderHeader = order_header });

            unit_of_work_mock.Verify(uow => uow.OrderHeader.GetT(It.IsAny<Expression<Func<OrderHeader, bool>>>(), null), Times.Once);
            unit_of_work_mock.Verify(uow => uow.OrderHeader.Update(It.IsAny<OrderHeader>()), Times.Once);
            unit_of_work_mock.Verify(uow => uow.Save(), Times.Once);
        }
    }

    public class OrderController
    {
        private readonly IUnitOfWork _unitOfWork;

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

        public void SetToInProcess(OrderVM vm)
        {
            _unitOfWork.OrderHeader.UpdateStatus(vm.OrderHeader.Id, OrderStatus.StatusInProcess);
            _unitOfWork.Save();
        }

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
    }
}