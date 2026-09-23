using System.Security.Cryptography;
using Application.DTOs.Auth;
using Application.Interfaces;
using Application.Options;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Infrastructure.Persistence;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly SocialDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IPasswordHasherService _passwordHasherService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailSender _emailSender;
    private readonly AppOptions _appOptions;

    public AuthService(
        SocialDbContext dbContext,
        IMapper mapper,
        IPasswordHasherService passwordHasherService,
        IJwtTokenService jwtTokenService,
        IEmailSender emailSender,
        IOptions<AppOptions> appOptions)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _passwordHasherService = passwordHasherService;
        _jwtTokenService = jwtTokenService;
        _emailSender = emailSender;
        _appOptions = appOptions.Value;
    }

    public async Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var existingUser = await _dbContext.Users
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (existingUser)
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

        _dbContext.Users.Add(user);
        _dbContext.VerificationTokens.Add(verificationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Tạo link xác minh và gửi email
        var verifyUrl = $"{_appOptions.FrontendBaseUrl.TrimEnd('/')}/verify-email?userId={user.Id}&token={rawToken}";
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
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

        if (user.EmailConfirmed)
        {
            return;
        }

        var tokenHash = TokenHasher.HashToken(token);
        var verificationToken = await _dbContext.VerificationTokens
            .FirstOrDefaultAsync(vt => vt.UserId == userId 
                                    && vt.Purpose == VerificationPurpose.EmailConfirmation 
                                    && vt.TokenHash == tokenHash, cancellationToken);

        if (verificationToken == null || verificationToken.IsConsumed || verificationToken.IsExpired)
        {
            throw new ValidationAppException("Token", "Liên kết xác minh không hợp lệ hoặc đã hết hạn.");
        }

        user.EmailConfirmed = true;
        verificationToken.ConsumedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ResendConfirmationAsync(ResendConfirmationRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        // Không tiết lộ user có tồn tại hay đã xác minh hay chưa
        if (user == null || user.EmailConfirmed)
        {
            return;
        }

        // Cooldown 60s
        var latestToken = await _dbContext.VerificationTokens
            .Where(vt => vt.UserId == user.Id && vt.Purpose == VerificationPurpose.EmailConfirmation)
            .OrderByDescending(vt => vt.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestToken != null && latestToken.CreatedAtUtc > DateTime.UtcNow.AddSeconds(-60))
        {
            return;
        }

        // Thu hồi toàn bộ token xác minh cũ chưa dùng
        var activeTokens = await _dbContext.VerificationTokens
            .Where(vt => vt.UserId == user.Id && vt.Purpose == VerificationPurpose.EmailConfirmation && vt.ConsumedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var activeToken in activeTokens)
        {
            activeToken.ConsumedAtUtc = DateTime.UtcNow;
        }

        var rawToken = GenerateSecureToken();
        var newToken = new VerificationToken
        {
            UserId = user.Id,
            TokenHash = TokenHasher.HashToken(rawToken),
            Purpose = VerificationPurpose.EmailConfirmation,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(24)
        };

        _dbContext.VerificationTokens.Add(newToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var verifyUrl = $"{_appOptions.FrontendBaseUrl.TrimEnd('/')}/verify-email?userId={user.Id}&token={rawToken}";
        var emailBody = $@"
            <h2>Xác minh lại tài khoản Social</h2>
            <p>Chào {user.DisplayName}, chúng tôi nhận được yêu cầu gửi lại email xác minh.</p>
            <p><a href=""{verifyUrl}"" style=""display:inline-block;padding:10px 20px;color:#fff;background-color:#007bff;text-decoration:none;border-radius:5px;"">Xác minh Email</a></p>
            <p>Hoặc copy đường link này vào trình duyệt: <br/>{verifyUrl}</p>
            <p>Liên kết này có hiệu lực trong 24 giờ.</p>";

        await _emailSender.SendEmailAsync(user.Email, "Gửi lại xác minh tài khoản Social của bạn", emailBody, cancellationToken);
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user == null || !_passwordHasherService.VerifyPassword(user, request.Password, user.PasswordHash))
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

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new LoginResult
        {
            AccessToken = accessToken,
            ExpiresAt = accessTokenExpiresAt,
            User = _mapper.Map<UserDto>(user),
            RawRefreshToken = rawRefreshToken,
            RefreshTokenExpiresAt = refreshTokenExpiresAt
        };
    }

    public async Task<RefreshResult> RefreshAsync(string rawRefreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            throw new ValidationAppException("Token", "Refresh token không hợp lệ.");
        }

        var tokenHash = TokenHasher.HashToken(rawRefreshToken);
        var token = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

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

        var newRefreshToken = new RefreshToken
        {
            UserId = token.UserId,
            TokenHash = newTokenHash,
            ExpiresAtUtc = newRefreshTokenExpiresAt,
            CreatedByIp = ipAddress
        };

        _dbContext.RefreshTokens.Add(newRefreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RefreshResult
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
        var token = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (token != null && !token.IsRevoked)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
