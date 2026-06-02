using ContactSystem.Helpers;
using ContactSystem.Interfaces;
using ContactSystem.Middleware;
using ContactSystem.Repositories;
using ContactSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Reflection;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuration ----
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

// ---- Logging ----
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

// ---- MVC + API behaviour ----
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize all request and response JSON using snake_case.
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.DictionaryKeyPolicy     = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Return consistent ApiResponse shape on automatic 400 validation errors.
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value!.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors.Select(err => err.ErrorMessage))
                .ToList();

            var response = new ContactSystem.DTOs.ApiResponse<object>
            {
                Success = false,
                Message = "Validation failed.",
                Errors  = errors,
                StatusCode = StatusCodes.Status400BadRequest
            };
            return new BadRequestObjectResult(response);
        };
    });

// ---- Dependency Injection ----
builder.Services.AddSingleton<IDatabaseHelper, DatabaseHelper>();
builder.Services.AddScoped<ISubscriberRepository, SubscriberRepository>();
builder.Services.AddScoped<ISubscriberService, SubscriberService>();

// ---- Swagger / OpenAPI ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerExamples();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version     = "v1",
        Title       = "Subscriber Management API",
        Description = "A production-ready ASP.NET Core 10 Web API for managing subscriber records, " +
                      "backed by ADO.NET and SQL Server stored procedures."
    });

    c.EnableAnnotations();
    c.ExampleFilters();
    c.IncludeXmlComments(
        Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"),
        includeControllerXmlComments: true);
});

var app = builder.Build();

// ---- Pipeline ----
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Subscriber Management API V1");
    c.RoutePrefix = string.Empty; // Swagger UI at root
    c.DocumentTitle = "Subscriber Management API";
    c.DisplayRequestDuration();
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
