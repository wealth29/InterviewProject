using Api.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Api.Services;
using Polly;
using Polly.Extensions.Http;
using Api.Middleware;
using Api.Data;
using Api.Background;
using Microsoft.OpenApi.Models;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

//  Configuration
var config = builder.Configuration;

// RateLimitingOptions for custom API key service
builder.Services.Configure<RateLimitingOptions>(
    config.GetSection("RateLimiting"));

// Register API Key service
builder.Services.AddSingleton<IApiKeyService, ApiKeyService>();

// EF Core
builder.Services.AddDbContext<ApplicationDbContext>(opts =>
    opts.UseSqlServer(config.GetConnectionString("DefaultConnection")));

// Program.cs
builder.Services.AddHttpClient<IExchangeClient, ExchangeClient>(client =>
    client.BaseAddress = new Uri("https://api.example.com"))
  .AddPolicyHandler(GetRetryPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    // Retry 3 times with exponential backoff
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}

//  In-memory IP rate limiting
builder.Services.AddMemoryCache();
builder.Services.AddOptions();
builder.Services.Configure<IpRateLimitOptions>(
    config.GetSection("RateLimiting"));
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

// Controllers and Swagger with API Key security
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MyCurrencyConverter", Version = "v1" });

    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key needed. Example: `abc123`",
        Name = "X-Api-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "ApiKey",
                    Type = ReferenceType.SecurityScheme
                },
                In = ParameterLocation.Header,
                Name = "X-Api-Key"
            },
            new List<string>()
        }
    });
});

//  Background Services
builder.Services.AddHostedService<RealTimeFetchService>();
builder.Services.AddHostedService<HistoricalFetchService>();

var app = builder.Build();

app.UseMiddleware<ApiKeyMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MyCurrencyConverter v1");
    });
}

app.UseMiddleware<ApiKeyMiddleware>();
app.UseIpRateLimiting();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
