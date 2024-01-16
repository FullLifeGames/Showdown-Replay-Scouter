using Microsoft.AspNetCore.OutputCaching;

namespace ShowdownReplayScouter.Api.Policy;
public static class CustomPost
{
    public static void AddPostPolicy(this OutputCacheOptions options)
    {
        options.AddBasePolicy(CustomPostPolicy.Instance);
        options.AddPolicy("post", cacheBuilder => cacheBuilder.VaryByValue((context) =>
        {
            context.Request.EnableBuffering();

            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = reader.ReadToEnd();

            // Reset the stream position to enable subsequent reads
            context.Request.Body.Position = 0;

            return new KeyValuePair<string, string>("requestBody", body);
        }), true);
    }
}
