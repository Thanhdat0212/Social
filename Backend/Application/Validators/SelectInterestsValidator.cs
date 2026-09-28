using Application.DTOs.Interests;
using FluentValidation;

namespace Application.Validators;

public class SelectInterestsValidator : AbstractValidator<SelectInterestsRequestDto>
{
    public SelectInterestsValidator()
    {
        RuleFor(x => x.InterestIds)
            .NotNull().WithMessage("Danh sách sở thích không được để trống.")
            .Must(ids => ids != null && ids.Distinct().Count() >= 3)
            .WithMessage("Vui lòng chọn tối thiểu 3 sở thích để hệ thống gợi ý nội dung chính xác.");

        RuleForEach(x => x.InterestIds)
            .NotEmpty().WithMessage("ID sở thích không được để trống.");
    }
}
