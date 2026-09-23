# Social — Mạng xã hội (Phase 1: Auth + Hồ sơ cá nhân)

Kế hoạch chi tiết (kiến trúc, quyết định kỹ thuật, 17 giai đoạn): [Social-Phase1-KeHoach.md](./Social-Phase1-KeHoach.md)

## Trạng thái hiện tại

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

### Frontend (GĐ 9–16)

Chưa khởi tạo (`FE/` đang trống).

### Triển khai (GĐ 17)

Chưa bắt đầu.

## Cấu trúc thư mục

```
Social/
├── Backend/                   # ASP.NET Core Web API (.NET 8, Clean Architecture)
│   ├── Domain/                 # Entity, Enum, Exception — không phụ thuộc project khác
│   ├── Application/             # DTO, Validator, interface service (IAuthService...)
│   ├── Infrastructure/          # EF Core, JWT, hasher, email, DbContext
│   └── API/                    # Controllers, Program.cs, cookie helper
├── FE/                         # React + Vite + TypeScript (chưa khởi tạo)
├── Mẫu Backend/                 # Project tham khảo (không thuộc code Social, không push)
└── Social-Phase1-KeHoach.md     # Kế hoạch đầy đủ
```

Chi tiết từng file theo từng giai đoạn: xem mục 12 trong [Social-Phase1-KeHoach.md](./Social-Phase1-KeHoach.md).

## Công nghệ

- **Backend:** ASP.NET Core Web API (.NET), Clean Architecture 4 project, EF Core + Npgsql
- **Database:** PostgreSQL trên Supabase (`social-dev` / `social-prod`)
- **Auth:** JWT access token (bộ nhớ FE) + refresh token cookie `httpOnly` (tự xây, không dùng Identity)
- **Email:** Console (dev) / Gmail SMTP (dev) / Brevo REST API (prod)
- **Media:** Cloudinary (avatar)
- **Frontend:** React + Vite + TypeScript, axios + react-router-dom
- **Hosting:** Render (API, Docker) · Vercel (FE) · Supabase (DB)

## Chạy backend (dev)

```bash
cd Backend/API
dotnet user-secrets set "Jwt:SigningKey" "<chuỗi bí mật ≥ 32 ký tự>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection string social-dev>"
dotnet run
```

Swagger UI: `https://localhost:<port>/swagger` (có nút Authorize để test endpoint `[Authorize]`).

Mặc định `Email:Provider = Console` trong `appsettings.json` — email xác minh/reset in ra console, không cần cấu hình SMTP khi dev.
