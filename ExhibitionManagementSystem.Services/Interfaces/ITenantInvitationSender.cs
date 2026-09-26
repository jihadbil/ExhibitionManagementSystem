namespace ExhibitionManagementSystem.Services.Interfaces;

public interface ITenantInvitationSender
{
    Task SendAsync(
        string email,
        string fullName,
        string companyName,
        string rawToken,
        CancellationToken cancellationToken = default);
}
