using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShoppingCart.DataAccess.Repositories;
using ShoppingCart.DataAccess.ViewModels;
using Xunit;
using Moq;
using ShoppingCart.Models;
using System.Linq.Expressions;


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
                _unitofWork.Save();;
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


    public class CategoryControllerTests
    {

        
        public static IEnumerable<object[]> Data_category =>
        new List<object[]>
        {
            new object[] { 0, "New category" }, 
            new object[] { 1, "Existing category" }
        };

        [Theory]
        [MemberData(nameof(Data_category))]

        // 1-test
        public void Create_new_categories_or_Update_categories(int categoryId, string categoryName)
        {
            // Arrange
            var category = new Category { Id = categoryId, Name = categoryName };
            var categoryVM = new CategoryVM { Category = category };

            var repository_mock = new Mock<ICategoryRepository>();
            var unit_of_work_mock = new Mock<IUnitOfWork>();
            unit_of_work_mock.Setup(u => u.Category).Returns(repository_mock.Object);

            var controller = new CategoryController(unit_of_work_mock.Object);

            // Act
            controller.CreateUpdate(categoryVM);

            // Assert
            if (categoryId == 0)
            {
                repository_mock.Verify(r => r.Add(category), Times.Once);
            }
            else
            {
                repository_mock.Verify(r => r.Update(category), Times.Once);
            }

            unit_of_work_mock.Verify(u => u.Save(), Times.Once);
        }

        // 2-test
        [Fact]
        public void Get_returns_all_categories()
        {
            // Arrange
            var expectedCategories = new List<Category> { new Category { Id = 1, Name = "Category 1" }, new Category { Id = 2, Name = "Category 2" } };
            var repository_mock = new Mock<ICategoryRepository>();
            repository_mock.Setup(r => r.GetAll(null)).Returns(expectedCategories);

            var unit_of_work_mock = new Mock<IUnitOfWork>();
            unit_of_work_mock.Setup(uow => uow.Category).Returns(repository_mock.Object);

            var controller = new CategoryController(unit_of_work_mock.Object);

            // Act
            var result = controller.Get();

            // Assert
            Assert.Equal(expectedCategories, result.Categories);
        }

        // 3-test
        [Fact]
        public void Get_returns_category_with_specified_Id()
        {
            // Arrange
            int categoryId = 1;
            var expectedCategory = new Category { Id = categoryId, Name = "Category 1" };

            var repository_mock = new Mock<ICategoryRepository>();
            repository_mock.Setup(r => r.GetT(It.IsAny<Expression<Func<Category, bool>>>(), null))
                          .Returns(expectedCategory);

            var unit_of_work_mock = new Mock<IUnitOfWork>();
            unit_of_work_mock.Setup(uow => uow.Category).Returns(repository_mock.Object);

            var controller = new CategoryController(unit_of_work_mock.Object);

            // Act
            var result = controller.Get(categoryId);

            // Assert
            Assert.Equal(expectedCategory, result.Category);
        }

    }
}
