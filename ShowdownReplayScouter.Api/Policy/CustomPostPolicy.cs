using Microsoft.AspNetCore.OutputCaching;

namespace ShowdownReplayScouter.Api.Policy
{
    public sealed class CustomPostPolicy : IOutputCachePolicy
    {
        public static readonly CustomPostPolicy Instance = new();

        private CustomPostPolicy() { }

        ValueTask IOutputCachePolicy.CacheRequestAsync(
            OutputCacheContext context,
            CancellationToken cancellationToken
        )
        {
            var attemptOutputCaching = AttemptOutputCaching(context);
            context.EnableOutputCaching = true;
            context.AllowCacheLookup = attemptOutputCaching;
            context.AllowCacheStorage = attemptOutputCaching;
            context.AllowLocking = true;

            return ValueTask.CompletedTask;
        }

        private static bool AttemptOutputCaching(OutputCacheContext context)
        {
            // Check if the current request fulfills the requirements to be cached
            var request = context.HttpContext.Request;

            // Verify the method, we only cache get, post and head verb
            return HttpMethods.IsGet(request.Method)
                || HttpMethods.IsPost(request.Method)
                || HttpMethods.IsHead(request.Method);
        }

        public ValueTask ServeFromCacheAsync(
            OutputCacheContext context,
            CancellationToken cancellation
        ) => ValueTask.CompletedTask;

        public ValueTask ServeResponseAsync(
            OutputCacheContext context,
            CancellationToken cancellation
        ) => ValueTask.CompletedTask;
    }
}
