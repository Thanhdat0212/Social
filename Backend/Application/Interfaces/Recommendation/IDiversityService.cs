using Application.DTOs.Recommendation;

namespace Application.Interfaces.Recommendation;

public interface IDiversityService
{
    IReadOnlyList<Guid> ApplyDiversity(
        IReadOnlyList<ScoredCandidateDto> rankedCandidates,
        int maxConsecutiveSameTopic = 2);
}
