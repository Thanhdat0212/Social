namespace Application.DTOs.Interests;

public class SelectInterestsRequestDto
{
    public List<Guid> InterestIds { get; set; } = new();
}
