namespace API.Auth;

public static class RefreshTokenCookie
{
    public const string CookieName = "social_rt";
    public const string CookiePath = "/api/auth";

    public static void Append(HttpResponse response, string rawRefreshToken, DateTime expiresAt)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Chrome/Edge treat localhost as secure context
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
            Expires = expiresAt
        };

        response.Cookies.Append(CookieName, rawRefreshToken, cookieOptions);
    }

    public static string? Get(HttpRequest request)
    {
        return request.Cookies.TryGetValue(CookieName, out var token) ? token : null;
    }

    public static void Delete(HttpResponse response)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
            Expires = DateTime.UtcNow.AddDays(-1)
        };

        response.Cookies.Delete(CookieName, cookieOptions);
    }
}
