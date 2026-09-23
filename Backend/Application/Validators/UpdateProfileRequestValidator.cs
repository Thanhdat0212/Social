using Application.DTOs.Profile.Requests;
using FluentValidation;

namespace Application.Validators;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequestDto>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Tên hiển thị không được để trống.")
            .MaximumLength(100).WithMessage("Tên hiển thị tối đa 100 ký tự.");

        RuleFor(x => x.Bio)
            .MaximumLength(500).WithMessage("Tiểu sử tối đa 500 ký tự.");
    }
}
