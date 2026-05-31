using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ExhibitionManagementSystem.Models;

public class ExchangeRate
{


    [Key] public int RateID { get; set; }
    [Required, StringLength(3)] public string FromCurrency { get; set; }
    [Required, StringLength(3)] public string ToCurrency { get; set; }
    [Column(TypeName = "decimal(18,6)")] public decimal Rate { get; set; }
    [Column(TypeName = "date")] public DateTime RateDate { get; set; }
    [Required, StringLength(50)] public string Source { get; set; }
    [StringLength(450)] public string CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(FromCurrency))] public virtual Currency FromCurrencyNav { get; set; }
    [ForeignKey(nameof(ToCurrency))] public virtual Currency ToCurrencyNav { get; set; }
    [ForeignKey(nameof(CreatedByUserId))] public virtual ApplicationUser CreatedByUser { get; set; }

}
