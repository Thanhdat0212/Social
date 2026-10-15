using System.Text.RegularExpressions;
using Application.DTOs.Auth.Requests;
using FluentValidation;

namespace Application.Validators;

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequestDto>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId không hợp lệ.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token không được để trống.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu mới không được để trống.")
            .MinimumLength(8).WithMessage("Mật khẩu phải có ít nhất 8 ký tự.")
            .Must(p => Regex.IsMatch(p, "[A-Z]")).WithMessage("Mật khẩu phải chứa ít nhất một chữ hoa.")
            .Must(p => Regex.IsMatch(p, "[a-z]")).WithMessage("Mật khẩu phải chứa ít nhất một chữ thường.")
            .Must(p => Regex.IsMatch(p, "[0-9]")).WithMessage("Mật khẩu phải chứa ít nhất một chữ số.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Xác nhận mật khẩu không được để trống.")
            .Equal(x => x.NewPassword).WithMessage("Xác nhận mật khẩu không khớp.");
    }
}
