using Microsoft.AspNetCore.HttpOverrides;
using NSwag;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using ShowdownReplayScouter.Api.Policy;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddOpenApiDocument(settings =>
{
    settings.Title = "Showdown Replay Scouter";
    settings.Description = "This is the Showdown Replay Scouter API. <a href='https://github.com/FullLifeGames/Showdown-Replay-Scouter'>This is the code.</a>";
});
builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 10;
        options.Window = TimeSpan.FromSeconds(5);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    }));
builder.Services.AddResponseCaching();
builder.Services.AddOutputCache(x => x.AddPostPolicy());

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

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
        config.PostProcess = (document, _) => document.Schemes = new[] { OpenApiSchema.Https };
    }
}); // serve OpenAPI/Swagger documents
app.UseSwaggerUi3((config) => config.DocumentTitle = "Showdown Replay Scouter"); // serve Swagger UI
app.UseReDoc(); // serve ReDoc UI
//}

app.UseOutputCache();
app.UseResponseCaching();

app.UseAuthorization();

app.MapControllers();

app.Run();
