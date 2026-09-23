using Application.DTOs.Auth.Requests;
using FluentValidation;

namespace Application.Validators.Auth;

public class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequestDto>
{
    public GoogleLoginRequestValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("Google ID Token không được để trống.");
    }
}
