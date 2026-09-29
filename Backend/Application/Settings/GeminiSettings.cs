namespace Application.Settings;

public class GeminiSettings
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-3.5-flash-lite";
    public string ApiEndpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models";
    public double MinConfidenceThreshold { get; set; } = 0.5;
    public int MaxContentLength { get; set; } = 2000;
    public int MaxOutputTokens { get; set; } = 1024;
    public double Temperature { get; set; } = 0.1;
    public int MaxCandidateTopics { get; set; } = 50;
    public int MaxReasonLength { get; set; } = 150;
    public int MaxTopicsPerPost { get; set; } = 3;
}
