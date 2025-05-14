using AspNetCoreRateLimit;
using Microsoft.EntityFrameworkCore;
using Api.Services;
using Api.Services.Interface;
using Polly;
using Api.Middleware;
using Api.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuration
var config = builder.Configuration;

// 2. Bind custom RateLimitingOptions (for API key middleware)
builder.Services.Configure<RateLimitingOptions>(
    config.GetSection("RateLimiting"));

// 3. Register ApiKeyService
builder.Services.AddSingleton<IApiKeyService, ApiKeyService>();

// 4. EF Core: SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(opts =>
    opts.UseSqlServer(config.GetConnectionString("DefaultConnection")));

// 5. Named HttpClient with Polly retry & circuit‑breaker
builder.Services
    .AddHttpClient<IExchangeClient, ExchangeClient>(client =>
    {
        client.BaseAddress = new Uri(config["ExternalService:BaseUrl"]!);
        client.DefaultRequestHeaders.Add("X-API-KEY", config["ExternalService:ApiKey"]!);
    });
    // .AddPolicyHandler(GetRetryPolicy())
    // .AddPolicyHandler(GetCircuitBreakerPolicy());

// 6. In‑memory IP rate limiting
builder.Services.AddOptions();
builder.Services.Configure<IpRateLimitOptions>(options =>
    config.GetSection("RateLimiting").Bind(options));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// 7. MVC + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 8. Middleware pipeline
// API key validation must run early
app.UseMiddleware<ApiKeyMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseIpRateLimiting();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// --- Polly policies ---
// static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
// {
//     // Retry on 5XX or network errors, 3 times with exponential backoff
//     return HttpPolicyExtensions
//         .HandleTransientHttpError()
//         .WaitAndRetryAsync(
//             retryCount: 3,
//             sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))
//         );
// }

// static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
// {
//     // Break circuit after 2 consecutive failures for 30 seconds
//     return HttpPolicyExtensions
//         .HandleTransientHttpError()
//         .CircuitBreakerAsync(
//             handledEventsAllowedBeforeBreaking: 2,
//             durationOfBreak: TimeSpan.FromSeconds(30)
//         );
// }