using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrchardCore.Admin;
using OrchardCore.Deployment;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Features.Controllers;
using OrchardCore.Features.Deployment;
using OrchardCore.Features.Recipes.Executors;
using OrchardCore.Features.Services;
using OrchardCore.Modules;
using OrchardCore.Mvc.Core.Utilities;
using OrchardCore.Navigation;
using OrchardCore.Recipes;
using OrchardCore.Security.Permissions;

namespace OrchardCore.Features
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
            services.AddRecipeExecutionStep<FeatureStep>();
            services.AddScoped<IPermissionProvider, Permissions>();
            services.AddScoped<IModuleService, ModuleService>();
            services.AddScoped<INavigationProvider, AdminMenu>();

            services.AddTransient<IDeploymentSource, AllFeaturesDeploymentSource>();
            services.AddSingleton<IDeploymentStepFactory>(new DeploymentStepFactory<AllFeaturesDeploymentStep>());
            services.AddScoped<IDisplayDriver<DeploymentStep>, AllFeaturesDeploymentStepDriver>();
        }

        public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            var adminControllerName = typeof(AdminController).ControllerName();

            routes.MapAreaControllerRoute(
                name: "Features",
                areaName: "OrchardCore.Features",
                pattern: _adminOptions.AdminUrlPrefix + "/Features",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.Features) }
            );
            routes.MapAreaControllerRoute(
                name: "FeaturesDisable",
                areaName: "OrchardCore.Features",
                pattern: _adminOptions.AdminUrlPrefix + "/Features/{id}/Disable",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.Disable) }
            );
            routes.MapAreaControllerRoute(
                name: "FeaturesEnable",
                areaName: "OrchardCore.Features",
                pattern: _adminOptions.AdminUrlPrefix + "/Features/{id}/Enable",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.Enable) }
            );
        }
    }
}
