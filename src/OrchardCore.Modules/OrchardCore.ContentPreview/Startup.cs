using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentPreview.Drivers;
using OrchardCore.ContentPreview.Handlers;
using OrchardCore.ContentPreview.Models;
using OrchardCore.ContentPreview.Settings;
using OrchardCore.ContentTypes.Editors;
using OrchardCore.Data.Migration;
using OrchardCore.ResourceManagement;

namespace OrchardCore.ContentPreview
{
    public class Startup : Modules.StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IResourceManifestProvider, ResourceManifest>();

            services.AddScoped<IContentDisplayDriver, ContentPreviewDriver>();

            // Preview Part
            services.AddContentPart<PreviewPart>()
                .AddHandler<PreviewPartHandler>();

            services.AddScoped<IDataMigration, Migrations>();
            services.AddScoped<IContentTypePartDefinitionDisplayDriver, PreviewPartSettingsDisplayDriver>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, PreviewStartupFilter>());
        }
    }
}
