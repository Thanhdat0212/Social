using Application.DTOs.Auth.Requests;
using Application.Validators;
using FluentAssertions;

namespace Tests.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void Validate_WhenRequestIsValid_ShouldSucceed()
    {
        var dto = new RegisterRequestDto
        {
            Email = "user@example.com",
            DisplayName = "Valid User",
            Password = "Password123",
            ConfirmPassword = "Password123"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("short1A")]
    [InlineData("alllowercase123")]
    [InlineData("ALLUPPERCASE123")]
    [InlineData("NoNumbersHere")]
    public void Validate_WhenPasswordIsWeak_ShouldFail(string invalidPassword)
    {
        var dto = new RegisterRequestDto
        {
            Email = "user@example.com",
            DisplayName = "Valid User",
            Password = invalidPassword,
            ConfirmPassword = invalidPassword
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequestDto.Password));
    }

    [Fact]
    public void Validate_WhenConfirmPasswordDoesNotMatch_ShouldFail()
    {
        var dto = new RegisterRequestDto
        {
            Email = "user@example.com",
            DisplayName = "Valid User",
            Password = "Password123",
            ConfirmPassword = "Mismatch123"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequestDto.ConfirmPassword));
    }
}

