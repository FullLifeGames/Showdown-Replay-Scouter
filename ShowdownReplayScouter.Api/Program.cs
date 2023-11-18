using Microsoft.AspNetCore.HttpOverrides;
using NSwag;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddOpenApiDocument();
builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 10;
        options.Window = TimeSpan.FromSeconds(5);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    }));
builder.Services.AddResponseCaching();
builder.Services.AddOutputCache();

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
app.UseSwaggerUi3(); // serve Swagger UI
app.UseReDoc(); // serve ReDoc UI
//}

app.UseResponseCaching();

app.UseAuthorization();

app.MapControllers();

app.Run();
