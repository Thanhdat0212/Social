# Kế hoạch: Kết nối database social-dev & kiểm thử toàn bộ backend (GĐ1–8)

## Bối cảnh

Dự án **chưa từng có migration EF Core nào** (không có thư mục `Infrastructure/Persistence/Migrations/` trong toàn bộ lịch sử git, kể cả ở commit GĐ2), và **chưa có connection string** nào trong user-secrets (chỉ có `Jwt:SigningKey`). Điều này chặn việc test thật các luồng cần DB (register, login, profile...) và chặn milestone GĐ2 ("3 bảng hiện trong Table Editor của social-dev") lẫn yêu cầu GĐ17 (script migration idempotent cho prod).

Đã test độc lập phần không cần DB: pipeline, Swagger, FluentValidation, `GlobalExceptionHandler` (bắt được cả lỗi hạ tầng thật khi Postgres không kết nối được), JWT `[Authorize]` — tất cả đều đúng. Đã sửa 2 bug: (1) `GlobalExceptionHandler` check sai điều kiện `EMAIL_NOT_CONFIRMED` (đã sửa, đã commit+push ở `4e963df`), (2) `SocialDbContextFactory` fallback connection string không kích hoạt vì `""` khác `null` (đã sửa, **chưa commit**).

Mục tiêu: nối được vào Supabase `social-dev`, tạo migration đầu tiên, áp lên DB, rồi test thật toàn bộ 11 endpoint để xác nhận GĐ1–8 hoàn thành trước khi bắt đầu GĐ9 (frontend). **Không đụng đến `social-prod`** — nằm ngoài phạm vi (thuộc GĐ17).

## Các bước thực hiện

### Bước 1 — Lấy connection string Supabase
Supabase Dashboard → project `social-dev` → Project Settings → Database → Connection string. Ưu tiên **Direct connection** (đúng quyết định #10); nếu máy dev không kết nối được (rủi ro IPv6 đã ghi ở mục 9 kế hoạch gốc) thì chuyển sang **Session pooler**. Đổi từ URI Postgres sang định dạng Npgsql:
```
Host=<host>;Port=5432;Database=postgres;Username=postgres;Password=<password>;SSL Mode=Require;Trust Server Certificate=true
```

### Bước 2 — Lưu vào user-secrets
```
cd Backend/API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<chuỗi ở bước 1>"
```
Không dán connection string thật vào chat — chỉ cần xác nhận đã set xong bằng `dotnet user-secrets list`.

### Bước 3 — Commit bug fix còn treo
Commit `Backend/Infrastructure/Persistence/SocialDbContextFactory.cs` (fix fallback connection string rỗng) trước khi tạo migration, để không lẫn với thay đổi migration.

### Bước 4 — Tạo migration `Initial`
```
dotnet ef migrations add Initial --project Infrastructure --startup-project API --output-dir Persistence/Migrations
```
Không cần DB đã kết nối được để chạy lệnh này (chỉ build model). Đọc lại file migration sinh ra để xác nhận đúng 3 bảng `Users`/`RefreshTokens`/`VerificationTokens` với index đúng theo các file `Configurations/*.cs` (unique `NormalizedEmail`, unique `TokenHash`, index `(UserId, Purpose)`).

### Bước 5 — Áp migration lên social-dev
```
dotnet ef database update --project Infrastructure --startup-project API
```
Đây là bước thật sự kiểm tra connection string có kết nối được không. Nếu lỗi IPv6/timeout → quay lại Bước 1 đổi sang Session pooler.

### Bước 6 — Xác nhận trên Supabase
Kiểm tra Table Editor của `social-dev` thấy 3 bảng + `__EFMigrationsHistory` → khớp milestone GĐ2.

### Bước 7 — Commit migration
Add `Infrastructure/Persistence/Migrations/` vào git, commit riêng (vd: "GĐ2: migration Initial cho social-dev").

### Bước 8 — Test thật toàn bộ 11 endpoint
Khởi động API (`dotnet run`), test qua curl/Swagger theo đúng kịch bản milestone từng giai đoạn trong kế hoạch gốc:
- Register → lấy token từ Console email sender → confirm-email → `EmailConfirmed=true`
- Resend-confirmation: gọi 2 lần liên tiếp trong 60s → chỉ 1 token mới được tạo
- Login khi chưa confirm → xác nhận **403 EMAIL_NOT_CONFIRMED** (test lại bug đã fix, giờ có DB thật để test end-to-end)
- Login sau khi confirm → check `Set-Cookie` có `HttpOnly`
- Refresh → token cũ bị revoke trong DB, cookie mới
- Logout → cookie bị xoá, token không dùng lại được
- Forgot/reset password → trong transaction, đổi mật khẩu + revoke toàn bộ refresh token (test bằng cách login 2 nơi rồi reset, xác nhận cả 2 refresh đều fail)
- Profile GET/PUT/avatar (avatar cần Cloudinary secrets — nếu chưa có, báo rõ là bỏ qua phần này, không phải lỗi)

### Bước 9 — Báo cáo kết quả
Liệt kê endpoint nào pass/fail, nếu tất cả pass thì xác nhận GĐ1–8 hoàn thành, sẵn sàng sang GĐ9.

## Điều kiện dừng / cần hỏi lại
- Nếu Direct connection và Session pooler đều không kết nối được → dừng lại, có thể do social-dev bị Supabase tạm ngưng do không hoạt động (rủi ro #6 mục 9 kế hoạch gốc).
- Nếu chưa có Cloudinary secrets → vẫn test được 10/11 endpoint, avatar upload báo rõ là chưa test được, không coi là fail.
- Không tạo/sửa gì trên `social-prod`.

## Kiểm chứng cuối
`dotnet build` sạch, `dotnet ef database update` chạy không lỗi, Table Editor Supabase hiện đủ bảng, 10–11/11 endpoint trả đúng status code như bảng mục 5 kế hoạch gốc, `git status` sạch sau khi commit migration.
