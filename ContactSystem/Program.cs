using ContactSystem.Helpers;
using ContactSystem.Interfaces;
using ContactSystem.Middleware;
using ContactSystem.Repositories;
using ContactSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;
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
        // Always emit DateTime / DateTime? as UTC ISO 8601 with an explicit
        // offset so clients can render the value in any timezone. Without
        // this, SqlDataReader returns Unspecified DateTime values and the
        // default System.Text.Json writer omits the offset, which makes
        // browsers parse the value as local time and silently shift it.
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableUtcDateTimeConverter());
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
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<IGroupService, GroupService>();

// ---- Swagger / OpenAPI ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerExamples();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version     = "v1",
        Title       = "Contact Management API",
        Description = "A production-ready ASP.NET Core 10 Web API for managing contact records, " +
                      "backed by ADO.NET and SQL Server stored procedures."
    });

    c.EnableAnnotations();
    c.ExampleFilters();
    c.IncludeXmlComments(
        Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"),
        includeControllerXmlComments: true);

    // Pin the order in which Swagger UI lists each tag (controller group).
    // Any tag not listed here is appended afterwards in its original order.
    // NOTE: The string[] is wrapped in an explicit object[] so that
    // Swashbuckle's `DocumentFilter<T>(params object[])` extension does not
    // unpack the string[] into individual positional ctor arguments; the
    // single object[] element (the string[]) then matches the (string[]) ctor.
    c.DocumentFilter<TagOrderingDocumentFilter>(new object[] { new[] { "Projects", "Groups", "Contacts" } });
});

var app = builder.Build();

// ---- Pipeline ----
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Contact Management API V1");
    c.RoutePrefix = string.Empty; // Swagger UI at root
    c.DocumentTitle = "Contact Management API";
    c.DisplayRequestDuration();
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
