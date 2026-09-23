using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Email;

public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string toEmail, string subject, string htmlContent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("\n========== [CONSOLE EMAIL SENDER] ==========\nTo: {ToEmail}\nSubject: {Subject}\nContent:\n{HtmlContent}\n============================================",
            toEmail, subject, htmlContent);

        return Task.CompletedTask;
    }
}
