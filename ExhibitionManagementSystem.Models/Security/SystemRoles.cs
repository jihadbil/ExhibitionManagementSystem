namespace ExhibitionManagementSystem.Models.Security;

public static class SystemRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string TenantAdmin = "TenantAdmin";
    public const string EventManager = "EventManager";
    public const string Finance = "Finance";
    public const string GateOperator = "GateOperator";

    public static readonly IReadOnlySet<string> TenantAssignable =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Admin,
            TenantAdmin,
            EventManager,
            Finance,
            GateOperator
        };

    public static readonly IReadOnlyList<string> Required =
    [
        SuperAdmin,
        TenantAdmin,
        EventManager,
        Finance,
        GateOperator
    ];
}
