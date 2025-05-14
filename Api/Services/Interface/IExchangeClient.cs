using Api.Models;

namespace Api.Services.Interface
{
    public interface IExchangeClient
    {
        Task<RealTimeRatesDto> GetRealTimeAsync(string baseCurrency);
        Task<HistoricalRatesDto> GetHistoricalAsync(
            string baseCurrency,
            string target,
            DateTime start,
            DateTime end);
    }
}