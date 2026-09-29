using Application.DTOs.Recommendation;
using Application.Interfaces.Recommendation;
using Domain.Entities;

namespace Infrastructure.Recommendation.Diversity;

public class DiversityService : IDiversityService
{
    public IReadOnlyList<Post> ApplyDiversity(
        IReadOnlyList<ScoredCandidateDto> rankedCandidates,
        int maxConsecutiveSameTopic = 2)
    {
        if (rankedCandidates.Count == 0)
        {
            return Array.Empty<Post>();
        }

        var result = new List<Post>(rankedCandidates.Count);
        var buffer = new List<ScoredCandidateDto>();

        Guid? currentTopicId = null;
        int consecutiveCount = 0;

        foreach (var candidate in rankedCandidates)
        {
            var topicId = candidate.PrimaryInterestId;

            // Nếu bài viết cùng chủ đề với bài liền trước
            if (topicId.HasValue && currentTopicId.HasValue && topicId.Value == currentTopicId.Value)
            {
                if (consecutiveCount >= maxConsecutiveSameTopic)
                {
                    // Đạt ngưỡng tối đa cùng topic liên tiếp -> Tạm đẩy vào buffer
                    buffer.Add(candidate);
                    continue;
                }

                // Vẫn trong giới hạn cho phép
                result.Add(candidate.Post);
                consecutiveCount++;
            }
            else
            {
                // Chủ đề khác -> Cho phép xuất hiện
                result.Add(candidate.Post);
                currentTopicId = topicId;
                consecutiveCount = 1;

                // Thử giải phóng bài trong buffer nếu chủ đề của bài trong buffer khác chủ đề hiện tại
                if (buffer.Count > 0)
                {
                    for (int i = 0; i < buffer.Count; i++)
                    {
                        var buffered = buffer[i];
                        if (!buffered.PrimaryInterestId.HasValue || buffered.PrimaryInterestId != currentTopicId)
                        {
                            result.Add(buffered.Post);
                            currentTopicId = buffered.PrimaryInterestId;
                            consecutiveCount = 1;
                            buffer.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }

        // Đổ toàn bộ các bài còn lại trong buffer vào cuối danh sách
        foreach (var remaining in buffer)
        {
            result.Add(remaining.Post);
        }

        return result;
    }
}
