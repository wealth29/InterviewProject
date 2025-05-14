using AspNetCoreRateLimit;
using Microsoft.EntityFrameworkCore;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuration
var config = builder.Configuration;

// 2. EF Core
builder.Services.AddDbContext<ApplicationDbContext>(opts =>
    opts.UseSqlServer(config.GetConnectionString("DefaultConnection")));

// 3. HttpClient + Polly retry policy
builder.Services.AddHttpClient<IExchangeClient, ExchangeClient>(client =>
    client.BaseAddress = new Uri(config["ExternalService:BaseUrl"]))
  .AddPolicyHandler(GetRetryPolicy());

// 4. Rate limiting (in-memory)
builder.Services.AddOptions();
builder.Services.Configure<IpRateLimitOptions>(options =>
    config.GetSection("RateLimiting").Bind(options));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// 5. Controllers, Swagger, etc.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middlewares
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseIpRateLimiting();      // Throttle based on ApiKeys from config
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Polly policy definition
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    // Retry on 5XX or network errors, 3 times, exponential backoff
    return HttpPolicyExtensions
      .HandleTransientHttpError()
      .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
