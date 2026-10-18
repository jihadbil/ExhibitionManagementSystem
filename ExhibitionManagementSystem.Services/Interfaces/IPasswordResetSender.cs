namespace ExhibitionManagementSystem.Services.Interfaces;

public interface IPasswordResetSender
{
    Task SendAsync(string email, string fullName, string token, CancellationToken cancellationToken = default);
}
