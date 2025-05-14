using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Models.Dto
{
    public class HistoricalRatesRequest
    {
        [Required, StringLength(3, MinimumLength = 3)]
        public string Base { get; set; } = default!;

        [Required, StringLength(3, MinimumLength = 3)]
        public string Target { get; set; } = default!;

        [Required]
        [DataType(DataType.Date)]
        public DateTime From { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime To { get; set; }
    }
}