using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Email;
using OrchardCore.Settings;
using OrchardCore.Users;
using OrchardCore.Users.Controllers;
using OrchardCore.Users.Events;
using OrchardCore.Users.Models;
using OrchardCore.Users.Services;
using OrchardCore.Users.ViewModels;
using Xunit;

namespace OrchardCore.Tests.OrchardCore.Users
{
    public class RegistrationControllerTests
    {
        [Fact]
        public async Task UsersShouldNotBeAbleToRegisterIfNotAllowed()
        {
            // Arrange
            var controller = SetupRegistrationController(new RegistrationSettings {
                UsersCanRegister = UserRegistrationType.NoRegistration
            });

            // Act & Assert
            var result = await controller.Register();
            Assert.IsType<NotFoundResult>(result);

            result = await controller.Register(new RegisterViewModel());
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UsersShouldBeAbleToRegisterIfAllowed()
        {
            // Arrange
            var controller = SetupRegistrationController();

            // Act & Assert
            var result = await controller.Register();
            Assert.IsType<ViewResult>(result);

            result = await controller.Register(new RegisterViewModel());
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task UsersShouldNotBeAbleToRegisterIfEmailIsDuplicate()
        {
            // Arrange
            var controller = SetupRegistrationController();

            // Act
            var result = await controller.Register(new RegisterViewModel { UserName = "SuperAdmin", Email = "admin@orchardcore.net" });
            result = await controller.Register(new RegisterViewModel { UserName = "Admin", Email = "admin@orchardcore.net" });

            // Assert
            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ViewData.ModelState.IsValid);
            Assert.True(controller.ViewData.ModelState["Email"].Errors.Count == 1);
            Assert.Equal("A user with the same email already exists.", controller.ViewData.ModelState["Email"].Errors[0].ErrorMessage);
        }

        private static Mock<SignInManager<TUser>> MockSignInManager<TUser>(UserManager<TUser> userManager = null) where TUser : class
        {
            var context = new Mock<HttpContext>();
            var manager = userManager ?? UsersMockHelper.MockUserManager<TUser>().Object;

            return new Mock<SignInManager<TUser>>(
                manager,
                new HttpContextAccessor { HttpContext = context.Object },
                Mock.Of<IUserClaimsPrincipalFactory<TUser>>(),
                null,
                null,
                null,
                null)
            { CallBase = true };
        }

        private static RegistrationController SetupRegistrationController()
            => SetupRegistrationController(new RegistrationSettings {
                UsersCanRegister = UserRegistrationType.AllowRegistration
            });

        private static RegistrationController SetupRegistrationController(RegistrationSettings registrationSettings)
        {
            var users = new List<IUser>();
            var mockUserManager = UsersMockHelper.MockUserManager<IUser>();
            mockUserManager.Setup(um => um.FindByEmailAsync(It.IsAny<string>()))
                .Returns<string>(e => {
                    var user = users.SingleOrDefault(u => (u as User).Email == e);

                    return Task.FromResult(user);
                });

            var mockSiteService = Mock.Of<ISiteService>(ss =>
                ss.GetSiteSettingsAsync() == Task.FromResult(
                    Mock.Of<ISite>(s => s.Properties == JObject.FromObject(new { RegistrationSettings = registrationSettings }))
                    )
            );
            var mockSmtpService = Mock.Of<ISmtpService>();
            var mockStringLocalizer = new Mock<IStringLocalizer<RegistrationController>>();
            mockStringLocalizer.Setup(l => l[It.IsAny<string>()])
                .Returns<string>(s => new LocalizedString(s, s));

            var userService = new Mock<IUserService>();
            userService.Setup(u => u.CreateUserAsync(It.IsAny<IUser>(), It.IsAny<string>(), It.IsAny<Action<string, string>>()))
                .Callback<IUser, string, Action<string, string>>((u, p, e) => users.Add(u));

            var controller = new RegistrationController(
                mockUserManager.Object,
                Mock.Of<IAuthorizationService>(),
                mockSiteService,
                Mock.Of<INotifier>(),
                new EmailAddressValidator(),
                Mock.Of<ILogger<RegistrationController>>(),
                Mock.Of<IHtmlLocalizer<RegistrationController>>(),
                mockStringLocalizer.Object);
            var mockServiceProvider = new Mock<IServiceProvider>();

            mockServiceProvider
                .Setup(x => x.GetService(typeof(ISmtpService)))
                .Returns(mockSmtpService);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(UserManager<IUser>)))
                .Returns(mockUserManager.Object);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(ISiteService)))
                .Returns(mockSiteService);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(IEnumerable<IRegistrationFormEvents>)))
                .Returns(Enumerable.Empty<IRegistrationFormEvents>());
            mockServiceProvider
                .Setup(x => x.GetService(typeof(IUserService)))
                .Returns(userService.Object);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(SignInManager<IUser>)))
                .Returns(MockSignInManager(mockUserManager.Object).Object);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(ITempDataDictionaryFactory)))
                .Returns(Mock.Of<ITempDataDictionaryFactory>());
            mockServiceProvider
                .Setup(x => x.GetService(typeof(IObjectModelValidator)))
                .Returns(Mock.Of<IObjectModelValidator>());

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext
                .Setup(x => x.RequestServices)
                .Returns(mockServiceProvider.Object);

            controller.ControllerContext.HttpContext = mockHttpContext.Object;

            return controller;
        }
    }
}
