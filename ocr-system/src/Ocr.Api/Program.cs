using System.Globalization;
using System.IO.Abstractions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Ocr.Api.Clients;
using Ocr.Api.Options;
using Ocr.Api.Repositories;
using Ocr.Api.Services;
using Ocr.Api.Processing;

var builder = WebApplication.CreateBuilder(args);

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection("Workers"));
builder.Services.Configure<TempStorageOptions>(builder.Configuration.GetSection("TempStorage"));
builder.Services.Configure<AzureOptions>(builder.Configuration.GetSection("Azure"));
builder.Services.Configure<DbOptions>(builder.Configuration.GetSection("Db"));
builder.Services.Configure<OcrOptions>(builder.Configuration.GetSection("Ocr"));
builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection("RateLimit"));

builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddSingleton<TempFileService>();
builder.Services.AddSingleton<WorkerPoolService>();
builder.Services.AddHttpClient<AzureReadClient>();
builder.Services.AddSingleton<PaddleOcrClient>();
builder.Services.AddSingleton<TextPostProcessor>();
builder.Services.AddSingleton<AzureUsageLimiter>();
builder.Services.AddSingleton<AuditRepository>();

builder.Services.AddControllers(options =>
{
    options.Filters.Add(new ProducesResponseTypeAttribute(typeof(ProblemDetails), StatusCodes.Status400BadRequest));
    options.Filters.Add(new ProducesResponseTypeAttribute(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100 MB default limit
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    if (!context.Items.ContainsKey("CorrelationId"))
    {
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers["X-Correlation-Id"] = correlationId;
    }

    using (app.Logger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = context.Items["CorrelationId"] }))
    {
        await next();
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<RateLimitMiddleware>();
app.MapControllers();

await EnsureTempStorageAsync(app.Services);

app.Run();

static async Task EnsureTempStorageAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var options = scope.ServiceProvider.GetRequiredService<IOptions<TempStorageOptions>>().Value;
    var fileSystem = scope.ServiceProvider.GetRequiredService<IFileSystem>();

    if (!fileSystem.Directory.Exists(options.Root))
    {
        fileSystem.Directory.CreateDirectory(options.Root);
    }

    await Task.CompletedTask;
}
