using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ExhibitionManagementSystem.Models;

public class Currency
{

    [Key, StringLength(3)] public string CurrencyCode { get; set; }
    [Required, StringLength(50)] public string CurrencyName { get; set; }
    [Required, StringLength(5)] public string Symbol { get; set; }
    public bool IsActive { get; set; } = true;
}
