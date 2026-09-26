namespace ExhibitionManagementSystem.DataAccess.MultiTenancy;

public interface ITenantContext
{
    int? TenantId { get; }
    bool CanBypassTenantIsolation { get; }
}

internal sealed class EmptyTenantContext : ITenantContext
{
    public int? TenantId => null;
    public bool CanBypassTenantIsolation => false;
}
