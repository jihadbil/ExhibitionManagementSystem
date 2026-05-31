namespace ExhibitionManagementSystem.Models.DTOs.Pricing;

public class PackageServiceItemDto
{
    public int PackageServiceID { get; set; }
    public int PackageID { get; set; }
    public int ServiceID { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
