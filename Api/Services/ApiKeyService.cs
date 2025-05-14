using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Services.Interface;
using Microsoft.Extensions.Options;

namespace Api.Services
{
    public class ApiKeyService: IApiKeyService
    {
        private readonly ConcurrentDictionary<string, int> _quotas;

        public ApiKeyService(IOptions<RateLimitingOptions> opts)
        {
            // Initialize from config
            _quotas = new ConcurrentDictionary<string, int>(opts.Value.ApiKeys);
        }

        public bool TryConsumeKey(string apiKey)
        {
            if (!_quotas.ContainsKey(apiKey))
                return false;

            return _quotas.AddOrUpdate(
                apiKey,
                addValue: 0,
                updateValueFactory: (_, current) =>
                {
                    if (current <= 0) return 0;
                    return current - 1;
                }) > 0;
        }

        public int? GetRemainingQuota(string apiKey)
        {
            return _quotas.TryGetValue(apiKey, out var remaining)
                ? remaining
                : (int?)null;
        }
    }

    // Options class to bind the JSON section
    public class RateLimitingOptions
    {
        public Dictionary<string, int> ApiKeys { get; set; } = new();
    }
}