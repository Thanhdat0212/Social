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
| 6 | Quên / đặt lại mật khẩu | ❌ Chưa làm |
| 7 | Hồ sơ + avatar | ❌ Chưa làm |
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
│   │   │   ├── Helpers/TokenHelper.cs
│   │   │   ├── ICurrentUserService.cs
│   │   │   ├── Interfaces/ISocialDbContext.cs   # stub, chưa dùng tới
│   │   │   └── Mappings/MappingProfile.cs       # AutoMapper: User -> UserDto
│   │   ├── DTOs/Auth/                    # Register/Login/Refresh request & response
│   │   ├── Interfaces/                   # IAuthService, IEmailSender, IJwtTokenService, IPasswordHasherService
│   │   ├── Options/                      # AppOptions, JwtOptions, EmailOptions, SmtpOptions, BrevoOptions
│   │   ├── Services/AuthService.cs       # stub, logic thật nằm ở Infrastructure/Services
│   │   ├── Validators/                   # FluentValidation cho từng request
│   │   └── DependencyInjection.cs
│   │
│   ├── Infrastructure/                  # Implement các interface của Application
│   │   ├── Email/                        # ConsoleEmailSender, SmtpEmailSender, BrevoEmailSender
│   │   ├── Identity/CurrentUserService.cs
│   │   ├── Persistence/
│   │   │   ├── SocialDbContext.cs
│   │   │   ├── SocialDbContextFactory.cs # design-time factory cho `dotnet ef migrations`
│   │   │   └── Configurations/           # Fluent API cho User/RefreshToken/VerificationToken
│   │   ├── Security/                     # PasswordHasherService (PBKDF2), JwtTokenService, TokenHasher (SHA-256)
│   │   ├── Services/AuthService.cs       # Implementation thật của IAuthService
│   │   └── DependencyInjection.cs
│   │
│   ├── API/                             # Presentation layer
│   │   ├── Auth/RefreshTokenCookie.cs    # Helper Append/Get/Delete cookie `social_rt`
│   │   ├── Controllers/AuthController.cs
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

**2. Application** — DTO, FluentValidation, và **interface** cho mọi nghiệp vụ/hạ tầng (`IAuthService`, `IEmailSender`, `IJwtTokenService`...). Chỉ phụ thuộc `Domain`. Application **định nghĩa hợp đồng**, không biết `Infrastructure` implement thế nào.

**3. Infrastructure** — Implement toàn bộ interface của Application: EF Core (`SocialDbContext` dùng **trực tiếp**, không qua Repository/UnitOfWork), JWT, băm mật khẩu, gửi email. Phụ thuộc `Application` + `Domain`.

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
| `SocialDbContext` dùng thẳng trong Service | Generic `IRepository<T>` / `UnitOfWork` | 1 DbContext, EF Core đã là Unit of Work sẵn — thêm Repository là bọc thêm 1 lớp không cần thiết ở quy mô này |
| `PasswordHasher<TUser>` (PBKDF2) | ASP.NET Core Identity đầy đủ | Không kéo theo schema/roles/2FA thừa |
| Exception nghiệp vụ (`NotFoundException`, `ConflictException`, `ValidationAppException`) | `OperationResult<T>` wrapper | Exception + `GlobalExceptionHandler` (GĐ8) map thẳng sang mã lỗi HTTP, không cần bọc kết quả thủ công mỗi method |

Toàn bộ quyết định gốc: xem mục 3 trong [Social-Phase1-KeHoach.md](./Social-Phase1-KeHoach.md).

## 🚀 Cách thêm một tính năng mới

Ví dụ minh hoạ bằng tính năng **Hồ sơ (GĐ7 — chưa làm)**, đúng theo mục 12.GĐ7 của kế hoạch:

1. **DTO** — `Application/DTOs/Profile/ProfileDto.cs`, `UpdateProfileRequest.cs`
2. **Validator** (nếu cần) — `Application/Validators/UpdateProfileRequestValidator.cs`
3. **Interface nghiệp vụ** — `Application/Interfaces/IProfileService.cs`, `IAvatarStorageService.cs`
4. **Implement** — `Infrastructure/Services/ProfileService.cs` (dùng thẳng `SocialDbContext`), `Infrastructure/Media/CloudinaryAvatarService.cs`
5. **Đăng ký DI** — thêm vào `Infrastructure/DependencyInjection.cs`:
   ```csharp
   services.AddScoped<IProfileService, ProfileService>();
   services.AddScoped<IAvatarStorageService, CloudinaryAvatarService>();
   ```
6. **Controller** — `API/Controllers/ProfileController.cs` với `[Authorize]`, gọi `IProfileService` qua constructor injection

Không có bước "tạo Repository" hay "đăng ký UnitOfWork" — Service gọi thẳng `DbContext.SaveChangesAsync()`.

## 🔄 Luồng xử lý request

```
Client
  │
  ▼
API/Controllers          — validate FluentValidation, map lỗi → ProblemDetails (GĐ8)
  │
  ▼
Application/Interfaces    — hợp đồng (I*Service)
  │
  ▼
Infrastructure/Services   — implementation, gọi thẳng SocialDbContext
  │
  ▼
Infrastructure/Persistence/SocialDbContext ──► PostgreSQL (Supabase)
  │
  ▼
Domain/Entities
```

## 🗄️ Cấu hình Database

Chỉ dùng **PostgreSQL qua Supabase** (Npgsql) — không có fallback InMemory. Connection string lấy từ `ConnectionStrings:DefaultConnection` (dev: `dotnet user-secrets`, prod: biến môi trường `ConnectionStrings__DefaultConnection`).

## ✅ Tính năng đã có (GĐ1–5)

- Đăng ký, xác minh email qua link 24h, gửi lại xác minh với cooldown 60 giây
- Đăng nhập → access token (JWT 15 phút, trả trong body) + refresh token 7 ngày (cookie `httpOnly; Path=/api/auth`)
- Refresh token **rotation**: mỗi lần `/refresh` sẽ revoke token cũ và phát token mới (`ReplacedByTokenHash`)
- Đăng xuất: revoke token trong DB + xoá cookie
- Mật khẩu băm bằng `PasswordHasher<User>` (PBKDF2), token lưu DB chỉ ở dạng hash SHA-256

**Chưa có:** quên/đặt lại mật khẩu (GĐ6), hồ sơ/avatar (GĐ7), `GlobalExceptionHandler` thống nhất — hiện mỗi action trong `AuthController` tự `catch` exception nghiệp vụ (GĐ8).

## ▶️ Cách chạy

```bash
cd Backend/API
dotnet user-secrets set "Jwt:SigningKey" "<chuỗi bí mật ≥ 32 ký tự>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection string social-dev>"
dotnet run
```

Swagger UI: `https://localhost:7068/swagger` (có nút Authorize để test endpoint `[Authorize]`).

Mặc định `Email:Provider = Console` trong `appsettings.json` — email xác minh/reset in ra console, không cần cấu hình SMTP khi dev.

## 📦 Package đã cài đặt (theo project)

| Project | Package chính |
|---|---|
| Domain | — (không có dependency ngoài) |
| Application | AutoMapper, FluentValidation, FluentValidation.DependencyInjectionExtensions |
| Infrastructure | Microsoft.EntityFrameworkCore + Npgsql.EntityFrameworkCore.PostgreSQL, Microsoft.AspNetCore.Authentication.JwtBearer, System.IdentityModel.Tokens.Jwt, Microsoft.Extensions.Identity.Core (chỉ dùng `PasswordHasher<TUser>`), MailKit, Microsoft.Extensions.Http, Microsoft.Extensions.Configuration.UserSecrets |
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
