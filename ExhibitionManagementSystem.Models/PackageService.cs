using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ExhibitionManagementSystem.Models;

public class PackageService
{
    [Key] public int PackageServiceID { get; set; }
    public int PackageID { get; set; }
    public int ServiceID { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal Quantity { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }

    [ForeignKey(nameof(PackageID))] public virtual PricingPackage Package { get; set; }
    [ForeignKey(nameof(ServiceID))] public virtual Service Service { get; set; }

}
