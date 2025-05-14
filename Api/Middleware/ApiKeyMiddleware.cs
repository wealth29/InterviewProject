using System.Collections.Concurrent;

namespace Api.Middleware
{
    public class ApiKeyMiddleware
    {
        private const string ApiKeyHeaderName = "X-Api-Key";
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiKeyMiddleware> _logger;
        private readonly ConcurrentDictionary<string, int> _remainingQuotas;

        public ApiKeyMiddleware(
            RequestDelegate next,
            IConfiguration configuration,
            ILogger<ApiKeyMiddleware> logger)
        {
            _next           = next;
            _logger         = logger;

            // Load the API keys and their initial quotas from configuration
            var configured = configuration
                .GetSection("RateLimiting:ApiKeys")
                .Get<Dictionary<string, int>>() ?? new Dictionary<string, int>();

            // Use a thread‑safe dictionary for concurrent requests
            _remainingQuotas = new ConcurrentDictionary<string, int>(configured);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip Swagger
            if (context.Request.Path.StartsWithSegments("/swagger"))
            {
                await _next(context);
                return;
            }

            // Extract key
            if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedValues))
            {
                _logger.LogWarning("API key missing. Headers: {Headers}", context.Request.Headers.Keys);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("API key missing");
                return;
            }

            var apiKey = extractedValues.ToString();

            // Validate key exists
            if (!_remainingQuotas.ContainsKey(apiKey))
            {
                _logger.LogWarning("Invalid API key: {ApiKey}", apiKey);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid API key");
                return;
            }

            //  Check and consume quota
            bool consumed = _remainingQuotas.AddOrUpdate(
                apiKey,
                addValueFactory: _ => 0,
                updateValueFactory: (_, current) => current > 0 ? current - 1 : current
            ) > 0;

            if (!consumed)
            {
                _logger.LogWarning("Quota exceeded for key: {ApiKey}", apiKey);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("Quota exceeded");
                return;
            }

            // Expose remaining quota
            context.Response.Headers["X-RateLimit-Remaining"] = 
                _remainingQuotas[apiKey].ToString();

            
            await _next(context);
        }
    }

    
}
