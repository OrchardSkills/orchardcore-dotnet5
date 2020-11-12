using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using OrchardCore.DisplayManagement.Entities;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Environment.Shell;
using OrchardCore.Localization.Models;
using OrchardCore.Localization.ViewModels;
using OrchardCore.Settings;

namespace OrchardCore.Localization.Drivers
{    
    /// <summary>
    /// Represents a <see cref="SectionDisplayDriver{TModel,TSection}"/> for the localization settings section in the admin site.
    /// </summary>
    public class LocalizationSettingsDisplayDriver : SectionDisplayDriver<ISite, LocalizationSettings>
    {
        public const string GroupId = "localization";
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuthorizationService _authorizationService;
        private readonly INotifier _notifier;
        private readonly IShellHost _shellHost;
        private readonly ShellSettings _shellSettings;
        private readonly IHtmlLocalizer H;
        private readonly IStringLocalizer S;

        public LocalizationSettingsDisplayDriver(
            INotifier notifier,
            IShellHost shellHost,
            ShellSettings shellSettings,
            IHttpContextAccessor httpContextAccessor,
            IAuthorizationService authorizationService,
            IHtmlLocalizer<LocalizationSettingsDisplayDriver> h,
            IStringLocalizer<LocalizationSettingsDisplayDriver> s
        )
        {
            _notifier = notifier;
            _shellHost = shellHost;
            _shellSettings = shellSettings;
            _httpContextAccessor = httpContextAccessor;
            _authorizationService = authorizationService;
            H = h;
            S = s;
        }

        /// <inheritdocs />
        public override async Task<IDisplayResult> EditAsync(LocalizationSettings section, BuildEditorContext context)
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (!await _authorizationService.AuthorizeAsync(user, Permissions.ManageCultures))
            {
                return null;
            }

            return Initialize<LocalizationSettingsViewModel>("LocalizationSettings_Edit", model =>
                {
                    model.Cultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
                        .Select(cultureInfo =>
                        {
                            return new CultureEntry
                            {
                                Supported = section.SupportedCultures.Contains(cultureInfo.Name, StringComparer.OrdinalIgnoreCase),
                                CultureInfo = cultureInfo,
                                IsDefault = String.Equals(section.DefaultCulture, cultureInfo.Name, StringComparison.OrdinalIgnoreCase)
                            };
                        }).ToArray();

                    if (!model.Cultures.Any(x => x.IsDefault))
                    {
                        model.Cultures[0].IsDefault = true;
                    }
                }).Location("Content:2").OnGroup(GroupId);
        }

        /// <inheritdocs />
        public override async Task<IDisplayResult> UpdateAsync(LocalizationSettings section, BuildEditorContext context)
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (!await _authorizationService.AuthorizeAsync(user, Permissions.ManageCultures))
            {
                return null;
            }

            if (context.GroupId == GroupId)
            {
                var model = new LocalizationSettingsViewModel();

                await context.Updater.TryUpdateModelAsync(model, Prefix);

                var supportedCulture = JsonConvert.DeserializeObject<string[]>(model.SupportedCultures);

                if (!supportedCulture.Any())
                {
                    context.Updater.ModelState.AddModelError("SupportedCultures", S["A culture is required"]);
                }

                if (context.Updater.ModelState.IsValid)
                {
                    // Invariant culture name is empty so a null value is bound.
                    section.DefaultCulture = model.DefaultCulture ?? "";
                    section.SupportedCultures = supportedCulture;

                    if (!section.SupportedCultures.Contains(section.DefaultCulture))
                    {
                        section.DefaultCulture = section.SupportedCultures[0];
                    }

                    // We always release the tenant for the default culture and also supported cultures to take effect
                    await _shellHost.ReleaseShellContextAsync(_shellSettings);

                    _notifier.Warning(H["The site has been restarted for the settings to take effect"]);
                }
            }

            return await EditAsync(section, context);
        }
    }
}
