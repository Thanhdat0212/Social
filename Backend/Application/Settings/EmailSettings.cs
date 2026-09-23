namespace Application.Settings;

public class EmailSettings
{
    public const string SectionName = "Email";

    public string Provider { get; set; } = "Console"; // "Console" | "Smtp" | "Brevo"
    public string FromAddress { get; set; } = "noreply@social.local";
    public string FromName { get; set; } = "Social";
}
