namespace Application.DTOs.AI;

public class TopicClassificationResult
{
    public string TopicName { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}
