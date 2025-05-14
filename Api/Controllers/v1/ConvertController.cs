using Api.Data;
using Api.Models.Dto;
using Api.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers.v1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ConvertController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IExchangeClient _client;

        public ConvertController(ApplicationDbContext db, IExchangeClient client)
        {
            _db = db;
            _client = client;
        }

        /// <summary>
        /// Real‑time conversion using the latest stored rate (or fallback to external service).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<decimal>> Get([FromQuery] ConversionRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Try DB first
            var rateEntity = await _db.ExchangeRates
                .Where(r => r.BaseCurrency == req.Base && r.TargetCurrency == req.Target && r.Date == null)
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefaultAsync();

            decimal rate;
            if (rateEntity != null)
            {
                rate = rateEntity.Rate;
            }
            else
            {
                // fallback to external call
                var dto = await _client.GetRealTimeAsync(req.Base);
                if (!dto.Rates.TryGetValue(req.Target, out rate))
                    return NotFound($"Rate {req.Base}->{req.Target} not available from external service.");
            }

            return Ok(req.Amount * rate);
        }

        /// <summary>
        /// Historical conversion on a specific date.
        /// </summary>
        [HttpGet("historical")]
        public async Task<ActionResult<decimal>> GetHistorical([FromQuery] HistoricalConversionRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var rateEntity = await _db.ExchangeRates
                .FirstOrDefaultAsync(r =>
                    r.BaseCurrency == req.Base &&
                    r.TargetCurrency == req.Target &&
                    r.Date == req.Date.Date);

            if (rateEntity == null)
                return NotFound($"Historical rate for {req.Base}->{req.Target} on {req.Date:yyyy-MM-dd} not found.");

            return Ok(req.Amount * rateEntity.Rate);
        }
    }
}