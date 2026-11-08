using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ShowdownReplayScouter.Core.Util
{
    public static class ParallelHelper
    {
        /// <summary>
        /// Based on https://medium.com/@alex.puiu/parallel-foreach-async-in-c-36756f8ebe62
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="body"></param>
        /// <param name="maxDegreeOfParallelism"></param>
        /// <param name="scheduler"></param>
        /// <param name="cancellationToken">Stops processing the remaining items when canceled.</param>
        /// <returns></returns>
        public static async Task ParallelForEachAsync<T>(
            this IEnumerable<T> source,
            Func<T, Task> body,
            int maxDegreeOfParallelism = DataflowBlockOptions.Unbounded,
            TaskScheduler? scheduler = null,
            CancellationToken cancellationToken = default
        )
        {
            var options = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                CancellationToken = cancellationToken,
            };
            if (scheduler != null)
                options.TaskScheduler = scheduler;

            var block = new ActionBlock<T>(body, options);

            foreach (var item in source)
                block.Post(item);

            block.Complete();
            await block.Completion;
        }

        /// <summary>
        /// Based on https://medium.com/@alex.puiu/parallel-foreach-async-in-c-36756f8ebe62
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="body"></param>
        /// <param name="maxDegreeOfParallelism"></param>
        /// <param name="scheduler"></param>
        /// <param name="cancellationToken">Stops the enumeration and the remaining items when canceled.</param>
        /// <returns></returns>
        public static async Task AsyncParallelForEachAsync<T>(
            this IAsyncEnumerable<T> source,
            Func<T, Task> body,
            int maxDegreeOfParallelism = DataflowBlockOptions.Unbounded,
            TaskScheduler? scheduler = null,
            CancellationToken cancellationToken = default
        )
        {
            // Also canceled when the source fails, so the queued items of a failed run are dropped
            using var blockCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken
            );
            var options = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                CancellationToken = blockCancellation.Token,
            };
            if (scheduler != null)
                options.TaskScheduler = scheduler;

            var block = new ActionBlock<T>(body, options);

            try
            {
                await foreach (var item in source.WithCancellation(cancellationToken))
                    block.Post(item);
            }
            catch
            {
                blockCancellation.Cancel();
                throw;
            }

            block.Complete();
            await block.Completion;
        }
    }
}
