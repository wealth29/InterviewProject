using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Models
{
    public class HistoricalRatesDto
    {
        public string Base { get; set; } = default!;
        public string Target { get; set; } = default!;
        public Dictionary<DateTime, decimal> Rates { get; set; } = new();
    }
}