using System;
using System.Collections.Generic;
using ExhibitionManagementSystem.Models.DTOs.Auth;

namespace ExhibitionManagementSystem.Desktop.Services.Auth
{
    public class SessionService : ISessionService
    {
        public int TenantId { get; private set; }
        public string UserId { get; private set; } = string.Empty;
        public string Token { get; private set; } = string.Empty;
        public bool IsAuthenticated { get; private set; }
        public IList<string> Roles { get; private set; } = new List<string>();
        public string UserDisplayName { get; private set; } = string.Empty;

        public void SetSession(LoginResponseDto response)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));

            TenantId = response.TenantID;
            UserId = response.UserId;
            Token = response.AccessToken;
            IsAuthenticated = true;
            Roles = response.Roles ?? new List<string>();
            UserDisplayName = response.FullName;
        }

        public void ClearSession()
        {
            TenantId = 0;
            UserId = string.Empty;
            Token = string.Empty;
            IsAuthenticated = false;
            Roles = new List<string>();
            UserDisplayName = string.Empty;
        }
    }
}
