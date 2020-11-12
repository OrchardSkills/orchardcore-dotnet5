using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.Lucene.Drivers;
using OrchardCore.Navigation;

namespace OrchardCore.Lucene
{
    public class AdminMenu : INavigationProvider
    {
        private readonly IStringLocalizer S;

        public AdminMenu(IStringLocalizer<AdminMenu> localizer)
        {
            S = localizer;
        }

        public Task BuildNavigationAsync(string name, NavigationBuilder builder)
        {
            if (!String.Equals(name, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            builder
                .Add(S["Search"], NavigationConstants.AdminMenuSearchPosition, search => search
                    .AddClass("search").Id("search")
                    .Add(S["Indexing"], S["Indexing"].PrefixPosition(), import => import
                        .Add(S["Lucene Indices"], S["Lucene Indices"].PrefixPosition(), indexes => indexes
                            .Action("Index", "Admin", new { area = "OrchardCore.Lucene" })
                            .Permission(Permissions.ManageIndexes)
                            .LocalNav())
                        .Add(S["Run Lucene Query"], S["Run Lucene Query"].PrefixPosition(), queries => queries
                            .Action("Query", "Admin", new { area = "OrchardCore.Lucene" })
                            .Permission(Permissions.ManageIndexes)
                            .LocalNav()))
                    .Add(S["Settings"], settings => settings
                        .Add(S["Search"], S["Search"].PrefixPosition(), entry => entry
                             .Action("Index", "Admin", new { area = "OrchardCore.Settings", groupId = LuceneSettingsDisplayDriver.GroupId })
                             .Permission(Permissions.ManageIndexes)
                             .LocalNav()
                        )));

            return Task.CompletedTask;
        }
    }
}
