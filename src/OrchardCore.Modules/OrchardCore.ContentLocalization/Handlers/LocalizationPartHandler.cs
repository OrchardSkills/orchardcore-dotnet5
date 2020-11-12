using System;
using System.Globalization;
using System.Threading.Tasks;
using OrchardCore.ContentLocalization.Models;
using OrchardCore.ContentLocalization.Services;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Handlers;

namespace OrchardCore.ContentLocalization.Handlers
{
    public class LocalizationPartHandler : ContentPartHandler<LocalizationPart>
    {
        private readonly ILocalizationEntries _entries;

        public LocalizationPartHandler(ILocalizationEntries entries)
        {
            _entries = entries;
        }

        public override Task GetContentItemAspectAsync(ContentItemAspectContext context, LocalizationPart part)
        {
            return context.ForAsync<CultureAspect>(cultureAspect =>
            {
                if (part.Culture != null)
                {
                    cultureAspect.Culture = CultureInfo.GetCultureInfo(part.Culture);
                }

                return Task.CompletedTask;
            });
        }

        public override Task PublishedAsync(PublishContentContext context, LocalizationPart part)
        {
            if (!String.IsNullOrWhiteSpace(part.LocalizationSet))
            {
                return _entries.AddEntryAsync(new LocalizationEntry()
                {
                    ContentItemId = part.ContentItem.ContentItemId,
                    LocalizationSet = part.LocalizationSet,
                    Culture = part.Culture.ToLowerInvariant()
                });
            }

            return Task.CompletedTask;
        }

        public override Task UnpublishedAsync(PublishContentContext context, LocalizationPart part)
        {
            return _entries.RemoveEntryAsync(new LocalizationEntry()
            {
                ContentItemId = part.ContentItem.ContentItemId,
                LocalizationSet = part.LocalizationSet,
                Culture = part.Culture.ToLowerInvariant()
            });
        }

        public override Task RemovedAsync(RemoveContentContext context, LocalizationPart part)
        {
            if (context.NoActiveVersionLeft)
            {
                return _entries.RemoveEntryAsync(new LocalizationEntry()
                {
                    ContentItemId = part.ContentItem.ContentItemId,
                    LocalizationSet = part.LocalizationSet,
                    Culture = part.Culture.ToLowerInvariant()
                });
            }

            return Task.CompletedTask;
        }

        public override Task CloningAsync(CloneContentContext context, LocalizationPart part)
        {
            var clonedPart = context.CloneContentItem.As<LocalizationPart>();
            clonedPart.LocalizationSet = string.Empty;
            clonedPart.Apply();
            return Task.CompletedTask;
        }
    }
}
