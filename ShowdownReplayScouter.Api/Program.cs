using Microsoft.AspNetCore.HttpOverrides;
using NSwag;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddOpenApiDocument();

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
