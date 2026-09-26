namespace ExhibitionManagementSystem.Models.Interfaces;

/// <summary>
/// Marks a domain entity whose records must be isolated by tenant.
/// </summary>
public interface ITenantEntity
{
    int TenantID { get; set; }
}
