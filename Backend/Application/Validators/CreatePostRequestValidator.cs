using Application.DTOs.Posts;
using FluentValidation;

namespace Application.Validators;

public class CreatePostRequestValidator : AbstractValidator<CreatePostRequestDto>
{
    public CreatePostRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Content) || (x.MediaUrls != null && x.MediaUrls.Count > 0))
            .WithMessage("Bài viết phải có ít nhất nội dung văn bản hoặc hình ảnh.");

        RuleFor(x => x.Content)
            .MaximumLength(5000)
            .WithMessage("Nội dung bài viết không được vượt quá 5.000 ký tự.");

        RuleFor(x => x.MediaUrls)
            .Must(urls => urls == null || urls.Count <= 10)
            .WithMessage("Không được đính kèm quá 10 hình ảnh trong một bài viết.");
    }
}
