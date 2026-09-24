namespace Application.Settings;

public class AppSettings
{
    public const string SectionName = "App";

    private string _frontendBaseUrl = "http://localhost:5173";

    public string FrontendBaseUrl
    {
        get => _frontendBaseUrl;
        set => _frontendBaseUrl = value?.Trim() ?? string.Empty;
    }
}
