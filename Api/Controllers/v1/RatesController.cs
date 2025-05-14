using Api.Data;
using Api.Models.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers.v1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class RatesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public RatesController(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Retrieves historical rates for a currency pair over a date range.
        /// </summary>
        [HttpGet("history")]
        public async Task<ActionResult<Dictionary<DateTime, decimal>>> GetHistory(
            [FromQuery] HistoricalRatesRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (req.From > req.To)
                return BadRequest("'From' date must be on or before 'To' date.");

            var rates = await _db.ExchangeRates
                .Where(r =>
                    r.BaseCurrency == req.Base &&
                    r.TargetCurrency == req.Target &&
                    r.Date != null &&
                    r.Date >= req.From.Date &&
                    r.Date <= req.To.Date)
                .OrderBy(r => r.Date)
                .ToListAsync();

            if (!rates.Any())
                return NotFound($"No historical data for {req.Base}->{req.Target} in the given range.");

            // Build a simple Date->Rate dictionary
            var result = rates.ToDictionary(r => r.Date!.Value, r => r.Rate);
            return Ok(result);
        }
    }
}