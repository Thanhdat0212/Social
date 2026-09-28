using Application.DTOs.AI;

namespace Application.Interfaces;

public interface IAiContentAnalyzer
{
    /// <summary>
    /// Phân tích nội dung bài viết và trả về danh sách các chủ đề phù hợp kèm độ tin cậy
    /// </summary>
    /// <param name="content">Văn bản nội dung bài viết</param>
    /// <param name="candidateTopics">Danh sách tên các chủ đề đang có trong hệ thống</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyList<TopicClassificationResult>> AnalyzeContentTopicsAsync(
        string content,
        IReadOnlyList<string> candidateTopics,
        CancellationToken cancellationToken = default);
}
