using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Documents;
using OrchardCore.Environment.Shell.Scope;
using OrchardCore.Lucene.Model;

namespace OrchardCore.Lucene
{
    public class LuceneIndexSettingsService
    {
        public LuceneIndexSettingsService()
        {
        }

        /// <summary>
        /// Loads the index settings document from the store for updating and that should not be cached.
        /// </summary>
        public Task<LuceneIndexSettingsDocument> LoadDocumentAsync() => DocumentManager.GetOrCreateMutableAsync();

        /// <summary>
        /// Gets the index settings document from the cache for sharing and that should not be updated.
        /// </summary>
        public async Task<LuceneIndexSettingsDocument> GetDocumentAsync()
        {
            var document = await DocumentManager.GetOrCreateImmutableAsync();

            foreach (var name in document.LuceneIndexSettings.Keys)
            {
                document.LuceneIndexSettings[name].IndexName = name;
            }

            return document;
        }

        public async Task<IEnumerable<LuceneIndexSettings>> GetSettingsAsync()
        {
            return (await GetDocumentAsync()).LuceneIndexSettings.Values;
        }

        public async Task<LuceneIndexSettings> GetSettingsAsync(string indexName)
        {
            var document = await GetDocumentAsync();

            if (document.LuceneIndexSettings.TryGetValue(indexName, out var settings))
            {
                return settings;
            }

            return null;
        }

        public async Task<string> GetIndexAnalyzerAsync(string indexName)
        {
            var document = await GetDocumentAsync();

            if (document.LuceneIndexSettings.TryGetValue(indexName, out var settings))
            {
                return settings.AnalyzerName;
            }

            return LuceneSettings.StandardAnalyzer;
        }

        public async Task<string> LoadIndexAnalyzerAsync(string indexName)
        {
            var document = await LoadDocumentAsync();

            if (document.LuceneIndexSettings.TryGetValue(indexName, out var settings))
            {
                return settings.AnalyzerName;
            }

            return LuceneSettings.StandardAnalyzer;
        }

        public async Task UpdateIndexAsync(LuceneIndexSettings settings)
        {
            var document = await LoadDocumentAsync();
            document.LuceneIndexSettings[settings.IndexName] = settings;
            await DocumentManager.UpdateAsync(document);
        }

        public async Task DeleteIndexAsync(string indexName)
        {
            var document = await LoadDocumentAsync();
            document.LuceneIndexSettings.Remove(indexName);
            await DocumentManager.UpdateAsync(document);
        }

        private static IDocumentManager<LuceneIndexSettingsDocument> DocumentManager =>
            ShellScope.Services.GetRequiredService<IDocumentManager<LuceneIndexSettingsDocument>>();
    }
}
