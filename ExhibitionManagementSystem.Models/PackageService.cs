using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل خدمة مضمنة داخل باقة تسعير مع الكمية والسعر.
/// </summary>
public class PackageService
{
    /// <summary>
    /// المعرف الفريد لربط الخدمة بالباقة.
    /// </summary>
    [Key] public int PackageServiceID { get; set; }

    /// <summary>
    /// معرف باقة التسعير.
    /// </summary>
    public int PackageID { get; set; }

    /// <summary>
    /// معرف الخدمة الموجودة ضمن الباقة.
    /// </summary>
    public int ServiceID { get; set; }

    /// <summary>
    /// كمية الخدمة المضمنة في الباقة.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")] public decimal Quantity { get; set; }

    /// <summary>
    /// سعر الوحدة للخدمة داخل الباقة.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }

    /// <summary>
    /// باقة التسعير المرتبطة.
    /// </summary>
    [ForeignKey(nameof(PackageID))] public virtual PricingPackage Package { get; set; }

    /// <summary>
    /// الخدمة المرتبطة بالباقة.
    /// </summary>
    [ForeignKey(nameof(ServiceID))] public virtual Service Service { get; set; }

}
