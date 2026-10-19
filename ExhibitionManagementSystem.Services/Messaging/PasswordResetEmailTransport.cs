using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace ExhibitionManagementSystem.Services.Messaging;

public interface IPasswordResetEmailTransport
{
    Task SendAsync(string recipient, Uri resetLink, CancellationToken cancellationToken);
}

public sealed class SmtpPasswordResetEmailTransport(IOptions<PasswordResetOptions> options) : IPasswordResetEmailTransport
{
    public async Task SendAsync(string recipient, Uri resetLink, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        using var mail = new MailMessage(settings.SenderAddress, recipient)
        {
            Subject = "إعادة تعيين كلمة المرور",
            Body = "لإعادة تعيين كلمة المرور استخدم الرابط التالي خلال 30 دقيقة:\n" + resetLink.AbsoluteUri,
            IsBodyHtml = false
        };
        using var smtp = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
        {
            EnableSsl = true, UseDefaultCredentials = false
        };
        if (!string.IsNullOrWhiteSpace(settings.SmtpUser))
            smtp.Credentials = new NetworkCredential(settings.SmtpUser, settings.SmtpPassword);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        await smtp.SendMailAsync(mail, deadline.Token);
    }
}
