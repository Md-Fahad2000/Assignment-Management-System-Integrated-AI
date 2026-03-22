using System.Net;
using System.Net.Mail;

namespace AssignmentManagement.Api.Services;

public interface IEmailService
{
    Task SendVerificationCodeAsync(string toEmail, string fullName, string code, CancellationToken cancellationToken = default);
    Task SendPasswordResetCodeAsync(string toEmail, string fullName, string code, CancellationToken cancellationToken = default);
    bool IsConfigured { get; }
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_config["Smtp:Host"]) &&
        !string.IsNullOrWhiteSpace(_config["Smtp:FromEmail"]);

    public async Task SendVerificationCodeAsync(string toEmail, string fullName, string code, CancellationToken cancellationToken = default)
    {
        var subject = "Verify your email — Assignment Manager";
        var body = $@"Hello {fullName},

Your verification code is: {code}

Enter this code on the registration page to complete your account. This code expires in 15 minutes.

If you did not register, ignore this email.

— Assignment Manager";
        await SendAsync(toEmail, subject, body, cancellationToken);
    }

    public async Task SendPasswordResetCodeAsync(string toEmail, string fullName, string code, CancellationToken cancellationToken = default)
    {
        var subject = "Reset your password — Assignment Manager";
        var body = $@"Hello {fullName},

Your password reset code is: {code}

Enter this code on the reset password page. This code expires in 15 minutes.

If you did not request a reset, ignore this email.

— Assignment Manager";
        await SendAsync(toEmail, subject, body, cancellationToken);
    }

    private async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("SMTP not configured; email not sent to {Email}. Subject: {Subject}", toEmail, subject);
            return;
        }

        var host = _config["Smtp:Host"]!;
        var port = int.TryParse(_config["Smtp:Port"], out var p) ? p : 587;
        var user = _config["Smtp:User"] ?? "";
        var password = _config["Smtp:Password"] ?? "";
        var fromEmail = _config["Smtp:FromEmail"]!;
        var fromName = _config["Smtp:FromName"] ?? "Assignment Manager";

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = bool.TryParse(_config["Smtp:EnableSsl"], out var ssl) && ssl,
            Credentials = string.IsNullOrWhiteSpace(user)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(user, password)
        };

        var mail = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        mail.To.Add(toEmail);

        await client.SendMailAsync(mail, cancellationToken);
    }
}
