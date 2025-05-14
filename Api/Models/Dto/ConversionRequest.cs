using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Models.Dto
{
    public class ConversionRequest
    {
        
        [Required, StringLength(3, MinimumLength = 3)]
        public string Base { get; set; } = default!;

        [Required, StringLength(3, MinimumLength = 3)]
        public string Target { get; set; } = default!;

        [Required, Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
    
    }
}