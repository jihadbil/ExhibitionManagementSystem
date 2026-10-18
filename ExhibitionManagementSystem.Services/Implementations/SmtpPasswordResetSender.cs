using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.Services.Options;
using Microsoft.Extensions.Options;

namespace ExhibitionManagementSystem.Services.Implementations;

public sealed class SmtpPasswordResetSender(IOptions<SmtpOptions> smtp, IOptions<PasswordResetOptions> reset)
    : IPasswordResetSender
{
    public async Task SendAsync(string email, string fullName, string token, CancellationToken cancellationToken = default)
    {
        var settings = smtp.Value;
        if (string.IsNullOrWhiteSpace(settings.Host) || string.IsNullOrWhiteSpace(settings.UserName) ||
            string.IsNullOrWhiteSpace(settings.Password) || string.IsNullOrWhiteSpace(settings.FromAddress))
            throw new InvalidOperationException("SMTP must be configured for password recovery.");
        var publicUrl = reset.Value.PublicAppUrl;
        if (!Uri.TryCreate(publicUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo))
            throw new InvalidOperationException("PasswordReset:PublicAppUrl must be the public HTTPS web application URL.");
        var link = $"{publicUrl.TrimEnd('/')}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        var encoder = HtmlEncoder.Default;
        using var message = new MailMessage
        {
            From = new MailAddress(settings.FromAddress, settings.FromName),
            Subject = "إعادة تعيين كلمة المرور",
            IsBodyHtml = true,
            Body = $"<div dir=\"rtl\" style=\"font-family:Arial,sans-serif\"><p>مرحبًا {encoder.Encode(fullName)}،</p>" +
                $"<p><a href=\"{encoder.Encode(link)}\">تعيين كلمة مرور جديدة</a></p>" +
                "<p>الرابط مؤقت وأحادي الاستخدام. إذا لم تطلب التغيير، يمكنك تجاهل هذه الرسالة.</p></div>"
        };
        message.To.Add(new MailAddress(email, fullName));
        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.EnableSsl,
            Credentials = new NetworkCredential(settings.UserName, settings.Password)
        };
        await client.SendMailAsync(message, cancellationToken);
    }
}
