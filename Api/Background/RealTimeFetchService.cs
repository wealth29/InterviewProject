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
    public class RealTimeFetchService : BackgroundService
    {
        private readonly ILogger<RealTimeFetchService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IExchangeClient _client;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

        public RealTimeFetchService(
            ILogger<RealTimeFetchService> logger,
            IServiceScopeFactory scopeFactory,
            IExchangeClient client)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _client = client;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RealTimeFetchService starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var dto = await _client.GetRealTimeAsync("USD");

                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    using var tx = await db.Database.BeginTransactionAsync(stoppingToken);

                    foreach (var (target, rate) in dto.Rates)
                    {
                        var ts = dto.Date; // or DateTime.UtcNow
                        var existing = await db.ExchangeRates
                            .FirstOrDefaultAsync(r =>
                                r.BaseCurrency == dto.Base &&
                                r.TargetCurrency == target &&
                                r.Timestamp == ts,
                            cancellationToken: stoppingToken);

                        if (existing != null)
                        {
                            existing.Rate = rate;
                        }
                        else
                        {
                            db.ExchangeRates.Add(new ExchangeRate {
                                BaseCurrency = dto.Base,
                                TargetCurrency = target,
                                Rate = rate,
                                Timestamp = ts,
                                Date = null
                            });
                        }
                    }

                    await db.SaveChangesAsync(stoppingToken);
                    await tx.CommitAsync(stoppingToken);

                    _logger.LogInformation("Real‑time rates upserted at {Time}", DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in RealTimeFetchService iteration");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("RealTimeFetchService stopping.");
        }
    }
}