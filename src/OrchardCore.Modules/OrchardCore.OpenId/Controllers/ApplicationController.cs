using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OpenIddict.Abstractions;
using OrchardCore.Admin;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Environment.Shell.Descriptor.Models;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.OpenId.Abstractions.Descriptors;
using OrchardCore.OpenId.Abstractions.Managers;
using OrchardCore.OpenId.Services;
using OrchardCore.OpenId.Settings;
using OrchardCore.OpenId.ViewModels;
using OrchardCore.Security.Services;
using OrchardCore.Settings;

namespace OrchardCore.OpenId.Controllers
{
    [Admin, Feature(OpenIdConstants.Features.Management)]
    public class ApplicationController : Controller
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly IStringLocalizer S;
        private readonly IHtmlLocalizer H;
        private readonly ISiteService _siteService;
        private readonly IOpenIdApplicationManager _applicationManager;
        private readonly INotifier _notifier;
        private readonly ShellDescriptor _shellDescriptor;
        private readonly dynamic New;

        public ApplicationController(
            IShapeFactory shapeFactory,
            ISiteService siteService,
            IStringLocalizer<ApplicationController> stringLocalizer,
            IAuthorizationService authorizationService,
            IOpenIdApplicationManager applicationManager,
            IHtmlLocalizer<ApplicationController> htmlLocalizer,
            INotifier notifier,
            ShellDescriptor shellDescriptor)
        {
            New = shapeFactory;
            _siteService = siteService;
            S = stringLocalizer;
            H = htmlLocalizer;
            _authorizationService = authorizationService;
            _applicationManager = applicationManager;
            _notifier = notifier;
            _shellDescriptor = shellDescriptor;
        }

        public async Task<ActionResult> Index(PagerParameters pagerParameters)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageApplications))
            {
                return Forbid();
            }

            var siteSettings = await _siteService.GetSiteSettingsAsync();
            var pager = new Pager(pagerParameters, siteSettings.PageSize);
            var count = await _applicationManager.CountAsync();

            var model = new OpenIdApplicationsIndexViewModel
            {
                Pager = (await New.Pager(pager)).TotalItemCount(count)
            };

            await foreach (var application in _applicationManager.ListAsync(pager.PageSize, pager.GetStartIndex()))
            {
                model.Applications.Add(new OpenIdApplicationEntry
                {
                    DisplayName = await _applicationManager.GetDisplayNameAsync(application),
                    Id = await _applicationManager.GetPhysicalIdAsync(application)
                });
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create(string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageApplications))
            {
                return Forbid();
            }

            var model = new CreateOpenIdApplicationViewModel();

            var roleService = HttpContext.RequestServices?.GetService<IRoleService>();
            if (roleService != null)
            {
                foreach (var role in await roleService.GetRoleNamesAsync())
                {
                    model.RoleEntries.Add(new CreateOpenIdApplicationViewModel.RoleEntry
                    {
                        Name = role
                    });
                }
            }
            else
            {
                _notifier.Warning(H["There are no registered services to provide roles."]);
            }

            ViewData[nameof(OpenIdServerSettings)] = await GetServerSettingsAsync();
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateOpenIdApplicationViewModel model, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageApplications))
            {
                return Forbid();
            }

            if (!string.IsNullOrEmpty(model.ClientSecret) &&
                 string.Equals(model.Type, OpenIddictConstants.ClientTypes.Public, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.ClientSecret), S["No client secret can be set for public applications."]);
            }
            else if (string.IsNullOrEmpty(model.ClientSecret) &&
                     string.Equals(model.Type, OpenIddictConstants.ClientTypes.Confidential, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.ClientSecret), S["The client secret is required for confidential applications."]);
            }

            if (!string.IsNullOrEmpty(model.ClientId) && await _applicationManager.FindByClientIdAsync(model.ClientId) != null)
            {
                ModelState.AddModelError(nameof(model.ClientId), S["The client identifier is already taken by another application."]);
            }

            if (!ModelState.IsValid)
            {
                ViewData[nameof(OpenIdServerSettings)] = await GetServerSettingsAsync();
                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }

            var descriptor = new OpenIdApplicationDescriptor
            {
                ClientId = model.ClientId,
                ClientSecret = model.ClientSecret,
                ConsentType = model.ConsentType,
                DisplayName = model.DisplayName,
                Type = model.Type
            };

            if (model.AllowLogoutEndpoint)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Logout);
            }
            if (model.AllowAuthorizationCodeFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
            }
            if (model.AllowClientCredentialsFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials);
            }
            if (model.AllowImplicitFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.Implicit);
            }
            if (model.AllowPasswordFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.Password);
            }
            if (model.AllowRefreshTokenFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);
            }
            if (model.AllowAuthorizationCodeFlow || model.AllowImplicitFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
            }
            if (model.AllowAuthorizationCodeFlow || model.AllowClientCredentialsFlow ||
                model.AllowPasswordFlow || model.AllowRefreshTokenFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
            }

            if (model.AllowAuthorizationCodeFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);
            }
            if (model.AllowImplicitFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.IdToken);

                if (string.Equals(model.Type, OpenIddictConstants.ClientTypes.Public, StringComparison.OrdinalIgnoreCase))
                {
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.IdTokenToken);
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Token);
                }
            }
            if (model.AllowHybridFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.CodeIdToken);

                if (string.Equals(model.Type, OpenIddictConstants.ClientTypes.Public, StringComparison.OrdinalIgnoreCase))
                {
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.CodeIdTokenToken);
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.CodeToken);
                }
            }

            descriptor.PostLogoutRedirectUris.UnionWith(
                from uri in model.PostLogoutRedirectUris?.Split(new[] { " ", "," }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>()
                select new Uri(uri, UriKind.Absolute));

            descriptor.RedirectUris.UnionWith(
                from uri in model.RedirectUris?.Split(new[] { " ", "," }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>()
                select new Uri(uri, UriKind.Absolute));

            descriptor.Roles.UnionWith(model.RoleEntries
                .Where(role => role.Selected)
                .Select(role => role.Name));

            await _applicationManager.CreateAsync(descriptor);

            if (string.IsNullOrEmpty(returnUrl))
            {
                return RedirectToAction("Index");
            }

            return LocalRedirect(returnUrl);
        }

        public async Task<IActionResult> Edit(string id, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageApplications))
            {
                return Forbid();
            }

            var application = await _applicationManager.FindByPhysicalIdAsync(id);
            if (application == null)
            {
                return NotFound();
            }

            ValueTask<bool> HasPermissionAsync(string permission) => _applicationManager.HasPermissionAsync(application, permission);

            var model = new EditOpenIdApplicationViewModel
            {
                AllowAuthorizationCodeFlow = await HasPermissionAsync(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode),
                AllowClientCredentialsFlow = await HasPermissionAsync(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials),
                AllowImplicitFlow = await HasPermissionAsync(OpenIddictConstants.Permissions.GrantTypes.Implicit),
                AllowPasswordFlow = await HasPermissionAsync(OpenIddictConstants.Permissions.GrantTypes.Password),
                AllowRefreshTokenFlow = await HasPermissionAsync(OpenIddictConstants.Permissions.GrantTypes.RefreshToken),
                AllowLogoutEndpoint = await HasPermissionAsync(OpenIddictConstants.Permissions.Endpoints.Logout),
                ClientId = await _applicationManager.GetClientIdAsync(application),
                ConsentType = await _applicationManager.GetConsentTypeAsync(application),
                DisplayName = await _applicationManager.GetDisplayNameAsync(application),
                Id = await _applicationManager.GetPhysicalIdAsync(application),
                PostLogoutRedirectUris = string.Join(" ", await _applicationManager.GetPostLogoutRedirectUrisAsync(application)),
                RedirectUris = string.Join(" ", await _applicationManager.GetRedirectUrisAsync(application)),
                Type = await _applicationManager.GetClientTypeAsync(application)
            };

            var roleService = HttpContext.RequestServices?.GetService<IRoleService>();
            if (roleService != null)
            {
                var roles = await _applicationManager.GetRolesAsync(application);

                foreach (var role in await roleService.GetRoleNamesAsync())
                {
                    model.RoleEntries.Add(new EditOpenIdApplicationViewModel.RoleEntry
                    {
                        Name = role,
                        Selected = roles.Contains(role, StringComparer.OrdinalIgnoreCase)
                    });
                }
            }
            else
            {
                _notifier.Warning(H["There are no registered services to provide roles."]);
            }

            ViewData[nameof(OpenIdServerSettings)] = await GetServerSettingsAsync();
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditOpenIdApplicationViewModel model, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageApplications))
            {
                return Forbid();
            }

            var application = await _applicationManager.FindByPhysicalIdAsync(model.Id);
            if (application == null)
            {
                return NotFound();
            }

            // If the application was a public client and is now a confidential client, ensure a client secret was provided.
            if (string.IsNullOrEmpty(model.ClientSecret) &&
               !string.Equals(model.Type, OpenIddictConstants.ClientTypes.Public, StringComparison.OrdinalIgnoreCase) &&
                await _applicationManager.HasClientTypeAsync(application, OpenIddictConstants.ClientTypes.Public))
            {
                ModelState.AddModelError(nameof(model.ClientSecret), S["Setting a new client secret is required."]);
            }

            if (!string.IsNullOrEmpty(model.ClientSecret) &&
                 string.Equals(model.Type, OpenIddictConstants.ClientTypes.Public, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.ClientSecret), S["No client secret can be set for public applications."]);
            }

            if (ModelState.IsValid)
            {
                var other = await _applicationManager.FindByClientIdAsync(model.ClientId);
                if (other != null && !string.Equals(
                    await _applicationManager.GetIdAsync(other),
                    await _applicationManager.GetIdAsync(application), StringComparison.Ordinal))
                {
                    ModelState.AddModelError(nameof(model.ClientId), S["The client identifier is already taken by another application."]);
                }
            }

            if (!ModelState.IsValid)
            {
                ViewData[nameof(OpenIdServerSettings)] = await GetServerSettingsAsync();
                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }

            var descriptor = new OpenIdApplicationDescriptor();
            await _applicationManager.PopulateAsync(descriptor, application);

            descriptor.ClientId = model.ClientId;
            descriptor.ConsentType = model.ConsentType;
            descriptor.DisplayName = model.DisplayName;
            descriptor.Type = model.Type;

            if (!string.IsNullOrEmpty(model.ClientSecret))
            {
                descriptor.ClientSecret = model.ClientSecret;
            }

            if (string.Equals(descriptor.Type, OpenIddictConstants.ClientTypes.Public, StringComparison.OrdinalIgnoreCase))
            {
                descriptor.ClientSecret = null;
            }

            if (model.AllowLogoutEndpoint)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Logout);
            }
            else
            {
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.Endpoints.Logout);
            }

            if (model.AllowAuthorizationCodeFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
            }
            else
            {
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
            }

            if (model.AllowClientCredentialsFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials);
            }
            else
            {
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials);
            }

            if (model.AllowImplicitFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.Implicit);
            }
            else
            {
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.GrantTypes.Implicit);
            }

            if (model.AllowPasswordFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.Password);
            }
            else
            {
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.GrantTypes.Password);
            }

            if (model.AllowRefreshTokenFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);
            }
            else
            {
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);
            }

            if (model.AllowAuthorizationCodeFlow || model.AllowImplicitFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
            }
            else
            {
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.Endpoints.Authorization);
            }

            if (model.AllowAuthorizationCodeFlow || model.AllowClientCredentialsFlow ||
                model.AllowPasswordFlow || model.AllowRefreshTokenFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
            }
            else
            {
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.Endpoints.Token);
            }

            if (model.AllowAuthorizationCodeFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);
            }
            else
            {
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.ResponseTypes.Code);
            }

            if (model.AllowImplicitFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.IdToken);

                if (string.Equals(model.Type, OpenIddictConstants.ClientTypes.Public, StringComparison.OrdinalIgnoreCase))
                {
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.IdTokenToken);
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Token);
                }
                else
                {
                    descriptor.Permissions.Remove(OpenIddictConstants.Permissions.ResponseTypes.IdTokenToken);
                    descriptor.Permissions.Remove(OpenIddictConstants.Permissions.ResponseTypes.Token);
                }
            }
            else
            {
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.ResponseTypes.IdToken);
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.ResponseTypes.IdTokenToken);
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.ResponseTypes.Token);
            }
            if (model.AllowHybridFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.CodeIdToken);

                if (string.Equals(model.Type, OpenIddictConstants.ClientTypes.Public, StringComparison.OrdinalIgnoreCase))
                {
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.CodeIdTokenToken);
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.CodeToken);
                }
                else
                {
                    descriptor.Permissions.Remove(OpenIddictConstants.Permissions.ResponseTypes.CodeIdTokenToken);
                    descriptor.Permissions.Remove(OpenIddictConstants.Permissions.ResponseTypes.CodeToken);
                }
            }
            else
            {
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.ResponseTypes.CodeIdToken);
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.ResponseTypes.CodeIdTokenToken);
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.ResponseTypes.CodeToken);
            }

            descriptor.Roles.Clear();

            foreach (string selectedRole in (model.RoleEntries
                .Where(role => role.Selected)
                .Select(role => role.Name)))
            {
                descriptor.Roles.Add(selectedRole);
            }

            descriptor.PostLogoutRedirectUris.Clear();
            foreach (Uri uri in
                (from uri in model.PostLogoutRedirectUris?.Split(new[] { " ", "," }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>()
                 select new Uri(uri, UriKind.Absolute)))
            {
                descriptor.PostLogoutRedirectUris.Add(uri);
            }

            descriptor.RedirectUris.Clear();
            foreach (Uri uri in
               (from uri in model.RedirectUris?.Split(new[] { " ", "," }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>()
                select new Uri(uri, UriKind.Absolute)))
            {
                descriptor.RedirectUris.Add(uri);
            }

            await _applicationManager.UpdateAsync(application, descriptor);

            if (string.IsNullOrEmpty(returnUrl))
            {
                return RedirectToAction("Index");
            }

            return LocalRedirect(returnUrl);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageApplications))
            {
                return Forbid();
            }

            var application = await _applicationManager.FindByPhysicalIdAsync(id);
            if (application == null)
            {
                return NotFound();
            }

            await _applicationManager.DeleteAsync(application);

            return RedirectToAction(nameof(Index));
        }

        private async Task<OpenIdServerSettings> GetServerSettingsAsync()
        {
            if (_shellDescriptor.Features.Any(feature => feature.Id == OpenIdConstants.Features.Server))
            {
                var service = HttpContext.RequestServices.GetRequiredService<IOpenIdServerService>();
                var settings = await service.GetSettingsAsync();
                if ((await service.ValidateSettingsAsync(settings)).Any(result => result != ValidationResult.Success))
                {
                    _notifier.Warning(H["OpenID Connect settings are not properly configured."]);
                }

                return settings;
            }

            return null;
        }
    }
}
