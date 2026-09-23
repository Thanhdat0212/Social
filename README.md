# Social — Mạng xã hội (Phase 1: Auth + Hồ sơ cá nhân)

Kế hoạch chi tiết (kiến trúc, quyết định kỹ thuật, 17 giai đoạn): [Social-Phase1-KeHoach.md](./Social-Phase1-KeHoach.md)

## 📊 Trạng thái hiện tại

Cập nhật: 2026-09-23

### Backend (GĐ 1–8)

| GĐ | Tên | Trạng thái |
|---|---|---|
| 1 | Repo & solution | ⚠️ Gần xong — 4 project build OK, nhưng chưa có `global.json` và vẫn `net8.0` (kế hoạch chốt `net10.0`) |
| 2 | Domain & DB | ✅ |
| 3 | Hasher, JWT, Swagger | ✅ |
| 4 | Đăng ký + xác minh + gửi lại | ✅ |
| 5 | Login / refresh / logout qua cookie | ✅ |
| 6 | Quên / đặt lại mật khẩu | ✅ |
| 7 | Hồ sơ + avatar | ✅ |
| 8 | Exception + pipeline hoàn chỉnh | ❌ Chưa làm |

### Frontend (GĐ 9–16) & Triển khai (GĐ 17)

Chưa bắt đầu (`FE/` đang trống, chưa có Dockerfile/vercel.json).

## 📦 Cấu trúc thư mục

```
Social/
├── Backend/
│   ├── Domain/                          # Core layer - không phụ thuộc project nào khác
│   │   ├── Common/
│   │   │   └── BaseEntity.cs            # Id, CreatedAtUtc, UpdatedAtUtc
│   │   ├── Entities/
│   │   │   ├── User.cs
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
│   │   │   ├── Auth/Requests/             # Register/Login/Refresh/Forgot/Reset...
│   │   │   ├── Auth/Responses/            # Login/Refresh/UserDto...
│   │   │   └── Profile/Requests|Responses/
│   │   ├── Interfaces/                   # IAuthService, IProfileService, IEmailSender, IJwtTokenService, IPasswordHasherService, IAvatarStorageService
│   │   │   └── Repositories/             # IUnitOfWork, IGenericRepository<T>, IUserRepository, IRefreshTokenRepository, IVerificationTokenRepository
│   │   ├── Settings/                     # AppSettings, JwtSettings, EmailSettings, SmtpSettings, BrevoSettings, CloudinarySettings
│   │   ├── Validators/                   # FluentValidation cho từng request
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
│   │   │   └── Repositories/             # GenericRepository<T>, UserRepository, RefreshTokenRepository, VerificationTokenRepository, UnitOfWork
│   │   ├── Security/                     # PasswordHasherService (PBKDF2), JwtTokenService, TokenHasher (SHA-256)
│   │   ├── Services/                     # AuthService, ProfileService — implementation thật, gọi qua IUnitOfWork
│   │   └── DependencyInjection.cs
│   │
│   ├── API/                             # Presentation layer
│   │   ├── Auth/RefreshTokenCookie.cs    # Helper Append/Get/Delete cookie `social_rt`
│   │   ├── Controllers/AuthController.cs
│   │   ├── Controllers/ProfileController.cs
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   └── Backend.slnx
│
├── FE/                                  # React + Vite + TypeScript (chưa khởi tạo)
├── Mẫu Backend/                          # Project tham khảo, không thuộc code Social (gitignore)
└── Social-Phase1-KeHoach.md              # Kế hoạch đầy đủ
```

## 🏗️ Kiến trúc Layers

**1. Domain** — Entity, Enum, Exception thuần. Không phụ thuộc project nào khác, không có logic hạ tầng hay framework.

**2. Application** — DTO, FluentValidation, và **interface** cho mọi nghiệp vụ/hạ tầng (`IAuthService`, `IProfileService`, `IEmailSender`, `IJwtTokenService`, `IUnitOfWork`...). Chỉ phụ thuộc `Domain`. Application **định nghĩa hợp đồng**, không biết `Infrastructure` implement thế nào.

**3. Infrastructure** — Implement toàn bộ interface của Application: EF Core qua Repository/`IUnitOfWork` (`SocialDbContext` không còn bị Service gọi trực tiếp), JWT, băm mật khẩu, gửi email, lưu trữ media (Cloudinary). Phụ thuộc `Application` + `Domain`.

**4. API** — Controllers, `Program.cs`, và helper cookie (`RefreshTokenCookie`) cô lập riêng ở đây vì đây là chi tiết HTTP, không thuộc về nghiệp vụ. Phụ thuộc cả 3 layer còn lại.

## 📋 Quy tắc Dependency

```
API ──► Infrastructure ──► Application ──► Domain
 │                              ▲
 └──────────────────────────────┘
   (Application không biết Infrastructure/API tồn tại)
```

## 🧩 Nguyên tắc thiết kế đã chọn (khác với một số template Clean Architecture khác)

| Có dùng | Không dùng | Lý do |
|---|---|---|
| Service class thuần (`AuthService : IAuthService`) | MediatR/CQRS | ~10 use case ở Phase 1, chưa cần thêm tầng trung gian |
| Generic `IRepository<T>` + `IUnitOfWork` (`Application/Interfaces/Repositories`) | `SocialDbContext` dùng thẳng trong Service | Từ GĐ6–7 trở đi cần transaction xuyên nhiều repository (vd: `ResetPasswordAsync` cập nhật `User` + `VerificationToken` + `RefreshToken` cùng lúc) — `IUnitOfWork.BeginTransactionAsync/CommitTransactionAsync` gói gọn việc đó, đồng thời Service không còn phụ thuộc trực tiếp EF Core |
| `PasswordHasher<TUser>` (PBKDF2) | ASP.NET Core Identity đầy đủ | Không kéo theo schema/roles/2FA thừa |
| Exception nghiệp vụ (`NotFoundException`, `ConflictException`, `ValidationAppException`) | `OperationResult<T>` wrapper | Exception + `GlobalExceptionHandler` (GĐ8) map thẳng sang mã lỗi HTTP, không cần bọc kết quả thủ công mỗi method |

Toàn bộ quyết định gốc: xem mục 3 trong [Social-Phase1-KeHoach.md](./Social-Phase1-KeHoach.md).

## 🚀 Cách thêm một tính năng mới

Ví dụ minh hoạ bằng tính năng **Hồ sơ (GĐ7 — đã làm)**, đúng theo mục 12.GĐ7 của kế hoạch:

1. **DTO** — `Application/DTOs/Profile/Responses/ProfileDto.cs`, `Requests/UpdateProfileRequestDto.cs`
2. **Validator** (nếu cần) — `Application/Validators/UpdateProfileRequestValidator.cs`
3. **Interface nghiệp vụ** — `Application/Interfaces/IProfileService.cs`, `IAvatarStorageService.cs`
4. **Implement** — `Infrastructure/Services/ProfileService.cs` (gọi qua `IUnitOfWork.Users`), `Infrastructure/Media/CloudinaryAvatarService.cs`
5. **Đăng ký DI** — thêm vào `Infrastructure/DependencyInjection.cs`:
   ```csharp
   services.AddScoped<IProfileService, ProfileService>();
   services.AddScoped<IAvatarStorageService, CloudinaryAvatarService>();
   ```
6. **Controller** — `API/Controllers/ProfileController.cs` với `[Authorize]`, gọi `IProfileService` qua constructor injection

Nếu entity mới cần truy vấn riêng, thêm interface con kế thừa `IGenericRepository<T>` (vd: `IUserRepository`) trong `Application/Interfaces/Repositories`, implement trong `Infrastructure/Persistence/Repositories`, rồi expose qua `IUnitOfWork`.

## 🔄 Luồng xử lý request

```
Client
  │
  ▼
API/Controllers            — validate FluentValidation, map lỗi → ProblemDetails (GĐ8)
  │
  ▼
Application/Interfaces      — hợp đồng (I*Service, IUnitOfWork, I*Repository)
  │
  ▼
Infrastructure/Services     — implementation, gọi qua IUnitOfWork.Users/RefreshTokens/VerificationTokens
  │
  ▼
Infrastructure/Persistence/Repositories ──► SocialDbContext ──► PostgreSQL (Supabase)
  │
  ▼
Domain/Entities
```

## 🗄️ Cấu hình Database

Chỉ dùng **PostgreSQL qua Supabase** (Npgsql) — không có fallback InMemory. Connection string lấy từ `ConnectionStrings:DefaultConnection` (dev: `dotnet user-secrets`, prod: biến môi trường `ConnectionStrings__DefaultConnection`).

## ✅ Tính năng đã có (GĐ1–7)

- Đăng ký, xác minh email qua link 24h, gửi lại xác minh với cooldown 60 giây
- Đăng nhập → access token (JWT 15 phút, trả trong body) + refresh token 7 ngày (cookie `httpOnly; Path=/api/auth`)
- Refresh token **rotation**: mỗi lần `/refresh` sẽ revoke token cũ và phát token mới (`ReplacedByTokenHash`)
- Đăng xuất: revoke token trong DB + xoá cookie
- Mật khẩu băm bằng `PasswordHasher<User>` (PBKDF2), token lưu DB chỉ ở dạng hash SHA-256
- Quên mật khẩu: gửi link reset hiệu lực 1 giờ, luôn trả 200 để không lộ email có tồn tại hay không
- Đặt lại mật khẩu: đổi mật khẩu trong 1 transaction (`IUnitOfWork`), đồng thời tiêu thụ toàn bộ token reset và revoke toàn bộ refresh token còn hiệu lực (đăng xuất mọi thiết bị)
- Hồ sơ cá nhân: xem/cập nhật tên hiển thị + tiểu sử (`GET/PUT /api/profile/me`)
- Avatar: upload lên Cloudinary (tối đa 5MB, JPG/PNG/WebP, auto-crop 500×500 theo khuôn mặt) qua `POST /api/profile/me/avatar`

**Chưa có:** `GlobalExceptionHandler` thống nhất — hiện mỗi action trong `AuthController`/`ProfileController` tự `catch` exception nghiệp vụ (GĐ8).

## ▶️ Cách chạy

```bash
cd Backend/API
dotnet user-secrets set "Jwt:SigningKey" "<chuỗi bí mật ≥ 32 ký tự>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection string social-dev>"
dotnet user-secrets set "Cloudinary:CloudName" "<cloud name>"
dotnet user-secrets set "Cloudinary:ApiKey" "<api key>"
dotnet user-secrets set "Cloudinary:ApiSecret" "<api secret>"
dotnet run
```

Swagger UI: `https://localhost:7068/swagger` (có nút Authorize để test endpoint `[Authorize]`).

Mặc định `Email:Provider = Console` trong `appsettings.json` — email xác minh/reset in ra console, không cần cấu hình SMTP khi dev. Thiếu cấu hình `Cloudinary:*` sẽ chỉ làm lỗi endpoint upload avatar (`POST /api/profile/me/avatar`), các API khác không bị ảnh hưởng.

## 📦 Package đã cài đặt (theo project)

| Project | Package chính |
|---|---|
| Domain | — (không có dependency ngoài) |
| Application | AutoMapper, FluentValidation, FluentValidation.DependencyInjectionExtensions |
| Infrastructure | Microsoft.EntityFrameworkCore + Npgsql.EntityFrameworkCore.PostgreSQL, Microsoft.AspNetCore.Authentication.JwtBearer, System.IdentityModel.Tokens.Jwt, Microsoft.Extensions.Identity.Core (chỉ dùng `PasswordHasher<TUser>`), MailKit, CloudinaryDotNet, Microsoft.Extensions.Http, Microsoft.Extensions.Configuration.UserSecrets |
| API | Microsoft.AspNetCore.Authentication.JwtBearer, Microsoft.EntityFrameworkCore.Design, Swashbuckle.AspNetCore |

## 🎯 Nguyên tắc

- **Dependency Rule** — layer trong không biết layer ngoài tồn tại
- **Interface Segregation** — Application định nghĩa interface, Infrastructure implement
- **Single Responsibility** — mỗi service/class một trách nhiệm (vd: `RefreshTokenCookie` chỉ lo cookie, không biết gì về JWT hay DB)
- **Separation of Concerns** — logic nghiệp vụ (Application/Infrastructure) tách biệt hoàn toàn khỏi chi tiết HTTP (API)

## Công nghệ

- **Backend:** ASP.NET Core Web API (.NET), Clean Architecture 4 project, EF Core + Npgsql
- **Database:** PostgreSQL trên Supabase (`social-dev` / `social-prod`)
- **Auth:** JWT access token (bộ nhớ FE) + refresh token cookie `httpOnly` (tự xây, không dùng Identity đầy đủ)
- **Email:** Console (dev) / Gmail SMTP (dev) / Brevo REST API (prod)
- **Media:** Cloudinary (avatar)
- **Frontend:** React + Vite + TypeScript, axios + react-router-dom
- **Hosting:** Render (API, Docker) · Vercel (FE) · Supabase (DB)
