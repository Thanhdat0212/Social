namespace Application.DTOs.Posts;

public class PostDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<string> MediaUrls { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public int ViewCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    public PostAuthorDto Author { get; set; } = null!;
    public List<PostTopicDto> Topics { get; set; } = new();
    public List<PostTaggedProductDto> TaggedProducts { get; set; } = new();
}

public class PostTaggedProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public long MinPrice { get; set; }
    public long MaxPrice { get; set; }
    public string? CoverImageUrl { get; set; }
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string ShopSlug { get; set; } = string.Empty;
}

