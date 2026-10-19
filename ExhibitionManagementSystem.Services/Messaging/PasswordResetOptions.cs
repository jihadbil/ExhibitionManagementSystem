using System.Net.Mail;

namespace ExhibitionManagementSystem.Services.Messaging;

public sealed class PasswordResetOptions
{
    public bool Enabled { get; set; }
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SenderAddress { get; set; } = string.Empty;
    public string? SmtpUser { get; set; }
    public string? SmtpPassword { get; set; }
    public string ResetUrl { get; set; } = string.Empty;

    public bool IsValid() => !Enabled || (!string.IsNullOrWhiteSpace(SmtpHost)
        && SmtpPort is > 0 and <= 65535 && MailAddress.TryCreate(SenderAddress, out _)
        && Uri.TryCreate(ResetUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
        && string.IsNullOrEmpty(uri.UserInfo)
        && (string.IsNullOrWhiteSpace(SmtpUser) == string.IsNullOrWhiteSpace(SmtpPassword)));
}
