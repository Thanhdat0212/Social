using System.Text.RegularExpressions;
using Application.DTOs.Auth;
using FluentValidation;

namespace Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không đúng định dạng.")
            .MaximumLength(256).WithMessage("Email tối đa 256 ký tự.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Tên hiển thị không được để trống.")
            .MaximumLength(100).WithMessage("Tên hiển thị tối đa 100 ký tự.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.")
            .MinimumLength(8).WithMessage("Mật khẩu phải có ít nhất 8 ký tự.")
            .Must(p => Regex.IsMatch(p, "[A-Z]")).WithMessage("Mật khẩu phải chứa ít nhất một chữ hoa.")
            .Must(p => Regex.IsMatch(p, "[a-z]")).WithMessage("Mật khẩu phải chứa ít nhất một chữ thường.")
            .Must(p => Regex.IsMatch(p, "[0-9]")).WithMessage("Mật khẩu phải chứa ít nhất một chữ số.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Xác nhận mật khẩu không được để trống.")
            .Equal(x => x.Password).WithMessage("Xác nhận mật khẩu không khớp.");
    }
}
