using System.Security.Cryptography;
using Application.DTOs.Auth.Requests;
using Application.DTOs.Auth.Responses;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Settings;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Google.Apis.Auth;
using Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPasswordHasherService _passwordHasherService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailSender _emailSender;
    private readonly AppSettings _appSettings;
    private readonly GoogleSettings _googleSettings;

    public AuthService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IPasswordHasherService passwordHasherService,
        IJwtTokenService jwtTokenService,
        IEmailSender emailSender,
        IOptions<AppSettings> appSettings,
        IOptions<GoogleSettings> googleSettings)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _passwordHasherService = passwordHasherService;
        _jwtTokenService = jwtTokenService;
        _emailSender = emailSender;
        _appSettings = appSettings.Value;
        _googleSettings = googleSettings.Value;
    }

    public async Task RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var isUnique = await _unitOfWork.Users.IsEmailUniqueAsync(normalizedEmail, cancellationToken);
        if (!isUnique)
        {
            throw new ConflictException("Email này đã được sử dụng bởi tài khoản khác.");
        }

        // Map DTO sang Entity qua AutoMapper
        var user = _mapper.Map<User>(request);
        user.PasswordHash = _passwordHasherService.HashPassword(user, request.Password);

        var rawToken = GenerateSecureToken();
        var verificationToken = new VerificationToken
        {
            UserId = user.Id,
            TokenHash = TokenHasher.HashToken(rawToken),
            Purpose = VerificationPurpose.EmailConfirmation,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(24)
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.VerificationTokens.AddAsync(verificationToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Tạo link xác minh và gửi email
        var verifyUrl = $"{_appSettings.FrontendBaseUrl.TrimEnd('/')}/verify-email?userId={user.Id}&token={rawToken}";
        var emailBody = $@"
            <h2>Chào mừng {user.DisplayName} đến với Social!</h2>
            <p>Vui lòng bấm vào liên kết bên dưới để xác minh địa chỉ email của bạn:</p>
            <p><a href=""{verifyUrl}"" style=""display:inline-block;padding:10px 20px;color:#fff;background-color:#007bff;text-decoration:none;border-radius:5px;"">Xác minh Email</a></p>
            <p>Hoặc copy đường link này vào trình duyệt: <br/>{verifyUrl}</p>
            <p>Liên kết này có hiệu lực trong 24 giờ.</p>";

        await _emailSender.SendEmailAsync(user.Email, "Xác minh tài khoản Social của bạn", emailBody, cancellationToken);
    }

    public async Task ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

        if (user.EmailConfirmed)
        {
            return;
        }

        var tokenHash = TokenHasher.HashToken(token);
        var verificationToken = await _unitOfWork.VerificationTokens
            .GetValidTokenAsync(userId, VerificationPurpose.EmailConfirmation, tokenHash, cancellationToken);

        if (verificationToken == null || verificationToken.IsConsumed || verificationToken.IsExpired)
        {
            throw new ValidationAppException("Token", "Liên kết xác minh không hợp lệ hoặc đã hết hạn.");
        }

        user.EmailConfirmed = true;
        verificationToken.ConsumedAtUtc = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        _unitOfWork.VerificationTokens.Update(verificationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ResendConfirmationAsync(ResendConfirmationRequestDto request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var user = await _unitOfWork.Users.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        // Không tiết lộ user có tồn tại hay đã xác minh hay chưa
        if (user == null || user.EmailConfirmed)
        {
            return;
        }

        // Cooldown 60s
        var latestToken = await _unitOfWork.VerificationTokens
            .GetLatestTokenAsync(user.Id, VerificationPurpose.EmailConfirmation, cancellationToken);

        if (latestToken != null && latestToken.CreatedAtUtc > DateTime.UtcNow.AddSeconds(-60))
        {
            return;
        }

        // Thu hồi toàn bộ token xác minh cũ chưa dùng
        var activeTokens = await _unitOfWork.VerificationTokens
            .GetActiveTokensAsync(user.Id, VerificationPurpose.EmailConfirmation, cancellationToken);

        foreach (var activeToken in activeTokens)
        {
            activeToken.ConsumedAtUtc = DateTime.UtcNow;
            _unitOfWork.VerificationTokens.Update(activeToken);
        }

        var rawToken = GenerateSecureToken();
        var newToken = new VerificationToken
        {
            UserId = user.Id,
            TokenHash = TokenHasher.HashToken(rawToken),
            Purpose = VerificationPurpose.EmailConfirmation,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(24)
        };

        await _unitOfWork.VerificationTokens.AddAsync(newToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var verifyUrl = $"{_appSettings.FrontendBaseUrl.TrimEnd('/')}/verify-email?userId={user.Id}&token={rawToken}";
        var emailBody = $@"
            <h2>Xác minh lại tài khoản Social</h2>
            <p>Chào {user.DisplayName}, chúng tôi nhận được yêu cầu gửi lại email xác minh.</p>
            <p><a href=""{verifyUrl}"" style=""display:inline-block;padding:10px 20px;color:#fff;background-color:#007bff;text-decoration:none;border-radius:5px;"">Xác minh Email</a></p>
            <p>Hoặc copy đường link này vào trình duyệt: <br/>{verifyUrl}</p>
            <p>Liên kết này có hiệu lực trong 24 giờ.</p>";

        await _emailSender.SendEmailAsync(user.Email, "Gửi lại xác minh tài khoản Social của bạn", emailBody, cancellationToken);
    }

    public async Task<LoginResultDto> LoginAsync(LoginRequestDto request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var user = await _unitOfWork.Users.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);
        if (user == null || string.IsNullOrEmpty(user.PasswordHash) || !_passwordHasherService.VerifyPassword(user, request.Password, user.PasswordHash))
        {
            throw new ValidationAppException("Credentials", "Email hoặc mật khẩu không chính xác.");
        }

        if (!user.EmailConfirmed)
        {
            throw new ValidationAppException("EMAIL_NOT_CONFIRMED", "Tài khoản chưa được xác minh email. Vui lòng kiểm tra hộp thư hoặc gửi lại yêu cầu xác minh.");
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var accessTokenExpiresAt = _jwtTokenService.GetAccessTokenExpiration();

        var rawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenExpiresAt = _jwtTokenService.GetRefreshTokenExpiration();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = TokenHasher.HashToken(rawRefreshToken),
            ExpiresAtUtc = refreshTokenExpiresAt,
            CreatedByIp = ipAddress
        };

        await _unitOfWork.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResultDto
        {
            AccessToken = accessToken,
            ExpiresAt = accessTokenExpiresAt,
            User = _mapper.Map<UserDto>(user),
            RawRefreshToken = rawRefreshToken,
            RefreshTokenExpiresAt = refreshTokenExpiresAt
        };
    }

    public async Task<LoginResultDto> GoogleLoginAsync(GoogleLoginRequestDto request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _googleSettings.ClientId }
            };
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
        }
        catch (Exception)
        {
            throw new ValidationAppException("IdToken", "Mã xác thực Google (ID Token) không hợp lệ hoặc đã hết hạn.");
        }

        if (string.IsNullOrWhiteSpace(payload.Email))
        {
            throw new ValidationAppException("Email", "Không thể lấy thông tin email từ tài khoản Google này.");
        }

        var normalizedEmail = payload.Email.Trim().ToUpperInvariant();

        // 1. Tìm user theo GoogleId trước, nếu chưa có thì tìm theo NormalizedEmail
        var user = await _unitOfWork.Users.GetByGoogleIdAsync(payload.Subject, cancellationToken);
        if (user == null)
        {
            user = await _unitOfWork.Users.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);
        }

        if (user == null)
        {
            // 2. Chưa có tài khoản -> Tạo mới hoàn toàn
            user = new User
            {
                Email = payload.Email.Trim(),
                NormalizedEmail = normalizedEmail,
                DisplayName = string.IsNullOrWhiteSpace(payload.Name) ? payload.Email.Split('@')[0] : payload.Name.Trim(),
                AvatarUrl = payload.Picture,
                GoogleId = payload.Subject,
                EmailConfirmed = true,
                PasswordHash = null
            };

            await _unitOfWork.Users.AddAsync(user, cancellationToken);
        }
        else
        {
            // 3. Đã có tài khoản -> Liên kết GoogleId và kích hoạt email nếu chưa
            var isUpdated = false;

            if (string.IsNullOrEmpty(user.GoogleId))
            {
                user.GoogleId = payload.Subject;
                isUpdated = true;
            }

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                isUpdated = true;
            }

            // Nếu user chưa có avatar thì lấy avatar từ Google
            if (string.IsNullOrEmpty(user.AvatarUrl) && !string.IsNullOrEmpty(payload.Picture))
            {
                user.AvatarUrl = payload.Picture;
                isUpdated = true;
            }

            if (isUpdated)
            {
                user.UpdatedAtUtc = DateTime.UtcNow;
                _unitOfWork.Users.Update(user);
            }
        }

        // 4. Phát cặp Token (AccessToken + RefreshToken)
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var accessTokenExpiresAt = _jwtTokenService.GetAccessTokenExpiration();

        var rawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenExpiresAt = _jwtTokenService.GetRefreshTokenExpiration();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = TokenHasher.HashToken(rawRefreshToken),
            ExpiresAtUtc = refreshTokenExpiresAt,
            CreatedByIp = ipAddress
        };

        await _unitOfWork.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResultDto
        {
            AccessToken = accessToken,
            ExpiresAt = accessTokenExpiresAt,
            User = _mapper.Map<UserDto>(user),
            RawRefreshToken = rawRefreshToken,
            RefreshTokenExpiresAt = refreshTokenExpiresAt
        };
    }

    public async Task<RefreshResultDto> RefreshAsync(string rawRefreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            throw new ValidationAppException("Token", "Refresh token không hợp lệ.");
        }

        var tokenHash = TokenHasher.HashToken(rawRefreshToken);
        var token = await _unitOfWork.RefreshTokens.GetByTokenHashWithUserAsync(tokenHash, cancellationToken);

        if (token == null || token.IsRevoked || token.IsExpired)
        {
            throw new ValidationAppException("Token", "Phiên đăng nhập đã hết hạn hoặc không hợp lệ. Vui lòng đăng nhập lại.");
        }

        // Token rotation: Thu hồi token cũ
        token.RevokedAtUtc = DateTime.UtcNow;

        // Sinh cặp token mới
        var newAccessToken = _jwtTokenService.GenerateAccessToken(token.User);
        var newAccessTokenExpiresAt = _jwtTokenService.GetAccessTokenExpiration();

        var newRawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var newRefreshTokenExpiresAt = _jwtTokenService.GetRefreshTokenExpiration();
        var newTokenHash = TokenHasher.HashToken(newRawRefreshToken);

        token.ReplacedByTokenHash = newTokenHash;
        _unitOfWork.RefreshTokens.Update(token);

        var newRefreshToken = new RefreshToken
        {
            UserId = token.UserId,
            TokenHash = newTokenHash,
            ExpiresAtUtc = newRefreshTokenExpiresAt,
            CreatedByIp = ipAddress
        };

        await _unitOfWork.RefreshTokens.AddAsync(newRefreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RefreshResultDto
        {
            AccessToken = newAccessToken,
            ExpiresAt = newAccessTokenExpiresAt,
            RawRefreshToken = newRawRefreshToken,
            RefreshTokenExpiresAt = newRefreshTokenExpiresAt
        };
    }

    public async Task LogoutAsync(string rawRefreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return;
        }

        var tokenHash = TokenHasher.HashToken(rawRefreshToken);
        var token = await _unitOfWork.RefreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (token != null && !token.IsRevoked)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            _unitOfWork.RefreshTokens.Update(token);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var user = await _unitOfWork.Users.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        // Luôn kết thúc thành công để không lộ email có tồn tại hay không
        if (user == null || !user.EmailConfirmed)
        {
            return;
        }

        // Thu hồi toàn bộ token reset mật khẩu cũ chưa dùng
        var activeTokens = await _unitOfWork.VerificationTokens
            .GetActiveTokensAsync(user.Id, VerificationPurpose.PasswordReset, cancellationToken);

        foreach (var activeToken in activeTokens)
        {
            activeToken.ConsumedAtUtc = DateTime.UtcNow;
            _unitOfWork.VerificationTokens.Update(activeToken);
        }

        var rawToken = GenerateSecureToken();
        var newToken = new VerificationToken
        {
            UserId = user.Id,
            TokenHash = TokenHasher.HashToken(rawToken),
            Purpose = VerificationPurpose.PasswordReset,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };

        await _unitOfWork.VerificationTokens.AddAsync(newToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var resetUrl = $"{_appSettings.FrontendBaseUrl.TrimEnd('/')}/reset-password?userId={user.Id}&token={rawToken}";
        var emailBody = $@"
            <h2>Yêu cầu đặt lại mật khẩu</h2>
            <p>Chào {user.DisplayName}, chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản Social của bạn.</p>
            <p><a href=""{resetUrl}"" style=""display:inline-block;padding:10px 20px;color:#fff;background-color:#dc3545;text-decoration:none;border-radius:5px;"">Đặt lại mật khẩu</a></p>
            <p>Hoặc copy đường link này vào trình duyệt: <br/>{resetUrl}</p>
            <p>Liên kết này có hiệu lực trong 1 giờ. Nếu bạn không gửi yêu cầu này, hãy bỏ qua email này.</p>";

        await _emailSender.SendEmailAsync(user.Email, "Đặt lại mật khẩu tài khoản Social của bạn", emailBody, cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User", request.UserId);
        }

        var tokenHash = TokenHasher.HashToken(request.Token);
        var verificationToken = await _unitOfWork.VerificationTokens
            .GetValidTokenAsync(request.UserId, VerificationPurpose.PasswordReset, tokenHash, cancellationToken);

        if (verificationToken == null || verificationToken.IsConsumed || verificationToken.IsExpired)
        {
            throw new ValidationAppException("Token", "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
        }

        // Thực hiện trong Transaction qua UnitOfWork
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Cập nhật mật khẩu mới
            user.PasswordHash = _passwordHasherService.HashPassword(user, request.NewPassword);
            _unitOfWork.Users.Update(user);

            // 2. Tiêu thụ token reset hiện tại và mọi token reset còn hiệu lực khác
            var allResetTokens = await _unitOfWork.VerificationTokens
                .GetActiveTokensAsync(user.Id, VerificationPurpose.PasswordReset, cancellationToken);

            foreach (var token in allResetTokens)
            {
                token.ConsumedAtUtc = DateTime.UtcNow;
                _unitOfWork.VerificationTokens.Update(token);
            }

            // 3. Thu hồi TOÀN BỘ refresh token còn hiệu lực -> Đăng xuất mọi thiết bị
            var activeRefreshTokens = await _unitOfWork.RefreshTokens
                .GetActiveTokensByUserIdAsync(user.Id, cancellationToken);

            foreach (var rt in activeRefreshTokens)
            {
                rt.RevokedAtUtc = DateTime.UtcNow;
                _unitOfWork.RefreshTokens.Update(rt);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
