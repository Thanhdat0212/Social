using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Application.Interfaces;
using Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Email;

public class BrevoEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly EmailOptions _emailOptions;
    private readonly BrevoOptions _brevoOptions;
    private readonly ILogger<BrevoEmailSender> _logger;

    public BrevoEmailSender(
        HttpClient httpClient,
        IOptions<EmailOptions> emailOptions,
        IOptions<BrevoOptions> brevoOptions,
        ILogger<BrevoEmailSender> logger)
    {
        _httpClient = httpClient;
        _emailOptions = emailOptions.Value;
        _brevoOptions = brevoOptions.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlContent, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                sender = new { name = _emailOptions.FromName, email = _emailOptions.FromAddress },
                to = new[] { new { email = toEmail } },
                subject = subject,
                htmlContent = htmlContent
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            request.Headers.Add("api-key", _brevoOptions.ApiKey);
            request.Content = jsonContent;

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Brevo API error: {StatusCode} - {Body}", response.StatusCode, responseBody);
                response.EnsureSuccessStatusCode();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail} via Brevo API.", toEmail);
            throw;
        }
    }
}
