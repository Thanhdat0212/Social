using Domain.Common;

namespace Domain.Entities.Marketplace.Complaints;

/// <summary>
/// Bằng chứng khiếu nại (ảnh chụp màn hình lỗi, video thao tác không thành công)
/// </summary>
public class ComplaintEvidence : BaseEntity
{
    public Guid ComplaintId { get; private set; }
    public string FileUrl { get; private set; } = string.Empty;
    public string FileType { get; private set; } = "Image"; // "Image" | "Video" | "Document"
    public string? Note { get; private set; }

    // Navigation
    public Complaint? Complaint { get; private set; }

    protected ComplaintEvidence() { }

    public ComplaintEvidence(Guid complaintId, string fileUrl, string fileType = "Image", string? note = null)
    {
        if (complaintId == Guid.Empty) throw new ArgumentException("ComplaintId không hợp lệ.", nameof(complaintId));
        if (string.IsNullOrWhiteSpace(fileUrl)) throw new ArgumentException("URL bằng chứng không được để trống.", nameof(fileUrl));

        ComplaintId = complaintId;
        FileUrl = fileUrl.Trim();
        FileType = string.IsNullOrWhiteSpace(fileType) ? "Image" : fileType.Trim();
        Note = note?.Trim();
    }
}
