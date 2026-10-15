namespace Domain.Entities;

public class PostEmbedding
{
    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;

    public float[] Values { get; set; } = Array.Empty<float>();
    public string Model { get; set; } = "text-embedding-004";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
