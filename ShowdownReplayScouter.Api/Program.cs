using Microsoft.AspNetCore.HttpOverrides;
using NeoSmart.Caching.Sqlite;
using NSwag;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddOpenApiDocument();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSqliteCache(options =>
    {
        options.CachePath = builder.Environment.IsDevelopment()
                                ? "ShowdownReplayScouter.db"
                                : "/home/apache/ShowdownReplayScouter.Cmd/ShowdownReplayScouter.db";
    });
}
else
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = "localhost";
        options.InstanceName = "ShowdownReplayScouter";
    });
}

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

app.UseAuthorization();

app.MapControllers();

app.Run();
