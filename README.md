# Social — Mạng xã hội (Phase 1: Auth + Hồ sơ cá nhân)

Kế hoạch chi tiết (kiến trúc, quyết định kỹ thuật, 17 giai đoạn): [Social-Phase1-KeHoach.md](./Social-Phase1-KeHoach.md)

## 📊 Trạng thái hiện tại

Cập nhật: 2026-09-23

### Backend (GĐ 1–8) — ✅ Đã hoàn thành 100%

| GĐ | Tên | Trạng thái | Ghi chú |
|---|---|:---:|---|
| 1 | Repo & solution | ✅ | 4 project Clean Architecture, đúng chiều phụ thuộc, build sạch (`net8.0`) |
| 2 | Domain & DB | ✅ | 3 entity (`User`, `RefreshToken`, `VerificationToken`), `SocialDbContext`, Repositories & UnitOfWork, migration đã áp dụng lên Supabase `social-dev` |
| 3 | Hasher, JWT, Swagger | ✅ | PBKDF2 (`PasswordHasherService`), JWT Token (15p), Swagger UI có nút Authorize Bearer |
| 4 | Đăng ký + xác minh + gửi lại | ✅ | DTO, FluentValidation, link xác minh 24h, cooldown gửi lại 60s, 3 email provider (Console, SMTP, Brevo) |
| 5 | Login / refresh / logout qua cookie | ✅ | Access token trong body, refresh token trong cookie `social_rt` (`httpOnly; Secure; SameSite=Strict`), xoay vòng token |
| 6 | Quên / đặt lại mật khẩu | ✅ | Link reset 1h, luôn trả 200 khi forgot, reset trong transaction đổi mật khẩu + thu hồi toàn bộ refresh token |
| 7 | Hồ sơ + avatar | ✅ | `GET/PUT /api/profile/me`, upload avatar Cloudinary (≤5MB, JPG/PNG/WebP, auto-crop khuôn mặt) |
| 8 | Exception + pipeline hoàn chỉnh | ✅ | `GlobalExceptionHandler` tập trung (ProblemDetails RFC 7807), pipeline chuẩn bị sẵn cho proxy 2 tầng |
| **+** | **Google OAuth 2.0 (Mới)** | ✅ | Đăng nhập Google ID Token (`Google.Apis.Auth`), tự động liên kết tài khoản cũ, cập nhật avatar Google |

### Frontend (GĐ 9–16) & Triển khai (GĐ 17)

Sẵn sàng bắt đầu (`FE/` khởi tạo với React + Vite + TypeScript).

---

## 📡 Danh sách API Endpoints (12 endpoints)

| # | Method | Endpoint | Quyền | Mô tả |
|---|---|---|:---:|---|
| 1 | `POST` | `/api/auth/register` | Public | Đăng ký tài khoản mới & gửi link xác minh 24h |
| 2 | `GET` | `/api/auth/confirm-email` | Public | Xác minh email qua token trong link |
| 3 | `POST` | `/api/auth/resend-confirmation` | Public | Gửi lại email xác minh (luôn trả 200, cooldown 60s) |
| 4 | `POST` | `/api/auth/login` | Public | Đăng nhập bằng Email/Password (trả JWT body + cookie `social_rt`) |
| 5 | `POST` | `/api/auth/google` | Public | **Đăng nhập / Đăng ký bằng Google ID Token** |
| 6 | `POST` | `/api/auth/refresh` | Cookie | Làm mới access token thông qua cookie `social_rt` (xoay vòng token) |
| 7 | `POST` | `/api/auth/logout` | Cookie | Đăng xuất: thu hồi refresh token trong DB và xóa cookie |
| 8 | `POST` | `/api/auth/forgot-password` | Public | Quên mật khẩu: gửi link reset 1h (luôn trả 200) |
| 9 | `POST` | `/api/auth/reset-password` | Public | Đặt lại mật khẩu mới (trong transaction: đổi pass + thu hồi mọi token) |
| 10 | `GET` | `/api/profile/me` | Bearer | Lấy thông tin hồ sơ của người dùng hiện tại |
| 11 | `PUT` | `/api/profile/me` | Bearer | Cập nhật tên hiển thị (`DisplayName`) và tiểu sử (`Bio`) |
| 12 | `POST` | `/api/profile/me/avatar` | Bearer | Upload avatar lên Cloudinary (multipart form-data, max 5MB) |

---

## 📦 Cấu trúc thư mục

```
Social/
├── Backend/
│   ├── Domain/                          # Core layer - không phụ thuộc project nào khác
│   │   ├── Common/
│   │   │   └── BaseEntity.cs            # Id, CreatedAtUtc, UpdatedAtUtc
│   │   ├── Entities/
│   │   │   ├── User.cs                  # Email, GoogleId, PasswordHash?, AvatarUrl...
│   │   │   ├── RefreshToken.cs
│   │   │   └── VerificationToken.cs
│   │   ├── Enums/
│   │   │   └── VerificationPurpose.cs   # EmailConfirmation | PasswordReset
│   │   └── Exceptions/
│   │       ├── NotFoundException.cs
│   │       ├── ConflictException.cs
│   │       └── ValidationAppException.cs
│   │
│   ├── Application/                     # Business logic - chỉ phụ thuộc Domain
│   │   ├── Common/
│   │   │   ├── ICurrentUserService.cs
│   │   │   └── Mappings/MappingProfile.cs       # AutoMapper: User -> UserDto/ProfileDto
│   │   ├── DTOs/
│   │   │   ├── Auth/Requests/             # Register, Login, GoogleLogin, Refresh, Forgot, Reset...
│   │   │   ├── Auth/Responses/            # Login, Refresh, UserDto...
│   │   │   └── Profile/Requests|Responses/
│   │   ├── Interfaces/                   # IAuthService, IProfileService, IEmailSender, IJwtTokenService, IPasswordHasherService, IAvatarStorageService
│   │   │   └── Repositories/             # IUnitOfWork, IGenericRepository<T>, IUserRepository, IRefreshTokenRepository, IVerificationTokenRepository
│   │   ├── Settings/                     # AppSettings, JwtSettings, EmailSettings, SmtpSettings, BrevoSettings, CloudinarySettings, GoogleSettings
│   │   ├── Validators/                   # FluentValidation cho từng request DTO
│   │   └── DependencyInjection.cs
│   │
│   ├── Infrastructure/                  # Implement các interface của Application
│   │   ├── Email/                        # ConsoleEmailSender, SmtpEmailSender, BrevoEmailSender
│   │   ├── Identity/CurrentUserService.cs
│   │   ├── Media/CloudinaryAvatarService.cs
│   │   ├── Persistence/
│   │   │   ├── SocialDbContext.cs
│   │   │   ├── SocialDbContextFactory.cs # design-time factory cho `dotnet ef migrations`
│   │   │   ├── Configurations/           # Fluent API cho User/RefreshToken/VerificationToken
│   │   │   ├── Migrations/               # Initial, AddGoogleAuth
│   │   │   └── Repositories/             # GenericRepository<T>, UserRepository, RefreshTokenRepository, VerificationTokenRepository, UnitOfWork
│   │   ├── Security/                     # PasswordHasherService (PBKDF2), JwtTokenService, TokenHasher (SHA-256)
│   │   ├── Services/                     # AuthService, ProfileService — implementation thật, gọi qua IUnitOfWork
│   │   └── DependencyInjection.cs
│   │
│   ├── API/                             # Presentation layer
│   │   ├── Auth/RefreshTokenCookie.cs    # Helper Append/Get/Delete cookie `social_rt`
│   │   ├── Controllers/AuthController.cs
│   │   ├── Controllers/ProfileController.cs
│   │   ├── ErrorHandling/GlobalExceptionHandler.cs # Bắt lỗi toàn cục -> RFC ProblemDetails
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   └── Backend.slnx
│
├── FE/                                  # React + Vite + TypeScript
├── Social-Phase1-KeHoach.md              # Kế hoạch chi tiết 17 giai đoạn
└── Social-Phase1-KeHoach_de_dac_de_doc.md
```

---

## 🏗️ Kiến trúc Layers & Quy tắc Dependency

```
API ──► Infrastructure ──► Application ──► Domain
 │                              ▲
 └──────────────────────────────┘
   (Application không biết Infrastructure/API tồn tại)
```

1. **Domain**: Entity, Enum, Exception thuần. Không phụ thuộc project nào khác, không có logic hạ tầng hay framework.
2. **Application**: DTO, FluentValidation, và **interface** cho mọi nghiệp vụ/hạ tầng (`IAuthService`, `IProfileService`, `IEmailSender`, `IJwtTokenService`, `IUnitOfWork`...). Chỉ phụ thuộc `Domain`.
3. **Infrastructure**: Implement toàn bộ interface của Application: EF Core qua Repository/`IUnitOfWork`, JWT, băm mật khẩu, gửi email, lưu trữ media (Cloudinary), Google Auth SDK. Phụ thuộc `Application` + `Domain`.
4. **API**: Controllers, `Program.cs`, middleware xử lý lỗi `GlobalExceptionHandler`, helper cookie `RefreshTokenCookie`. Phụ thuộc cả 3 layer còn lại.

---

## 🧩 Nguyên tắc thiết kế đã chọn

| Có dùng | Không dùng | Lý do |
|---|---|---|
| Service class thuần (`AuthService : IAuthService`) | MediatR/CQRS | Giữ code tinh gọn, dễ đọc, không thừa tầng trung gian ở Phase 1 |
| Generic `IRepository<T>` + `IUnitOfWork` | `SocialDbContext` dùng thẳng trong Service | Hỗ trợ Transaction xuyên nhiều bảng (đổi mật khẩu, đăng ký) và tách rời EF Core khỏi Service |
| `PasswordHasher<TUser>` (PBKDF2) | ASP.NET Core Identity đầy đủ | Không kéo theo schema/roles/2FA thừa không cần thiết |
| Exception nghiệp vụ + `GlobalExceptionHandler` | `OperationResult<T>` wrapper | Exception map thẳng sang mã lỗi HTTP ProblemDetails (RFC 7807) |
| Cookie `httpOnly` cho Refresh Token | Lưu Refresh Token trong LocalStorage | Bảo vệ tuyệt đối khỏi tấn công XSS đánh cắp token |

---

## ▶️ Cách chạy Backend trên máy Dev

```powershell
cd Backend/API

# Cấu hình bí mật môi trường (User-Secrets)
dotnet user-secrets set "Jwt:SigningKey" "<chuỗi bí mật ≥ 32 ký tự>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection string Supabase social-dev>"
dotnet user-secrets set "Cloudinary:CloudName" "<cloud name>"
dotnet user-secrets set "Cloudinary:ApiKey" "<api key>"
dotnet user-secrets set "Cloudinary:ApiSecret" "<api secret>"
dotnet user-secrets set "Google:ClientId" "<google oauth client id>"
dotnet user-secrets set "Google:ClientSecret" "<google oauth client secret>"

# Chạy API
dotnet run
```

- **Swagger UI**: [http://localhost:5126/swagger](http://localhost:5126/swagger) (hoặc `https://localhost:7068/swagger`).
- **Mặc định Email Provider**: `Console` — nội dung email kèm token/link xác minh in trực tiếp ra Terminal, không cần cấu hình SMTP khi dev.
- **Trang test nhanh Google Login**: [http://localhost:5126/test-google](http://localhost:5126/test-google).

---

## 📦 Package đã cài đặt (theo project)

| Project | Package chính |
|---|---|
| Domain | — *(không phụ thuộc thư viện bên ngoài)* |
| Application | `AutoMapper`, `FluentValidation`, `FluentValidation.DependencyInjectionExtensions` |
| Infrastructure | `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `System.IdentityModel.Tokens.Jwt`, `Microsoft.Extensions.Identity.Core`, `MailKit`, `CloudinaryDotNet`, `Google.Apis.Auth`, `Microsoft.Extensions.Http` |
| API | `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.EntityFrameworkCore.Design`, `Swashbuckle.AspNetCore` |
