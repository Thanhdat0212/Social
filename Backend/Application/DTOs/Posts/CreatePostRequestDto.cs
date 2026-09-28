namespace Application.DTOs.Posts;

public class CreatePostRequestDto
{
    public string Content { get; set; } = string.Empty;
    public List<string> MediaUrls { get; set; } = new();
}
