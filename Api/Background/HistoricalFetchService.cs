using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Data;
using Api.Models;
using Api.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace Api.Background
{
    public class HistoricalFetchService: BackgroundService
    {
        private readonly ILogger<HistoricalFetchService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IExchangeClient _client;

        // Define your major pairs
        private readonly (string Base, string Target)[] _pairs =
            { ("USD","GBP"), ("USD","EUR"), ("GBP","EUR") };

        public HistoricalFetchService(
            ILogger<HistoricalFetchService> logger,
            IServiceScopeFactory scopeFactory,
            IExchangeClient client)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _client = client;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("HistoricalFetchService starting.");

            var end = DateTime.UtcNow.Date;
            var start = end.AddYears(-1);

            foreach (var (baseC, target) in _pairs)
            {
                try
                {
                    var hist = await _client.GetHistoricalAsync(baseC, target, start, end);

                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    using var tx = await db.Database.BeginTransactionAsync(stoppingToken);

                    foreach (var (date, rate) in hist.Rates)
                    {
                        var existing = await db.ExchangeRates
                            .FirstOrDefaultAsync(r =>
                                r.BaseCurrency == baseC &&
                                r.TargetCurrency == target &&
                                r.Date == date,
                            cancellationToken: stoppingToken);

                        if (existing != null)
                        {
                            existing.Rate = rate;
                        }
                        else
                        {
                            db.ExchangeRates.Add(new ExchangeRate {
                                BaseCurrency = baseC,
                                TargetCurrency = target,
                                Rate = rate,
                                Timestamp = date,  // or keep null
                                Date = date
                            });
                        }
                    }

                    await db.SaveChangesAsync(stoppingToken);
                    await tx.CommitAsync(stoppingToken);

                    _logger.LogInformation(
                      "Historical rates for {Base}->{Target} stored ({Count} days)",
                      baseC, target, hist.Rates.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                      ex, "Error fetching historical {Base}->{Target}", baseC, target);
                }

                if (stoppingToken.IsCancellationRequested) break;
            }

            _logger.LogInformation("HistoricalFetchService completed.");
        }
    }
}