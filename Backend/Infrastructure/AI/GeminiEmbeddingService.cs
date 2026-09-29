using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces.Recommendation;
using Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.AI;

public class GeminiEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiEmbeddingService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GeminiEmbeddingService(
        HttpClient httpClient,
        IOptions<GeminiSettings> settings,
        ILogger<GeminiEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<float>();
        }

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogWarning("Gemini API Key chưa được cấu hình. Bỏ qua tạo vector embedding.");
            return Array.Empty<float>();
        }

        try
        {
            // Cắt ngắn văn bản nếu vượt quá độ dài tối đa
            var cleanText = text.Length > 2000 ? text[..2000] : text;

            var requestBody = new
            {
                model = "models/text-embedding-004",
                content = new
                {
                    parts = new[]
                    {
                        new { text = cleanText }
                    }
                }
            };

            var url = $"{_settings.ApiEndpoint.TrimEnd('/')}/text-embedding-004:embedContent?key={_settings.ApiKey}";

            using var response = await _httpClient.PostAsJsonAsync(url, requestBody, JsonOptions, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Gemini Embedding API trả về lỗi {StatusCode}: {Error}", response.StatusCode, errorBody);
                return Array.Empty<float>();
            }

            var result = await response.Content.ReadFromJsonAsync<GeminiEmbeddingResponse>(JsonOptions, cancellationToken);
            if (result?.Embedding?.Values != null && result.Embedding.Values.Count > 0)
            {
                return result.Embedding.Values.ToArray();
            }

            return Array.Empty<float>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra khi gọi Gemini Embedding API.");
            return Array.Empty<float>();
        }
    }

    private class GeminiEmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public GeminiEmbeddingContent? Embedding { get; set; }
    }

    private class GeminiEmbeddingContent
    {
        [JsonPropertyName("values")]
        public List<float> Values { get; set; } = new();
    }
}
