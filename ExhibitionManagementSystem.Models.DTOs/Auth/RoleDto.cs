namespace ExhibitionManagementSystem.Models.DTOs.Auth;

public class RoleDto
{
    public string RoleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int TenantID { get; set; }
}
