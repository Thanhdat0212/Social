using Application.DTOs.Recommendation;
using Domain.Entities;

namespace Application.Interfaces.Recommendation;

public interface IDiversityService
{
    IReadOnlyList<Post> ApplyDiversity(
        IReadOnlyList<ScoredCandidateDto> rankedCandidates,
        int maxConsecutiveSameTopic = 2);
}
