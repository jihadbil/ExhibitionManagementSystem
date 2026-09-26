namespace ExhibitionManagementSystem.Services.Options;

public sealed class TenantInvitationOptions
{
    public const string SectionName = "TenantInvitations";

    public int ExpiryHours { get; set; } = 24;
    public string PublicAppUrl { get; set; } = string.Empty;
}

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Exhibition Management System";
}
