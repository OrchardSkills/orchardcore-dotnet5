using System.Threading.Tasks;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using OrchardCore.Facebook.Widgets.Models;
using OrchardCore.Recipes.Services;

namespace OrchardCore.Facebook.Widgets
{
    public class WidgetMigrations : DataMigration
    {
        private readonly IRecipeMigrator _recipeMigrator;
        private readonly IContentDefinitionManager _contentDefinitionManager;

        public WidgetMigrations(IRecipeMigrator recipeMigrator, IContentDefinitionManager contentDefinitionManager)
        {
            _recipeMigrator = recipeMigrator;
            _contentDefinitionManager = contentDefinitionManager;
        }

        public async Task<int> CreateAsync()
        {
            _contentDefinitionManager.AlterPartDefinition(nameof(FacebookPluginPart), builder => builder
                .Attachable()
                .WithDescription("Provides a facebook plugin part to create facebook social plugin widgets."));

            await _recipeMigrator.ExecuteAsync("Widgets/migration.recipe.json", this);
            return await Task.FromResult(1);
        }
    }
}
