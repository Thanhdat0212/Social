using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Application.Interfaces;
using Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Email;

public class BrevoEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly EmailSettings _emailSettings;
    private readonly BrevoSettings _brevoSettings;
    private readonly ILogger<BrevoEmailSender> _logger;

    public BrevoEmailSender(
        HttpClient httpClient,
        IOptions<EmailSettings> emailSettings,
        IOptions<BrevoSettings> brevoSettings,
        ILogger<BrevoEmailSender> logger)
    {
        _httpClient = httpClient;
        _emailSettings = emailSettings.Value;
        _brevoSettings = brevoSettings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlContent, CancellationToken cancellationToken = default)
    {
        var apiKey = !string.IsNullOrWhiteSpace(_brevoSettings.ApiKey)
            ? _brevoSettings.ApiKey
            : Environment.GetEnvironmentVariable("Brevo__ApiKey")
              ?? Environment.GetEnvironmentVariable("BREVO_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var err = "Brevo API Key chưa được cấu hình! Vui lòng kiểm tra biến môi trường Brevo__ApiKey hoặc BREVO_API_KEY trên Render / file .env.";
            _logger.LogError(err);
            throw new InvalidOperationException(err);
        }

        var senderEmail = !string.IsNullOrWhiteSpace(_emailSettings.FromAddress)
            ? _emailSettings.FromAddress.Trim()
            : Environment.GetEnvironmentVariable("Email__FromAddress")
              ?? Environment.GetEnvironmentVariable("EMAIL_FROM_ADDRESS")
              ?? "thuthuycan5@gmail.com";

        var senderName = !string.IsNullOrWhiteSpace(_emailSettings.FromName)
            ? _emailSettings.FromName.Trim()
            : Environment.GetEnvironmentVariable("Email__FromName")
              ?? Environment.GetEnvironmentVariable("EMAIL_FROM_NAME")
              ?? "Mạng xã hội Social";

        try
        {
            var payload = new
            {
                sender = new { name = senderName, email = senderEmail },
                to = new[] { new { email = toEmail.Trim() } },
                subject = subject,
                htmlContent = htmlContent
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("api-key", apiKey.Trim());
            request.Content = jsonContent;

            _logger.LogInformation("Đang gửi email qua Brevo API tới {ToEmail} (Sender: {SenderEmail})...", toEmail, senderEmail);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Brevo API phản hồi lỗi: HTTP {StatusCode} - Nội dung: {Body}", response.StatusCode, responseBody);
                response.EnsureSuccessStatusCode();
            }

            _logger.LogInformation("Gửi email thành công qua Brevo tới {ToEmail}. Response: {Body}", toEmail, responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Thất bại khi gửi email tới {ToEmail} qua Brevo API.", toEmail);
            throw;
        }
    }
}
