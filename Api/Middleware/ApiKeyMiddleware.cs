using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Services.Interface;

namespace Api.Middleware
{
    public class ApiKeyMiddleware
    {
        
        private readonly RequestDelegate _next;
        private const string HEADER = "X-Api-Key";

        public ApiKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext ctx, IApiKeyService keyService)
        {
            if (!ctx.Request.Headers.TryGetValue(HEADER, out var extractedKey))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsync("API Key missing");
                return;
            }

            var apiKey = extractedKey.ToString();
            var remaining = keyService.GetRemainingQuota(apiKey);

            if (remaining == null)
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsync("Invalid API Key");
                return;
            }

            if (remaining == 0 || !keyService.TryConsumeKey(apiKey))
            {
                ctx.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await ctx.Response.WriteAsync("Quota exceeded");
                return;
            }

            // Optional: expose remaining in response headers
            ctx.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();

            await _next(ctx);
        }
    }
}