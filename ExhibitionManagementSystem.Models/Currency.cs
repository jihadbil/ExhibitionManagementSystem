using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل عملة مالية مستخدمة في التسعير والفوترة والتحويلات.
/// </summary>
public class Currency
{

    /// <summary>
    /// رمز العملة القياسي المكون من ثلاثة أحرف.
    /// </summary>
    [Key, StringLength(3)] public string CurrencyCode { get; set; }

    /// <summary>
    /// الاسم الكامل للعملة.
    /// </summary>
    [Required, StringLength(50)] public string CurrencyName { get; set; }

    /// <summary>
    /// رمز عرض العملة مثل $ أو د.ل.
    /// </summary>
    [Required, StringLength(5)] public string Symbol { get; set; }

    /// <summary>
    /// يحدد ما إذا كانت العملة متاحة للاستخدام.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
