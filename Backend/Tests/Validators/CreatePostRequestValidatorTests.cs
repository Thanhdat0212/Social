using Application.DTOs.Posts;
using Application.Validators;
using FluentAssertions;

namespace Tests.Validators;

public class CreatePostRequestValidatorTests
{
    private readonly CreatePostRequestValidator _validator = new();

    [Fact]
    public void Validate_WhenBothContentAndMediaAreEmpty_ShouldFail()
    {
        var dto = new CreatePostRequestDto
        {
            Content = "   ",
            MediaUrls = new List<string>()
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenMediaUrlsExceedTen_ShouldFail()
    {
        var dto = new CreatePostRequestDto
        {
            Content = "Post with too many images",
            MediaUrls = Enumerable.Range(1, 11).Select(i => $"https://cdn.example.com/{i}.jpg").ToList()
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePostRequestDto.MediaUrls));
    }

    [Fact]
    public void Validate_WhenContentOrMediaIsValid_ShouldSucceed()
    {
        var dto = new CreatePostRequestDto
        {
            Content = "Valid post content",
            MediaUrls = new List<string> { "https://cdn.example.com/1.jpg" }
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }
}

