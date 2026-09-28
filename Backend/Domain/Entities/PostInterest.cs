namespace Domain.Entities;

public class PostInterest // Bảng kết nối giữa POST VÀ INTEREST 
{
    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;

    public Guid InterestId { get; set; }
    public Interest Interest { get; set; } = null!;

    public double Confidence { get; set; } = 1.0; // Trường độ tin cậy do AI tự đánh giá 
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
