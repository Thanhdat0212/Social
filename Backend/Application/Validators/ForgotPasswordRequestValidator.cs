using Application.DTOs.Auth.Requests;
using FluentValidation;

namespace Application.Validators;

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequestDto>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không đúng định dạng.")
            .MaximumLength(256).WithMessage("Email tối đa 256 ký tự.");
    }
}
