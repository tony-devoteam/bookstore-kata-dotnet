using System.Text.Json.Serialization;
using Bookstore.Api.Application;
using Bookstore.Api.Contracts;
using Bookstore.Api.Endpoints;
using Bookstore.Api.Infrastructure;
using Bookstore.Api.Middleware;
using Bookstore.Api.Serialization;
using Bookstore.Api.Validation;
using FluentValidation;
using MongoDB.Driver;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Formatting.Json;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Host.UseSerilog((context, _, loggerConfiguration) =>
    loggerConfiguration
        .MinimumLevel.Is(context.HostingEnvironment.IsProduction()
            ? LogEventLevel.Information
            : LogEventLevel.Debug)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Bookstore.Api")
        .WriteTo.Console(new JsonFormatter()));

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new OffsetRequiredDateTimeOffsetConverter());
});
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var traceId = RequestTraceId.Get(context.HttpContext);
        context.ProblemDetails.Detail ??= context.ProblemDetails.Title ?? "The request could not be completed.";
        context.ProblemDetails.Instance = $"urn:trace:{traceId}";
        context.ProblemDetails.Extensions["traceId"] = traceId;
    };
});
builder.Services.AddExceptionHandler<BookExceptionHandler>();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<IMongoClient>(_ =>
{
    var connectionString = builder.Configuration["MONGODB_URI"] ?? "mongodb://localhost:27017";
    var settings = MongoClientSettings.FromConnectionString(connectionString);
    settings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
    return new MongoClient(settings);
});
builder.Services.AddSingleton<IBookIdGenerator, MongoObjectIdGenerator>();
builder.Services.AddSingleton<IBookRepository, MongoBookRepository>();
builder.Services.AddSingleton<IBookService, BookService>();
builder.Services.AddSingleton<IValidator<CreateBookRequest>, CreateBookRequestValidator>();
builder.Services.AddSingleton<IValidator<UpdateBookRequest>, UpdateBookRequestValidator>();
builder.Services.AddHostedService<MongoIndexInitializer>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    var traceId = context.TraceIdentifier;
    context.Items[RequestTraceId.ItemKey] = traceId;
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Trace-Id"] = traceId;
        return Task.CompletedTask;
    });

    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("RequestTrace");
    using (LogContext.PushProperty("TraceId", traceId))
    {
        logger.LogInformation("Handling {Method} {Path}", context.Request.Method, context.Request.Path);
        await next(context);
    }
});
app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();
if (!app.Environment.IsProduction())
{
    app.MapScalarApiReference();
}

app.MapBookEndpoints();
app.Run();

public partial class Program;
