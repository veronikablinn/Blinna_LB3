using Microsoft.AspNetCore.Mvc;
using ShoppingCart.DataAccess.Repositories;
using ShoppingCart.DataAccess.ViewModels;
using Xunit;
using Moq;
using ShoppingCart.Models;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using ShoppingCart.Web.Areas.Admin.Controllers;

namespace ShoppingCart.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private IUnitOfWork _unitofWork;

        public CategoryController(IUnitOfWork unitofWork)
        {
            _unitofWork = unitofWork;
        }

        [HttpGet]
        public CategoryVM Get()
        {
            CategoryVM categoryVM = new CategoryVM();
            categoryVM.Categories = _unitofWork.Category.GetAll();
            return categoryVM;
        }

        [HttpGet]
        public CategoryVM Get(int id)
        {
            CategoryVM vm = new CategoryVM();
            vm.Category = _unitofWork.Category.GetT(x => x.Id == id);
            return vm;
        }

        [HttpPost]
        public void CreateUpdate(CategoryVM vm)
        {
            if (ModelState.IsValid)
            {
                if (vm.Category.Id == 0)
                {
                    _unitofWork.Category.Add(vm.Category);
                }
                else
                {
                    _unitofWork.Category.Update(vm.Category);
                }
                _unitofWork.Save();
            }
            else
            {
                throw new Exception("Model is invalid");
            }
        }

        [HttpPost, ActionName("Delete")]
        public void DeleteData(int? id)
        {
            var category = _unitofWork.Category.GetT(x => x.Id == id);
            if (category == null)
            {
                throw new Exception("Category not found");
            }

            _unitofWork.Category.Delete(category);
            _unitofWork.Save();
        }
    }
}

namespace ShoppingCart.Tests
{
    public class CategoryControllerTests
    {
        // Тест 1
        [Fact]
        public void deleteData_removes()
        {
            int categoryId = 1;
            var category = new Category { Id = categoryId };
            var repository_mock = new Mock<ICategoryRepository>();
            repository_mock.Setup(r => r.GetT(It.IsAny<Expression<Func<Category, bool>>>(), null)).Returns(category);

            var unit_of_work_mock = new Mock<IUnitOfWork>();
            unit_of_work_mock.Setup(uow => uow.Category).Returns(repository_mock.Object);

            var controller = new CategoryController(unit_of_work_mock.Object);

            controller.DeleteData(categoryId);

            repository_mock.Verify(r => r.Delete(category), Times.Once);
            unit_of_work_mock.Verify(u => u.Save(), Times.Once);
        }

        // Тест 2
        [Fact]
        public void createUpdate_data()
        {
            var categoryVM = new CategoryVM { Category = new Category { Id = 0, Name = "" } }; // Invalid data
            var repository_mock = new Mock<ICategoryRepository>();
            var unit_of_work_mock = new Mock<IUnitOfWork>();
            unit_of_work_mock.Setup(u => u.Category).Returns(repository_mock.Object);

            var controller = new CategoryController(unit_of_work_mock.Object);
            controller.ModelState.AddModelError("Name", "Required");

            var exception = Assert.Throws<Exception>(() => controller.CreateUpdate(categoryVM));
            Assert.Equal("Model is invalid", exception.Message);
        }

        // Тест 3
        [Fact]
        public void nullWhenCategory_NotFound()
        {
            int categoryId = 99; 
            var repository_mock = new Mock<ICategoryRepository>();
            repository_mock.Setup(r => r.GetT(It.IsAny<Expression<Func<Category, bool>>>(), null)).Returns((Category)null);

            var unit_of_work_mock = new Mock<IUnitOfWork>();
            unit_of_work_mock.Setup(uow => uow.Category).Returns(repository_mock.Object);

            var controller = new CategoryController(unit_of_work_mock.Object);

            var result = controller.Get(categoryId);

            Assert.Null(result.Category);
        }
    }
}
