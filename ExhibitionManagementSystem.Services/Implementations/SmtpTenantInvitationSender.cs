using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.Services.Options;
using Microsoft.Extensions.Options;

namespace ExhibitionManagementSystem.Services.Implementations;

public sealed class SmtpTenantInvitationSender : ITenantInvitationSender
{
    private readonly SmtpOptions _smtp;
    private readonly TenantInvitationOptions _invitationOptions;

    public SmtpTenantInvitationSender(
        IOptions<SmtpOptions> smtp,
        IOptions<TenantInvitationOptions> invitationOptions)
    {
        _smtp = smtp.Value;
        _invitationOptions = invitationOptions.Value;
    }

    public async Task SendAsync(
        string email,
        string fullName,
        string companyName,
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();

        var baseUrl = _invitationOptions.PublicAppUrl.TrimEnd('/');
        var invitationUrl = $"{baseUrl}/accept-invitation?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(rawToken)}";
        var encoder = HtmlEncoder.Default;

        using var message = new MailMessage
        {
            From = new MailAddress(_smtp.FromAddress, _smtp.FromName),
            Subject = "دعوة إدارة حساب المعرض",
            Body = $"""
                <div dir="rtl" style="font-family:Arial,sans-serif">
                  <p>مرحبًا {encoder.Encode(fullName)}،</p>
                  <p>تمت دعوتك لإدارة حساب {encoder.Encode(companyName)}.</p>
                  <p><a href="{encoder.Encode(invitationUrl)}">قبول الدعوة وتعيين كلمة المرور</a></p>
                  <p>الرابط أحادي الاستخدام وتنتهي صلاحيته تلقائيًا.</p>
                </div>
                """,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(email, fullName));

        using var client = new SmtpClient(_smtp.Host, _smtp.Port)
        {
            EnableSsl = _smtp.EnableSsl,
            Credentials = new NetworkCredential(_smtp.UserName, _smtp.Password)
        };

        await client.SendMailAsync(message, cancellationToken);
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_smtp.Host) ||
            string.IsNullOrWhiteSpace(_smtp.UserName) ||
            string.IsNullOrWhiteSpace(_smtp.Password) ||
            string.IsNullOrWhiteSpace(_smtp.FromAddress) ||
            !Uri.TryCreate(_invitationOptions.PublicAppUrl, UriKind.Absolute, out var publicUri) ||
            publicUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                "SMTP and the public HTTPS invitation URL must be configured before tenant provisioning.");
        }
    }
}
