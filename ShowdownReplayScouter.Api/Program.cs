using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using NSwag;
using ShowdownReplayScouter.Api.Policy;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddOpenApiDocument(settings =>
{
    settings.Title = "Showdown Replay Scouter";
    settings.Description =
        "This is the Showdown Replay Scouter API. <a href='https://github.com/FullLifeGames/Showdown-Replay-Scouter'>This is the code.</a>";
});
builder.Services.Configure<KestrelServerOptions>(x => x.AllowSynchronousIO = true);
builder.Services.AddRateLimiter(config =>
{
    config.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // One window per client IP (taken from X-Forwarded-For by UseForwardedHeaders),
    // so a single client cannot use up the requests of everybody else
    config.AddPolicy(
        "fixed",
        context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromSeconds(5),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 2,
                }
            )
    );
});
builder.Services.AddResponseCaching();
builder.Services.AddOutputCache(x => x.AddPostPolicy());
var insightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
if (insightsConnectionString is not null)
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = insightsConnectionString;
    });
}

var app = builder.Build();

app.UseForwardedHeaders(
    new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    }
);

app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseOpenApi(config =>
{
    if (!app.Environment.IsDevelopment())
    {
        config.PostProcess = (document, _) => document.Schemes = [OpenApiSchema.Https];
    }
}); // serve OpenAPI/Swagger documents
app.UseSwaggerUi((config) => config.DocumentTitle = "Showdown Replay Scouter"); // serve Swagger UI
app.UseReDoc(); // serve ReDoc UI

//}

// Before the output cache, so cached answers count as well
app.UseRateLimiter();

app.UseOutputCache();
app.UseResponseCaching();

app.UseAuthorization();

app.MapControllers();

app.Run();
