using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Models;
using Api.Services.Interface;

namespace Api.Services
{
    public class ExchangeClient: IExchangeClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<ExchangeClient> _logger;
        private readonly IWebHostEnvironment _env;

        public ExchangeClient(
            HttpClient http,
            ILogger<ExchangeClient> logger,
            IWebHostEnvironment env)
        {
            _http = http;
            _logger = logger;
            _env = env;
        }

        public async Task<RealTimeRatesDto> GetRealTimeAsync(string baseCurrency)
        {
            // DEV stub
            if (_env.IsDevelopment())
            {
                _logger.LogInformation("Returning stubbed real-time rates for {Base}", baseCurrency);
                return new RealTimeRatesDto
                {
                    Base = baseCurrency,
                    Date = DateTime.UtcNow.Date,
                    Rates = new Dictionary<string, decimal>
                    {
                        ["GBP"] = 0.80m,
                        ["EUR"] = 0.92m,
                        ["JPY"] = 155.00m
                    }
                };
            }

            var url = $"realtime?base={baseCurrency}";
            var resp = await _http.GetAsync(url);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("RealTime API call failed: {Status} {Body}",
                    resp.StatusCode, await resp.Content.ReadAsStringAsync());
                throw new ExternalServiceException($"RealTime API returned {(int)resp.StatusCode}");
            }

            using var stream = await resp.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<RealTimeRatesDto>(stream)!
                ?? throw new ExternalServiceException("Failed to deserialize real-time payload");
        }

        public async Task<HistoricalRatesDto> GetHistoricalAsync(
            string baseCurrency,
            string target,
            DateTime start,
            DateTime end)
        {
            // DEV stub
            if (_env.IsDevelopment())
            {
                _logger.LogInformation("Returning stubbed historical rates {Base}->{Target} from {Start} to {End}",
                    baseCurrency, target, start, end);
                return new HistoricalRatesDto
                {
                    Base = baseCurrency,
                    Target = target,
                    Rates = new Dictionary<DateTime, decimal>
                    {
                        [start] = 0.79m,
                        [start.AddDays(1)] = 0.80m,
                        [start.AddDays(2)] = 0.81m,
                        [start.AddDays(3)] = 0.80m,
                        [start.AddDays(4)] = 0.795m
                    }
                };
            }

            var url = $"historical?base={baseCurrency}&target={target}&start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}";
            var resp = await _http.GetAsync(url);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Historical API call failed: {Status} {Body}",
                    resp.StatusCode, await resp.Content.ReadAsStringAsync());
                throw new ExternalServiceException($"Historical API returned {(int)resp.StatusCode}");
            }

            using var stream = await resp.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<HistoricalRatesDto>(stream)!
                ?? throw new ExternalServiceException("Failed to deserialize historical payload");
        }
    }
}