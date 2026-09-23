# Social — Kế hoạch Phase 1 (MVP): Auth + Hồ sơ cá nhân

> Bản tổ chức lại từ file kế hoạch gốc, \\\*\\\*đã cập nhật theo các quyết định mới\\\*\\\* (xem mục 0).

\---

## 0\. Nhật ký thay đổi so với kế hoạch gốc

|#|Thay đổi|Lý do|Giai đoạn bị ảnh hưởng|
|-|-|-|-|
|C1|Backend deploy lên Render **bằng Docker** (gói free)|Render không có runtime .NET dựng sẵn|17|
|C2|Email production gửi qua **Brevo REST API** (HTTPS); dev vẫn dùng console/Gmail SMTP|Render free chặn cổng SMTP 25/465/587|4, 17|
|C3|**Refresh token lưu trong cookie `httpOnly`**, access token chỉ nằm trong bộ nhớ|JavaScript không đọc được refresh token → giảm rủi ro XSS|5, 10, 12, 13, 16|
|C4|**Vercel rewrite `/api/\\\*` → Render** (dev dùng Vite proxy) → FE và API cùng origin|Cookie trở thành cookie cùng site, không bị trình duyệt chặn; gần như bỏ được CORS|8, 9, 17|
|C5|Thêm endpoint **`POST /api/auth/resend-confirmation`** → đủ 11 endpoint|Tránh user kẹt khi link xác minh hết hạn|4, 11|
|C6|Link trong email trỏ về **trang frontend** (`App:FrontendBaseUrl`), FE gọi API|Khớp với `VerifyEmailPage`/`ResetPasswordPage` đã có trong kế hoạch|4, 6|
|C7|**Đặt lại mật khẩu → revoke toàn bộ refresh token** của user (đăng xuất mọi thiết bị)|Nếu phiên bị chiếm, đổi mật khẩu phải cắt được kẻ xấu|6, 14|
|C8|**Tách 2 project Supabase**: `social-dev` và `social-prod`|Dữ liệu test không lẫn dữ liệu thật; migration thử nghiệm không đụng production|2, 17|
|C9|**Reuse detection** cho refresh token → dời sang Phase 2|Giữ Phase 1 gọn; rotation cơ bản vẫn làm ở GĐ 5|—|
|C10|**Không viết test tự động / CI ở Phase 1** — kiểm thử thủ công theo milestone|Tập trung hoàn thành luồng; bổ sung ở Phase 2|Toàn bộ|
|C11|`resend-confirmation` có **cooldown 60 giây** (BE chặn, FE đếm ngược)|Tránh spam email và tốn hạn mức Brevo|4, 11|

\---

## 1\. Tóm tắt nhanh

|Mục|Nội dung|
|-|-|
|Sản phẩm|Mạng xã hội **Social**|
|Phạm vi Phase 1|Đăng ký, xác minh email (có gửi lại), đăng nhập, refresh/đăng xuất, quên/đặt lại mật khẩu, xem/sửa hồ sơ, upload avatar|
|Ngoài phạm vi|Post, feed, follow, nhắn tin, rate limiting/lockout, reuse detection refresh token|
|Backend|ASP.NET Core Web API (.NET 10), Clean Architecture 4 project, Controllers|
|Database|PostgreSQL trên **Supabase** (chỉ dùng DB), **2 project riêng: dev và prod**, EF Core + Npgsql|
|Auth|JWT access token 15' (trong bộ nhớ FE) + refresh token 7 ngày (cookie `httpOnly`, xoay vòng), tự xây|
|Email|Dev: console / Gmail SMTP · Prod: **Brevo REST API**|
|Media|Cloudinary (avatar)|
|Frontend|React + Vite + TypeScript, CSS thuần, axios + react-router-dom|
|Hosting|Render (API, **Docker**, free) · Vercel (FE + **proxy `/api`**) · Supabase (DB, `social-dev` + `social-prod`)|
|Máy dev|Windows, .NET SDK 10.0.302, Node 24.15.0 / npm 11.12.1|
|Nguyên tắc|17 giai đoạn, mỗi giai đoạn có **milestone kiểm chứng được**|

\---

## 2\. Kiến trúc tổng thể

```
Người dùng (trình duyệt) — chỉ thấy 1 origin: https://social.vercel.app
   │
   ├─ GET /, /login, /verify-email ...  ──► Vercel: trả React build (SPA fallback → index.html)
   │
   └─ /api/\\\*  ──► Vercel rewrite (proxy) ──► Render: Social.Api (Docker)
                                               │  ForwardedHeaders → ExceptionHandler → (Swagger dev)
                                               │  → HTTPS → Authentication → Authorization → Controllers
                                               ├──► Supabase Postgres (5432, SSL)
                                               ├──► Cloudinary (avatar)
                                               └──► Brevo REST API (email, HTTPS 443)
```

Môi trường dev mô phỏng y hệt: `http://localhost:5173/api/\\\*` → Vite proxy → `http://localhost:<port>/api/\\\*`.

**Phụ thuộc giữa các project backend**

```
Social.Api ──► Social.Application ──► Social.Domain
     │                 ▲
     └──► Social.Infrastructure ──┘ (+ Domain)
```

|Project|Vai trò|Package|
|-|-|-|
|Social.Domain|Entity POCO, enum — không phụ thuộc gì|—|
|Social.Application|DTO, validator, `IAuthService`, `IProfileService`, interface hạ tầng (`IEmailSender`...)|FluentValidation|
|Social.Infrastructure|DbContext, EF config, JWT, hasher, email (Console/SMTP/Brevo), Cloudinary|EF Core, Npgsql, EF Tools, Identity.Core, IdentityModel.Tokens.Jwt, Options.ConfigurationExtensions, Http.Abstractions, **Microsoft.Extensions.Http**, MailKit, CloudinaryDotNet|
|Social.Api|Controllers, `Program.cs`, middleware, cookie refresh token|EF Core Design, JwtBearer, Swashbuckle|

\---

## 3\. Các quyết định kỹ thuật đã chốt

|#|Vấn đề|Quyết định|Lý do|
|-|-|-|-|
|1|Token xác minh email / reset|1 bảng `VerificationToken` có cột `Purpose`|Không rải cột nullable trên `User`; dùng chung cơ chế hết hạn/tiêu thụ|
|2|Application layer|Service class thuần, **không MediatR/CQRS**|\~10 use case, chưa cần; xem lại ở Phase 2|
|3|Băm mật khẩu|`PasswordHasher<TUser>` (PBKDF2), không dùng full Identity|Không kéo theo schema/roles/2FA thừa|
|4|Ngôn ngữ FE|TypeScript|An toàn kiểu cho DTO/form|
|5|Lưu token phía client *(đổi — C3)*|Access token: JSON body → **bộ nhớ** (`AuthContext`). Refresh token: **cookie `httpOnly; Secure; SameSite=Strict; Path=/api/auth`**, hạn 7 ngày|JS không đọc được refresh token; reload trang thì gọi `/refresh` im lặng để khôi phục phiên|
|6|Chưa xác minh email|Chặn đăng nhập hoàn toàn: `403 EMAIL\\\_NOT\\\_CONFIRMED`|Rõ ràng; có endpoint gửi lại email để không bị kẹt|
|7|Lưu token trong DB|Chỉ lưu **hash SHA-256**|Lộ DB cũng không dùng lại được token|
|8|Kiểu API|Controllers `\\\[ApiController]`|Tiện cho multipart, `\\\[Authorize]`, Swagger|
|9|Gửi email *(đổi — C2)*|`IEmailSender` có 3 implementation: `ConsoleEmailSender`, `SmtpEmailSender` (MailKit), `BrevoEmailSender` (HttpClient); chọn qua `Email:Provider`|Dev test không cần secret; prod đi HTTPS nên không bị Render chặn|
|10|DB hosting|Supabase Postgres + Npgsql|Render không có SQL Server; free tier đủ MVP|
|11|Supabase Auth/Storage|**Không dùng**|Giữ JWT tự xây và Cloudinary|
|12|Deploy backend *(mới — C1)*|Docker trên Render free, `Dockerfile` multi-stage (`sdk:10.0` build → `aspnet:10.0` chạy), lắng nghe cổng từ biến `PORT`|Render không hỗ trợ .NET native|
|13|Kết nối FE ⇄ API *(đổi — C4)*|Cùng origin qua proxy: Vercel rewrite (prod), Vite `server.proxy` (dev); axios `baseURL = "/api"`|Cookie cùng site; không cần CORS; không lộ URL Render ra FE|
|14|Link trong email *(mới — C6)*|Dựng từ `App:FrontendBaseUrl`: `/verify-email?userId=\\\&token=`, `/reset-password?userId=\\\&token=`|FE hiển thị kết quả, FE gọi API|
|15|Hệ quả của reset mật khẩu *(mới — C7)*|Trong cùng 1 transaction: đổi `PasswordHash`, tiêu thụ token reset đang dùng **và mọi token `PasswordReset` còn hiệu lực khác**, revoke **mọi** refresh token chưa revoke của user|Đăng xuất mọi thiết bị; link reset cũ không dùng lại được|
|16|Database theo môi trường *(mới — C8)*|`social-dev` (máy dev, user-secrets) và `social-prod` (Render). Migration lên prod bằng **script SQL idempotent**|Tách biệt dữ liệu; kiểm soát được thay đổi schema trên prod|
|17|Reuse detection *(C9)*|**Chưa làm ở Phase 1**; giữ sẵn cột `ReplacedByTokenHash` để Phase 2 lần theo chuỗi token|Không phải sửa schema khi bổ sung|
|18|Kiểm thử *(C10)*|Thủ công theo milestone từng giai đoạn (Swagger cho backend, trình duyệt cho frontend)|Phase 1 nhỏ, milestone đã mô tả rõ kịch bản|

\---

## 4\. Mô hình dữ liệu

|Bảng|Cột|Ghi chú|
|-|-|-|
|**Users**|`Id`, `Email`, `NormalizedEmail`, `PasswordHash`, `DisplayName`, `Bio?`, `AvatarUrl?`, `AvatarPublicId?`, `EmailConfirmed`, `CreatedAtUtc`, `UpdatedAtUtc`|Unique index trên `NormalizedEmail`|
|**RefreshTokens**|`Id`, `UserId` (FK), `TokenHash`, `ExpiresAtUtc`, `CreatedAtUtc`, `CreatedByIp?`, `RevokedAtUtc?`, `ReplacedByTokenHash?`|Unique `TokenHash`; hỗ trợ xoay vòng|
|**VerificationTokens**|`Id`, `UserId` (FK), `TokenHash`, `Purpose`, `ExpiresAtUtc`, `CreatedAtUtc`, `ConsumedAtUtc?`|`Purpose` = `EmailConfirmation` (24h) / `PasswordReset` (1h)|

Không cần thêm bảng/cột cho endpoint gửi lại email — dùng lại `VerificationTokens`.

\---

## 5\. Danh sách API (11 endpoint)

|#|Method|Endpoint|Auth|Request → Response|
|-|-|-|-|-|
|1|POST|`/api/auth/register`|—|Body đăng ký → 201; gửi email xác minh|
|2|GET|`/api/auth/confirm-email?userId=\\\&token=`|—|→ 200 / 400 (token sai, hết hạn, đã dùng)|
|3|POST|`/api/auth/resend-confirmation` *(mới)*|—|`{ email }` → **luôn 200**|
|4|POST|`/api/auth/login`|—|`{ email, password }` → body `{ accessToken, expiresAt, user }` + **Set-Cookie refresh**; 403 nếu chưa xác minh|
|5|POST|`/api/auth/refresh`|Cookie|Không có body → body access token mới + **Set-Cookie refresh mới**|
|6|POST|`/api/auth/logout`|Cookie|Revoke token trong cookie + **xoá cookie** → 204|
|7|POST|`/api/auth/forgot-password`|—|`{ email }` → luôn 200; token reset 1h|
|8|POST|`/api/auth/reset-password`|—|`{ userId, token, newPassword, confirmPassword }` → 200|
|9|GET|`/api/profile/me`|Bearer|Hồ sơ|
|10|PUT|`/api/profile/me`|Bearer|`{ displayName, bio }`|
|11|POST|`/api/profile/me/avatar`|Bearer|Multipart, ≤5MB, jpg/png/webp → `{ avatarUrl }`|

**Quy ước lỗi (GlobalExceptionHandler → ProblemDetails):** `ValidationAppException → 400`, `NotFoundException → 404`, `ConflictException → 409`, khác → 500; login chưa xác minh → `403 EMAIL\\\_NOT\\\_CONFIRMED`.

**Quy tắc validate đăng ký:** email hợp lệ; mật khẩu ≥ 8 ký tự, có hoa, thường, số; confirm password khớp.

**Cookie refresh token:** `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api/auth`, `Expires` = hạn refresh token. Tên đề xuất: `social\\\_rt`. (Chrome/Edge coi `http://localhost` là ngữ cảnh an toàn nên cookie `Secure` vẫn chạy được lúc dev.)

\---

## 6\. Các luồng nghiệp vụ chính

**Đăng ký \& xác minh**

1. FE gửi form → `register` → tạo `User` (`EmailConfirmed=false`) + `VerificationToken` (24h) → email chứa link `{FrontendBaseUrl}/verify-email?userId=\\\&token=`.
2. User mở link → `VerifyEmailPage` gọi `confirm-email` khi mount → BE kiểm tra hash + hạn + chưa dùng → `EmailConfirmed=true`, set `ConsumedAtUtc`.

**Gửi lại email xác minh (mới)**

1. User bấm “Gửi lại email” (ở trang báo đăng ký thành công, trang xác minh thất bại, hoặc khi login bị 403) → `resend-confirmation { email }`.
2. BE **luôn trả 200** (không tiết lộ email có tồn tại/đã xác minh hay chưa). Chỉ thực sự gửi khi user tồn tại và `EmailConfirmed = false`.
3. Trước khi tạo token mới: đánh dấu mọi token `EmailConfirmation` còn hiệu lực của user là đã dùng (`ConsumedAtUtc = now`) → chỉ link mới nhất có hiệu lực.
4. Cooldown 60 giây (đã chốt): nếu token `EmailConfirmation` gần nhất của user tạo < 60 giây trước thì không gửi, vẫn trả 200. FE khoá nút “Gửi lại” và hiện đếm ngược 60 giây sau mỗi lần bấm.

**Đăng nhập, refresh, khôi phục phiên**

1. `login` → hợp lệ: trả access token trong body (FE giữ trong bộ nhớ) + set cookie refresh.
2. Access hết hạn → 401 → interceptor gọi `refresh` **một lần duy nhất** (promise dùng chung); trình duyệt tự gửi cookie.
3. BE revoke token cũ (`RevokedAtUtc`, `ReplacedByTokenHash`), phát cặp mới, ghi đè cookie → FE gọi lại request gốc.
4. **Reload trang / mở tab mới:** access token trong bộ nhớ mất → `AuthProvider` gọi `refresh` im lặng khi khởi động; thành công thì vào thẳng, thất bại thì coi như chưa đăng nhập. `ProtectedRoute` chờ bước này xong mới quyết định redirect.
5. Refresh thất bại → xoá session → `/login`.

**Đăng xuất:** `logout` → BE revoke token trong cookie và xoá cookie → FE xoá access token trong bộ nhớ → `/login`.

**Quên/đặt lại mật khẩu**

1. `forgot-password` → luôn 200 → token reset 1h → link `{FrontendBaseUrl}/reset-password?userId=\\\&token=`.
2. `reset-password` → kiểm tra token → trong 1 transaction:

   * đổi `PasswordHash`;
   * set `ConsumedAtUtc` cho token đang dùng và mọi token `PasswordReset` còn hiệu lực khác;
   * set `RevokedAtUtc` cho **mọi** refresh token chưa revoke của user → đăng xuất mọi thiết bị.
3. FE chuyển về `/login` với thông báo “Đổi mật khẩu thành công, vui lòng đăng nhập lại”.

> Giới hạn đã biết: access token đã phát trước đó vẫn dùng được tới khi hết hạn (tối đa 15 phút). Chấp nhận được ở MVP; muốn cắt ngay lập tức cần thêm cơ chế kiểu `SecurityStamp` (Phase sau).

**Hồ sơ:** avatar lên Cloudinary với `PublicId = avatars/{userId}`, `Overwrite = true`; lưu URL **có version** Cloudinary trả về để trình duyệt không hiển thị ảnh cũ.

\---

## 7\. Lộ trình 17 giai đoạn

### 7.1 Backend (GĐ 1–8)

|GĐ|Tên|Việc chính|Milestone|
|-|-|-|-|
|1|Repo \& solution|`git init`, `.gitignore`, README; 4 project + reference; `dotnet new webapi --use-controllers --no-openapi`; `global.json` pin 10.0.302; xoá code mẫu|`dotnet build` thành công|
|2|Domain \& DB|3 entity, `SocialDbContext`, cấu hình EF; tạo project **`social-dev`** trên Supabase, lưu connection string vào user-secrets; migration đầu tiên lên `social-dev` (project prod để tới GĐ 17)|3 bảng hiện trong Table Editor của `social-dev`|
|3|Hasher, JWT, Swagger|`PasswordHasherService`, `JwtTokenService`, `JwtOptions`, `CurrentUserService`, JWT Bearer, Swagger có nút Authorize|Swagger UI chạy, có nút Authorize|
|4|Đăng ký + xác minh + **gửi lại**|DTO, FluentValidation, `RegisterAsync`, `ConfirmEmailAsync`, **`ResendConfirmationAsync`**; `IEmailSender` + **3 implementation** (Console, SMTP, Brevo) chọn qua `Email:Provider`; dựng link từ `App:FrontendBaseUrl`|Đăng ký → lấy link → confirm → `EmailConfirmed=true`. Gửi lại → link cũ bị từ chối, link mới chạy. Gửi lại 2 lần liên tiếp trong 60 giây → chỉ tạo 1 token mới. Gửi thử 1 email thật qua Brevo từ máy dev|
|5|Login/refresh/logout **qua cookie**|`LoginAsync`, `RefreshAsync` (rotation), `LogoutAsync`; controller set/đọc/xoá cookie `social\\\_rt` (tách logic cookie ra helper ở tầng Api, service chỉ làm việc với chuỗi token)|Login thấy `Set-Cookie` với cờ `HttpOnly`; refresh không cần body; token cũ bị revoke; sau logout cookie bị xoá và token không dùng lại được|
|6|Quên/đặt lại MK|`ForgotPasswordAsync`, `ResetPasswordAsync` (kèm revoke toàn bộ refresh token, trong 1 transaction)|MK cũ hỏng, MK mới chạy; token hết hạn/dùng lại bị từ chối. Đăng nhập ở 2 nơi (vd Swagger + trình duyệt ẩn danh) → reset → cả 2 phiên gọi `refresh` đều thất bại, DB thấy mọi refresh token đã có `RevokedAtUtc`|
|7|Hồ sơ + avatar|`ProfileService`, `CloudinaryAvatarService`, 3 endpoint `\\\[Authorize]`|GET/PUT profile ổn; ảnh hiện trên Cloudinary|
|8|Exception + pipeline|`GlobalExceptionHandler`; dựng `Program.cs` **đúng thứ tự cuối cùng** (mục 7.3) ngay từ đây, gồm cả `UseForwardedHeaders`; CORS **không bắt buộc** (cùng origin) — giữ `Cors:AllowedOrigins` rỗng mặc định, chỉ bật khi cần|Toàn bộ 11 endpoint test qua Swagger; lỗi trả về dạng ProblemDetails|

> Lưu ý test cookie trên Swagger: Swagger UI chạy cùng origin với API nên trình duyệt tự lưu và gửi cookie — test được `refresh`/`logout` mà không cần công cụ khác.

### 7.2 Frontend (GĐ 9–16)

|GĐ|Tên|Việc chính|Milestone|
|-|-|-|-|
|9|Khởi tạo|`npm create vite` (react-ts), cài router + axios, dựng thư mục; **`vite.config.ts` thêm `server.proxy: { '/api': 'http://localhost:<port>' }`**; axios `baseURL = '/api'`|`npm run dev` chạy; gọi `/api/...` từ trình duyệt tới được backend qua proxy|
|10|Auth plumbing|`axiosClient` (gắn header Bearer từ bộ nhớ); `AuthContext` giữ access token + user **trong state, không lưu storage**; khi khởi động gọi `refresh` im lặng, có cờ `isInitializing`; `ProtectedRoute` chờ khởi tạo xong mới redirect|Reload trang khi đã đăng nhập vẫn giữ phiên; chưa đăng nhập thì về `/login`|
|11|Đăng ký + xác minh + **gửi lại**|`RegisterPage`, `VerifyEmailPage`; nút **“Gửi lại email xác minh”** (khoá + đếm ngược 60 giây sau khi bấm) ở trang đăng ký thành công và trang xác minh thất bại|Đăng ký → mở link → báo thành công; link hết hạn → bấm gửi lại → link mới chạy|
|12|Đăng nhập|`LoginPage` → lưu access token vào `AuthContext` → `/profile`; nếu 403 `EMAIL\\\_NOT\\\_CONFIRMED` thì hiện thông báo + nút gửi lại|Đăng nhập thật thành công; tab Application thấy cookie `social\\\_rt` (JS không đọc được)|
|13|Refresh interceptor|Bắt 401 (bỏ qua `/login`, `/refresh`, request đã retry), promise refresh dùng chung, `refresh` không gửi body, retry request gốc, fail → `/login`|Đặt access 1 phút → thấy refresh tự động trong tab Network|
|14|Quên/đặt lại MK|`ForgotPasswordPage`, `ResetPasswordPage`; reset xong xoá state đăng nhập hiện tại (nếu có) và chuyển về `/login` kèm thông báo|Quên → reset → đăng nhập bằng MK mới; tab khác đang đăng nhập bị đẩy về `/login` ở lần refresh kế tiếp|
|15|Hồ sơ|`ProfilePage`: sửa DisplayName/Bio, upload avatar có preview|Reload vẫn giữ; avatar cập nhật ngay|
|16|Đăng xuất \& hoàn thiện|Logout gọi API (BE xoá cookie) + xoá state; thông báo lỗi rõ; loading/disable nút; trả access về 15'|Toàn bộ luồng chạy mượt; sau logout, reload trang không tự đăng nhập lại|

Cấu trúc `frontend/src`: `main.tsx`, `App.tsx`, `api/` (axiosClient, authApi, profileApi), `context/AuthContext.tsx`, `components/` (ProtectedRoute, Layout), `pages/` (Register, VerifyEmail, Login, ForgotPassword, ResetPassword, Profile), `types/` (auth, profile), `styles/App.css`.

### 7.3 Triển khai (GĐ 17)

**Thứ tự middleware trong `Program.cs`**

|#|Middleware|Ghi chú|
|-|-|-|
|1|`UseForwardedHeaders()`|Request đi qua **2 proxy** (Vercel → Render). Cấu hình `XForwardedFor|
|2|`UseExceptionHandler()`|Bọc toàn bộ phía sau|
|3|`UseSwagger()/UseSwaggerUI()`|Chỉ ở Development|
|4|`UseHttpsRedirection()`||
|5|`UseCors()`|Chỉ khi `Cors:AllowedOrigins` có giá trị|
|6|`UseAuthentication()`|Đọc JWT từ header|
|7|`UseAuthorization()`|Kiểm tra `\\\[Authorize]`|
|8|`MapControllers()`||

**File `frontend/vercel.json`** (thứ tự rule quan trọng: `/api` trước, SPA fallback sau)

```json
{
  "rewrites": \\\[
    { "source": "/api/:path\\\*", "destination": "https://social-api.onrender.com/api/:path\\\*" },
    { "source": "/(.\\\*)", "destination": "/index.html" }
  ]
}
```

Rule thứ 2 cũng giải quyết lỗi 404 khi mở thẳng link `/verify-email?...` từ email.

**`backend/Dockerfile`** (khung)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Social.Api/Social.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE\\\_HTTP\\\_PORTS=10000
ENTRYPOINT \\\["dotnet", "Social.Api.dll"]
```

> Cổng phải khớp biến `PORT` mà Render dùng (mặc định 10000) — hoặc đặt `ASPNETCORE\\\_HTTP\\\_PORTS` theo đúng giá trị `PORT` trong Render Dashboard. Thêm `.dockerignore` loại `bin/`, `obj/`.

**Checklist deploy (theo thứ tự)**

|Bước|Nơi|Việc|
|-|-|-|
|1|Supabase|Tạo project **`social-prod`** (mật khẩu DB khác dev), lấy connection string. Tạo script: `dotnet ef migrations script --idempotent -o migrate.sql` (project Infrastructure, startup Api) → xem lại → chạy trong **SQL Editor** của `social-prod`|
|2|Brevo|Tạo tài khoản, xác minh địa chỉ gửi (sender), tạo API key|
|3|Render|New Web Service → runtime **Docker**, Root Directory `backend`, Dockerfile path `./Dockerfile`; khai báo biến môi trường; ghi lại URL `https://social-api.onrender.com`|
|4|Vercel|Root Directory `frontend`, preset Vite; sửa `vercel.json` trỏ đúng URL Render; deploy|
|5|Render|Đặt `App\\\_\\\_FrontendBaseUrl` = domain Vercel thật → redeploy|

**Biến môi trường Render**

|Biến|Giá trị|
|-|-|
|`ASPNETCORE\\\_ENVIRONMENT`|`Production`|
|`ConnectionStrings\\\_\\\_DefaultConnection`|Connection string của **`social-prod`**|
|`Jwt\\\_\\\_SigningKey`|Chuỗi bí mật ≥ 32 ký tự|
|`Email\\\_\\\_Provider`|`Brevo`|
|`Email\\\_\\\_FromAddress`, `Email\\\_\\\_FromName`|Địa chỉ đã xác minh trên Brevo, `Social`|
|`Brevo\\\_\\\_ApiKey`|API key Brevo|
|`App\\\_\\\_FrontendBaseUrl`|`https://social.vercel.app`|
|`Cloudinary\\\_\\\_CloudName`, `Cloudinary\\\_\\\_ApiKey`, `Cloudinary\\\_\\\_ApiSecret`|Từ Cloudinary dashboard|

Không cần `Cors\\\_\\\_AllowedOrigins` và `Smtp\\\_\\\_AppPassword` trên production. Phía Vercel **không cần biến môi trường** nào (URL Render nằm trong `vercel.json`).

**Milestone:** mở `https://social.vercel.app` → đăng ký → nhận email thật qua Brevo → xác minh → đăng nhập → reload vẫn giữ phiên → sửa hồ sơ/avatar → đăng xuất. Tab Network chỉ thấy request tới `social.vercel.app`, không thấy domain Render.

\---

## 8\. Cấu hình \& bí mật

|Môi trường|Cách lưu|
|-|-|
|Backend local|`appsettings.Development.json` (gitignore) + file `.example` commit; secret (gồm connection string **`social-dev`**) qua `dotnet user-secrets`; `Email:Provider = Console` (hoặc `Smtp`/`Brevo` khi muốn gửi thật); `App:FrontendBaseUrl = http://localhost:5173`|
|Backend Render|Biến môi trường dạng `Section\\\_\\\_Key` (mục 7.3)|
|Frontend|Không có secret, không cần file `.env`: dev dùng Vite proxy, prod dùng `vercel.json`|

`.gitignore`: `backend/\\\*\\\*/bin/`, `backend/\\\*\\\*/obj/`, `\\\*\\\*/appsettings.Development.json`, `frontend/node\\\_modules/`, `frontend/dist/`, `.vs/`, `\\\*.user`

\---

## 9\. ⚠️ Phân tích \& rủi ro

### Đã xử lý nhờ các quyết định mới

|Rủi ro cũ|Cách xử lý|
|-|-|
|Render không chạy .NET native|✅ Docker (C1)|
|Render free chặn SMTP|✅ Brevo REST API (C2)|
|Link sâu 404 trên Vercel|✅ SPA fallback trong `vercel.json`|
|User kẹt khi link xác minh hết hạn|✅ `resend-confirmation` (C5)|
|Link email trỏ về đâu|✅ `App:FrontendBaseUrl` (C6)|
|Refresh token lộ qua XSS|✅ Cookie `httpOnly` (C3)|
|Tên CORS policy không nhất quán|✅ Không còn cần CORS; cấu hình tuỳ chọn|
|Pipeline GĐ 8 khác GĐ 17|✅ Dựng đúng thứ tự cuối cùng từ GĐ 8|
|Dùng chung DB dev/prod|✅ Tách `social-dev` / `social-prod` (C8)|
|Phiên bị chiếm vẫn sống sau khi đổi MK|✅ Revoke toàn bộ refresh token khi reset (C7)|

### Còn lại cần theo dõi

1. **Cold start qua proxy Vercel.** Render free ngủ sau \~15 phút; request đầu tiên (Docker khởi động) có thể mất vài chục giây trở lên. Cần kiểm tra thực tế xem proxy của Vercel có timeout trước khi Render dậy không. Phương án dự phòng: FE hiện thông báo “đang khởi động máy chủ” và tự thử lại.
2. **Supabase Direct connection chỉ có IPv6.** Nếu Render hoặc mạng dev không kết nối được, đổi sang **Session pooler** (host `...pooler.supabase.com`, port 5432).
3. **Email từ địa chỉ @gmail.com qua Brevo dễ vào Spam.** Chấp nhận ở MVP; có tên miền riêng thì cấu hình SPF/DKIM.
4. **Brevo free giới hạn 300 email/ngày** — đủ cho MVP, cần để ý nếu test tự động gửi mail hàng loạt.
5. **Giới hạn số project Supabase free.** Tài khoản free chỉ cho một số ít project đang hoạt động (hiện là 2) — 2 project dev/prod dùng hết suất; kiểm tra lại trên trang giá Supabase.
6. **Supabase free tạm dừng project khi không hoạt động** một thời gian — `social-prod` ít truy cập sẽ dễ bị dừng.
7. **Migration prod là bước thủ công** — quên chạy script sau khi thêm migration thì API mới lỗi ngay khi deploy. Ghi vào checklist mỗi lần release.
8. **Access token cũ còn sống tối đa 15 phút sau khi reset mật khẩu** (xem mục 6).
9. **Chưa có reuse detection** — token bị đánh cắp và dùng trước chủ thật thì vẫn xoay vòng được (Phase 2).
10. **Không có test tự động (C10)** — sửa code ở giai đoạn sau có thể làm hỏng luồng cũ mà không biết. Cách giảm rủi ro: trước mỗi lần deploy, chạy lại nhanh các milestone GĐ 4–7 trên Swagger như một checklist hồi quy.

\---

## 10\. ✅ Câu hỏi mở

Đã chốt toàn bộ (Q1–Q11, xem mục 0). Không còn câu hỏi mở cho Phase 1.

\---

## 11\. Để dành cho Phase 2

* Reuse detection cho refresh token (lần theo `ReplacedByTokenHash`, thu hồi cả chuỗi)
* `SecurityStamp` để vô hiệu hoá access token ngay khi đổi mật khẩu
* Rate limiting/lockout cho `/login`, `/forgot-password`, `/resend-confirmation`
* Unit test cho service + integration test API, GitHub Actions build/test
* Post, feed, follow, nhắn tin



## 12. Cấu trúc thư mục backend theo từng giai đoạn

Bảng dưới ánh xạ cấu trúc 4 project (mục 2) vào đúng 8 giai đoạn backend của mục 7.1 — mỗi giai đoạn chỉ tạo những file cần cho milestone của chính nó, không tạo trước file của giai đoạn sau. GĐ 9–16 (frontend) không phát sinh thêm thư mục backend — xem `frontend/src` ở mục 7.2. GĐ 17 chỉ thêm file hạ tầng triển khai (Dockerfile, `.dockerignore`, `vercel.json`, `migrate.sql`) đã liệt kê ở mục 7.3, không đổi cấu trúc 4 project.

### GĐ 1 — Repo & solution

Chưa có file nghiệp vụ nào, chỉ chuẩn hoá khung 4 project:

| Việc | Tác dụng |
|---|---|
| `global.json` (pin `10.0.302`) | Ghim đúng SDK cho cả team và môi trường CI/deploy, tránh lệch version .NET 10 |
| Đổi `TargetFramework` → `net10.0` ở cả 4 `.csproj` | Khớp quyết định dùng .NET 10 (mục 1) |
| Thêm `<ProjectReference>` đúng chiều mục 2 | `Application`→`Domain`; `Infrastructure`→`Application`+`Domain`; `Api`→cả 3 — ép buộc dependency rule ngay từ đầu, sai chiều sẽ không build được |

### GĐ 2 — Domain & DB

| File / Thư mục | Tác dụng |
|---|---|
| `Domain/Common/BaseEntity.cs` | Base class chung: `Id` (Guid), `CreatedAtUtc`, `UpdatedAtUtc` — 3 entity kế thừa để không lặp code |
| `Domain/Entities/User.cs` | Entity ánh xạ bảng `Users` (mục 4) |
| `Domain/Entities/RefreshToken.cs` | Entity ánh xạ bảng `RefreshTokens`, có sẵn cột `ReplacedByTokenHash` để dành cho reuse detection Phase 2 (quyết định #17) |
| `Domain/Entities/VerificationToken.cs` | Entity dùng chung cho cả email-confirmation lẫn password-reset (quyết định #1) |
| `Domain/Enums/VerificationPurpose.cs` | Enum `EmailConfirmation` / `PasswordReset` cho cột `Purpose` |
| `Infrastructure/Persistence/SocialDbContext.cs` | `DbContext` chính: khai báo 3 `DbSet`, gọi `ApplyConfigurationsFromAssembly` |
| `Infrastructure/Persistence/SocialDbContextFactory.cs` | Design-time factory để `dotnet ef migrations add` chạy được độc lập, không cần start API — đọc connection string từ user-secrets |
| `Infrastructure/Persistence/Configurations/UserConfiguration.cs` | Unique index trên `NormalizedEmail`, ràng buộc độ dài cột |
| `Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs` | Unique `TokenHash`, FK → `User` |
| `Infrastructure/Persistence/Configurations/VerificationTokenConfiguration.cs` | FK → `User`, index theo `(UserId, Purpose)` để tra cứu nhanh khi resend/consume |
| `Infrastructure/Persistence/Migrations/` | EF Core tự sinh khi chạy `dotnet ef migrations add Initial` |
| `Infrastructure/DependencyInjection.cs` (khởi tạo) | Ở bước này chỉ có `AddDbContext<SocialDbContext>(UseNpgsql(...))` |

### GĐ 3 — Hasher, JWT, Swagger

| File / Thư mục | Tác dụng |
|---|---|
| `Application/Common/ICurrentUserService.cs` | Interface đọc `UserId` hiện tại — Application không được phép biết `HttpContext` nên phải đi qua interface này (dependency rule mục 2) |
| `Application/Options/JwtOptions.cs` | Model bind từ config `Jwt:SigningKey/Issuer/Audience/AccessTokenMinutes` |
| `Application/Interfaces/IJwtTokenService.cs` | Hợp đồng sinh/verify access token |
| `Application/Interfaces/IPasswordHasherService.cs` | Hợp đồng hash/verify mật khẩu |
| `Infrastructure/Security/PasswordHasherService.cs` | Wrap `PasswordHasher<User>` (PBKDF2) theo quyết định #3, không kéo theo full Identity |
| `Infrastructure/Security/JwtTokenService.cs` | Sinh access token JWT sống 15 phút |
| `Infrastructure/Identity/CurrentUserService.cs` | Implement `ICurrentUserService`, đọc `ClaimTypes.NameIdentifier` qua `IHttpContextAccessor` |
| `Infrastructure/DependencyInjection.cs` (mở rộng) | Thêm `AddAuthentication().AddJwtBearer(...)`, bind `JwtOptions` |
| `API/Program.cs` (mở rộng) | `AddSwaggerGen` kèm `AddSecurityDefinition("Bearer", ...)` để có nút Authorize |

### GĐ 4 — Đăng ký + xác minh + gửi lại

| File / Thư mục | Tác dụng |
|---|---|
| `Domain/Exceptions/NotFoundException.cs` | Ném khi không tìm thấy user/token; `GlobalExceptionHandler` (GĐ8) map → 404 |
| `Domain/Exceptions/ConflictException.cs` | Ném khi email đã tồn tại lúc đăng ký; map → 409 |
| `Domain/Exceptions/ValidationAppException.cs` | Ném khi một rule nghiệp vụ (không phải FluentValidation) thất bại trong service; map → 400 |
| `Application/Options/AppOptions.cs` | `FrontendBaseUrl` để dựng link xác minh (quyết định #14/C6) |
| `Application/Options/EmailOptions.cs` | `Provider` = `Console`\|`Smtp`\|`Brevo` + `FromAddress`/`FromName` |
| `Application/Options/SmtpOptions.cs`, `BrevoOptions.cs` | Cấu hình riêng cho từng nhà cung cấp email |
| `Application/Interfaces/IAuthService.cs` (khởi tạo) | Khai báo `RegisterAsync`, `ConfirmEmailAsync`, `ResendConfirmationAsync` — bổ sung thêm method ở GĐ5, GĐ6 |
| `Application/Interfaces/IEmailSender.cs` | Trừu tượng hoá việc gửi mail để Infrastructure có 3 cách implement khác nhau (quyết định #9) |
| `Application/DTOs/Auth/RegisterRequest.cs`, `ResendConfirmationRequest.cs` | Request body của 2 endpoint đầu |
| `Application/Validators/RegisterRequestValidator.cs` | Rule email hợp lệ, mật khẩu ≥ 8 ký tự có hoa/thường/số, confirm khớp (mục 5) |
| `Application/DependencyInjection.cs` (khởi tạo) | `AddValidatorsFromAssembly` + `AddFluentValidationAutoValidation` |
| `Infrastructure/Security/TokenHasher.cs` | Hash SHA-256 token trước khi lưu DB (quyết định #7) — dùng chung cho verification token lẫn refresh token |
| `Infrastructure/Email/ConsoleEmailSender.cs`, `SmtpEmailSender.cs`, `BrevoEmailSender.cs` | 3 implementation của `IEmailSender`; `AddInfrastructure()` chỉ đăng ký 1 cái theo `Email:Provider` |
| `Infrastructure/Services/AuthService.cs` (khởi tạo) | Hiện thực 3 method của GĐ4: tạo user, sinh/kiểm `VerificationToken`, áp cooldown 60 giây (C11) |
| `API/Controllers/AuthController.cs` (khởi tạo) | 3 action: `register`, `confirm-email`, `resend-confirmation` |

### GĐ 5 — Login/refresh/logout qua cookie

| File / Thư mục | Tác dụng |
|---|---|
| `Application/DTOs/Auth/LoginRequest.cs`, `LoginResult.cs`, `RefreshResult.cs` | `LoginResult` chứa `accessToken`+`expiresAt`+`user`; `RefreshResult` tách riêng vì response của `/refresh` không kèm thông tin user |
| `Application/Validators/LoginRequestValidator.cs` | Rule email/password bắt buộc |
| `IAuthService` (bổ sung) | Thêm `LoginAsync`, `RefreshAsync`, `LogoutAsync` — chỉ trả **chuỗi refresh token thô**, không biết gì về cookie |
| `Infrastructure/Services/AuthService.cs` (bổ sung) | Hiện thực rotation: revoke token cũ + set `ReplacedByTokenHash`, phát cặp token mới |
| `API/Auth/RefreshTokenCookie.cs` | Helper duy nhất biết cách Append/Read/Delete cookie `social_rt` (`HttpOnly; Secure; SameSite=Strict; Path=/api/auth`) — cô lập cookie-logic khỏi Application/Infrastructure đúng yêu cầu mục 7 GĐ5 |
| `API/Controllers/AuthController.cs` (bổ sung) | 3 action `login`/`refresh`/`logout`, gọi `RefreshTokenCookie` để set/đọc/xoá cookie |

### GĐ 6 — Quên/đặt lại mật khẩu

| File / Thư mục | Tác dụng |
|---|---|
| `Application/DTOs/Auth/ForgotPasswordRequest.cs`, `ResetPasswordRequest.cs` | Request body 2 endpoint |
| `Application/Validators/ResetPasswordRequestValidator.cs` | Rule mật khẩu mới hợp lệ + confirm khớp |
| `IAuthService` (bổ sung) | Thêm `ForgotPasswordAsync`, `ResetPasswordAsync` |
| `Infrastructure/Services/AuthService.cs` (bổ sung) | Trong 1 transaction (`Database.BeginTransactionAsync`): đổi `PasswordHash`, tiêu thụ mọi token `PasswordReset` còn hiệu lực, revoke mọi `RefreshToken` chưa revoke (quyết định #15/C7) |
| `API/Controllers/AuthController.cs` (bổ sung) | 2 action `forgot-password`/`reset-password` |

### GĐ 7 — Hồ sơ + avatar

| File / Thư mục | Tác dụng |
|---|---|
| `Application/Options/CloudinaryOptions.cs` | `CloudName`/`ApiKey`/`ApiSecret` |
| `Application/Interfaces/IProfileService.cs`, `IAvatarStorageService.cs` | Hợp đồng nghiệp vụ hồ sơ và hợp đồng lưu trữ ảnh (tách riêng để không ràng Application vào SDK Cloudinary) |
| `Application/DTOs/Profile/ProfileDto.cs`, `UpdateProfileRequest.cs` | Response/request cho `GET`/`PUT /api/profile/me` |
| `Application/Validators/UpdateProfileRequestValidator.cs` | Rule độ dài `DisplayName`/`Bio` |
| `Infrastructure/Media/CloudinaryAvatarService.cs` | Implement `IAvatarStorageService`: upload với `PublicId = avatars/{userId}`, `Overwrite = true`, trả URL có version (mục 6) |
| `Infrastructure/Services/ProfileService.cs` | Implement `IProfileService` |
| `API/Controllers/ProfileController.cs` | 3 endpoint `[Authorize]`: `GET`/`PUT me`, `POST me/avatar` (multipart ≤5MB) |

### GĐ 8 — Exception + pipeline

| File / Thư mục | Tác dụng |
|---|---|
| `API/ErrorHandling/GlobalExceptionHandler.cs` | Implement `IExceptionHandler` (.NET 8+): map `ValidationAppException→400`, `NotFoundException→404`, `ConflictException→409`, còn lại→500, trả về `ProblemDetails` |
| `API/Program.cs` (hoàn thiện) | Sắp middleware đúng thứ tự cuối cùng theo mục 7.3: `UseForwardedHeaders` → `UseExceptionHandler` → Swagger (dev) → `UseHttpsRedirection` → `UseCors` (tuỳ chọn) → `UseAuthentication` → `UseAuthorization` → `MapControllers` — dùng luôn từ đây, không đợi tới GĐ 17 |
| `appsettings.json` / `appsettings.Development.json` (hoàn thiện) | Đủ section `Jwt`, `App`, `Email`, `Smtp`, `Brevo`, `Cloudinary`, `ConnectionStrings`, `Cors:AllowedOrigins` (mặc định rỗng) |




