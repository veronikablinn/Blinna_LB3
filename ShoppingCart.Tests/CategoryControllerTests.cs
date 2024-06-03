using Microsoft.AspNetCore.Mvc;
using Moq;
using ShoppingCart.DataAccess.Repositories;
using ShoppingCart.DataAccess.ViewModels;
using ShoppingCart.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;

namespace ShoppingCart.Web.Areas.Admin.Controllers.Tests
{
    public class CategoryControllerTests
    {
        // Тест 1 - Перевірка видалення категорії
        [Fact]
        public void DeleteCategory_Check()
        {
            int categoryId = 1;
            var category = new Category { Id = categoryId, Name = "Category 1" };

            var repositoryMock = new Mock<ICategoryRepository>();
            repositoryMock.Setup(r => r.GetT(It.IsAny<Expression<Func<Category, bool>>>(), null))
                          .Returns(category);

            var unitOfWorkMock = new Mock<IUnitOfWork>();
            unitOfWorkMock.Setup(uow => uow.Category).Returns(repositoryMock.Object);

            var controller = new ShoppingCart.Web.Areas.Admin.Controllers.CategoryController(unitOfWorkMock.Object);

            controller.DeleteData(categoryId);

            repositoryMock.Verify(r => r.Delete(category), Times.Once);
            unitOfWorkMock.Verify(uow => uow.Save(), Times.Once);
        }

        // Тест 2 - Перевірка виключення при створенні/оновленні з невалідною моделлю
        [Fact]
        public void CreateUpdateInvalidModel_Check()
        {
            var categoryVM = new CategoryVM { Category = new Category { Id = 0, Name = "" } };

            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var controller = new ShoppingCart.Web.Areas.Admin.Controllers.CategoryController(unitOfWorkMock.Object);
            controller.ModelState.AddModelError("Name", "Required");

            var exception = Assert.Throws<Exception>(() => controller.CreateUpdate(categoryVM));
            Assert.Equal("Model is invalid", exception.Message);
        }

        // Тест 3 - Перевірка отримання всіх категорій
        [Fact]
        public void GetReturnsAllCategories_Check()
        {
            var expectedCategories = new List<Category> { new Category { Id = 1, Name = "Category 1" }, new Category { Id = 2, Name = "Category 2" } };
            var repositoryMock = new Mock<ICategoryRepository>();
            repositoryMock.Setup(r => r.GetAll(null)).Returns(expectedCategories);

            var unitOfWorkMock = new Mock<IUnitOfWork>();
            unitOfWorkMock.Setup(uow => uow.Category).Returns(repositoryMock.Object);

            var controller = new ShoppingCart.Web.Areas.Admin.Controllers.CategoryController(unitOfWorkMock.Object);

            var result = controller.Get();

            Assert.Equal(expectedCategories, result.Categories);
        }

        // Тест 4 - Перевірка отримання категорії за Id
        [Fact]
        public void GetCategoryById_CHeck()
        {
            int categoryId = 1;
            var expectedCategory = new Category { Id = categoryId, Name = "Category 1" };

            var repositoryMock = new Mock<ICategoryRepository>();
            repositoryMock.Setup(r => r.GetT(It.IsAny<Expression<Func<Category, bool>>>(), null))
                          .Returns(expectedCategory);

            var unitOfWorkMock = new Mock<IUnitOfWork>();
            unitOfWorkMock.Setup(uow => uow.Category).Returns(repositoryMock.Object);

            var controller = new ShoppingCart.Web.Areas.Admin.Controllers.CategoryController(unitOfWorkMock.Object);

            var result = controller.Get(categoryId);

            Assert.Equal(expectedCategory, result.Category);
        }
    }
}
