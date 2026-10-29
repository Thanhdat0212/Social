using Application.DTOs.Recommendation;
using FluentAssertions;
using Infrastructure.Recommendation.Diversity;
using Xunit;

namespace Tests.Recommendation;

public class DiversityServiceTests
{
    private readonly DiversityService _diversityService = new();

    [Fact]
    public void ApplyDiversity_WhenEmpty_ShouldReturnEmptyList()
    {
        var result = _diversityService.ApplyDiversity(Array.Empty<ScoredCandidateDto>());
        result.Should().BeEmpty();
    }

    [Fact]
    public void ApplyDiversity_WhenConsecutiveTopicsExceedLimit_ShouldBufferAndReorder()
    {
        var topicA = Guid.NewGuid();
        var topicB = Guid.NewGuid();

        var candidates = new List<ScoredCandidateDto>
        {
            new() { Post = new PostCandidateDto { Id = Guid.NewGuid() }, PrimaryInterestId = topicA, FinalScore = 100 },
            new() { Post = new PostCandidateDto { Id = Guid.NewGuid() }, PrimaryInterestId = topicA, FinalScore = 95 },
            new() { Post = new PostCandidateDto { Id = Guid.NewGuid() }, PrimaryInterestId = topicA, FinalScore = 90 }, // should be buffered
            new() { Post = new PostCandidateDto { Id = Guid.NewGuid() }, PrimaryInterestId = topicB, FinalScore = 85 },
            new() { Post = new PostCandidateDto { Id = Guid.NewGuid() }, PrimaryInterestId = topicB, FinalScore = 80 },
        };

        var result = _diversityService.ApplyDiversity(candidates, maxConsecutiveSameTopic: 2);

        result.Should().HaveCount(5);
        result[0].Should().Be(candidates[0].Post.Id);
        result[1].Should().Be(candidates[1].Post.Id);
        // Candidate 2 (topic A) was buffered, so candidate 3 (topic B) appears next
        result[2].Should().Be(candidates[3].Post.Id);
    }
}

