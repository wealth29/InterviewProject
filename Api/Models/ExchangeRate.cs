using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Api.Models
{
    [Index(nameof(BaseCurrency), nameof(TargetCurrency), nameof(Timestamp), IsUnique = true)]
    [Index(nameof(BaseCurrency), nameof(TargetCurrency), nameof(Date), IsUnique = true)]
    public class ExchangeRate
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(3)]
        public string BaseCurrency { get; set; } = default!;

        [Required, MaxLength(3)]
        public string TargetCurrency { get; set; } = default!;

        [Column(TypeName = "decimal(18,6)")]
        public decimal Rate { get; set; }

        public DateTime Timestamp { get; set; }
        public DateTime? Date { get; set; }
    }
}