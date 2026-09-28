using Application.DTOs.Interests;
using FluentValidation;

namespace Application.Validators;

public class SelectInterestsValidator : AbstractValidator<SelectInterestsRequestDto>
{
    public SelectInterestsValidator()
    {
        RuleFor(x => x.InterestIds)
            .NotNull().WithMessage("Danh sách sở thích không được để trống.")
            .Must(ids => ids != null && ids.Distinct().Count() >= 1)
            .WithMessage("Vui lòng chọn tối thiểu 1 sở thích.");

        RuleForEach(x => x.InterestIds)
            .NotEmpty().WithMessage("ID sở thích không được để trống.");
    }
}
