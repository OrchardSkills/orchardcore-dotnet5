using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrchardCore.Admin;
using OrchardCore.Deployment;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Theming;
using OrchardCore.Modules;
using OrchardCore.Mvc.Core.Utilities;
using OrchardCore.Navigation;
using OrchardCore.Recipes;
using OrchardCore.Security.Permissions;
using OrchardCore.Themes.Controllers;
using OrchardCore.Themes.Deployment;
using OrchardCore.Themes.Recipes;
using OrchardCore.Themes.Services;

namespace OrchardCore.Themes
{
    /// <summary>
    /// These services are registered on the tenant service collection
    /// </summary>
    public class Startup : StartupBase
    {
        private readonly AdminOptions _adminOptions;

        public Startup(IOptions<AdminOptions> adminOptions)
        {
            _adminOptions = adminOptions.Value;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddRecipeExecutionStep<ThemesStep>();
            services.AddScoped<IPermissionProvider, Permissions>();
            services.AddScoped<IThemeSelector, SiteThemeSelector>();
            services.AddScoped<ISiteThemeService, SiteThemeService>();
            services.AddScoped<INavigationProvider, AdminMenu>();
            services.AddScoped<IThemeService, ThemeService>();

            services.AddTransient<IDeploymentSource, ThemesDeploymentSource>();
            services.AddSingleton<IDeploymentStepFactory>(new DeploymentStepFactory<ThemesDeploymentStep>());
            services.AddScoped<IDisplayDriver<DeploymentStep>, ThemesDeploymentStepDriver>();
        }

        public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            var themeControllerName = typeof(AdminController).ControllerName();

            routes.MapAreaControllerRoute(
                name: "Themes.Index",
                areaName: "OrchardCore.Themes",
                pattern: _adminOptions.AdminUrlPrefix + "/Themes",
                defaults: new { controller = themeControllerName, action = nameof(AdminController.Index) }
            );

            routes.MapAreaControllerRoute(
                name: "Themes.SetCurrentTheme",
                areaName: "OrchardCore.Themes",
                pattern: _adminOptions.AdminUrlPrefix + "/Themes/SetCurrentTheme/{id}",
                defaults: new { controller = themeControllerName, action = nameof(AdminController.SetCurrentTheme) }
            );

            routes.MapAreaControllerRoute(
                name: "Themes.ResetSiteTheme",
                areaName: "OrchardCore.Themes",
                pattern: _adminOptions.AdminUrlPrefix + "/Themes/ResetSiteTheme",
                defaults: new { controller = themeControllerName, action = nameof(AdminController.ResetSiteTheme) }
            );

            routes.MapAreaControllerRoute(
                name: "Themes.ResetAdminTheme",
                areaName: "OrchardCore.Themes",
                pattern: _adminOptions.AdminUrlPrefix + "/Themes/ResetAdminTheme",
                defaults: new { controller = themeControllerName, action = nameof(AdminController.ResetAdminTheme) }
            );

            routes.MapAreaControllerRoute(
                name: "Themes.Disable",
                areaName: "OrchardCore.Themes",
                pattern: _adminOptions.AdminUrlPrefix + "/Themes/Disable/{id}",
                defaults: new { controller = themeControllerName, action = nameof(AdminController.Disable) }
            );

            routes.MapAreaControllerRoute(
                name: "Themes.Enable",
                areaName: "OrchardCore.Themes",
                pattern: _adminOptions.AdminUrlPrefix + "/Themes/Enable/{id}",
                defaults: new { controller = themeControllerName, action = nameof(AdminController.Enable) }
            );
        }
    }
}
