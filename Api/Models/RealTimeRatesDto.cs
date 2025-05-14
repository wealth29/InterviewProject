using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Models
{
    public class RealTimeRatesDto
    {
        public string Base { get; set; } = default!;
        public DateTime Date { get; set; }
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }
}