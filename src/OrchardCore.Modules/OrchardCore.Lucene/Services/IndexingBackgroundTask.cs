using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.BackgroundTasks;

namespace OrchardCore.Lucene
{
    /// <summary>
    /// This background task will index content items using.
    /// </summary>
    /// <remarks>
    /// This services is only registered from OrchardCore.Lucene.Worker feature.
    /// </remarks>
    [BackgroundTask(Schedule = "* * * * *", Description = "Update lucene indexes.")]
    public class IndexingBackgroundTask : IBackgroundTask
    {
        public Task DoWorkAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var indexingService = serviceProvider.GetService<LuceneIndexingService>();
            return indexingService.ProcessContentItemsAsync();
        }
    }
}
