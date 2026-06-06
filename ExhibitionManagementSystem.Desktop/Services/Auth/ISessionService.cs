using System.Collections.Generic;
using ExhibitionManagementSystem.Models.DTOs.Auth;

namespace ExhibitionManagementSystem.Desktop.Services.Auth
{
    public interface ISessionService
    {
        int TenantId { get; }
        string UserId { get; }
        string Token { get; }
        bool IsAuthenticated { get; }
        IList<string> Roles { get; }
        string UserDisplayName { get; }

        void SetSession(LoginResponseDto response);
        void ClearSession();
    }
}
