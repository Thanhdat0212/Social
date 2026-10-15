# 🌐 Social — Mạng Xã Hội Thông Minh

Nền tảng mạng xã hội full-stack với hệ thống **gợi ý nội dung lai (Hybrid Recommendation)**, **AI phân tích bài viết (Gemini)**, **real-time thông báo (SignalR)** và **xác thực bảo mật (JWT + Google OAuth)**.

---

## ✨ Tính Năng Nổi Bật

| Nhóm | Tính năng |
|---|---|
| 🔐 **Xác thực** | Đăng ký/Đăng nhập, JWT (Access + Refresh Token), Google OAuth 2.0, Xác minh email, Quên/Đặt lại mật khẩu |
| 👤 **Hồ sơ cá nhân** | Cập nhật avatar (Cloudinary), bio, username, onboarding chọn sở thích |
| 📝 **Bài viết** | Tạo/xóa bài viết, đính kèm ảnh/video, phân tích AI tự động (Gemini), gắn thẻ sở thích |
| ❤️ **Tương tác** | Like, comment, theo dõi (follow/unfollow) |
| 🛍️ **Gian hàng số** | Tạo gian hàng, quy trình xét duyệt/từ chối/kỷ luật Shop, Memory Cache tra cứu nhanh |
| 📂 **Danh mục & Thuộc tính** | Cây danh mục 3 cấp, từ điển thuộc tính kế thừa tổ tiên, đề xuất mở danh mục từ Seller |
| 🤖 **Gợi ý thông minh** | Pipeline Hybrid Recommendation (Interest · Following · Trending · Collaborative · Semantic · Exploration) |
| 🔔 **Real-time** | Thông báo tức thì qua SignalR WebSocket (like, comment, follow, bài viết mới) |
| 📱 **Giao diện** | Responsive, Mobile Navigation, Infinite Scroll, Dark Theme |

---

## 🏗️ Kiến Trúc Hệ Thống

```
Social/
├── Backend/                  # ASP.NET Core 8 — Clean Architecture
│   ├── API/                  # Presentation Layer (Controllers, Auth, ErrorHandling)
│   ├── Application/          # Business Logic (Interfaces, DTOs, Validators)
│   ├── Domain/               # Entities, Domain Models
│   └── Infrastructure/       # Implementations (DB, Services, AI, Realtime, ...)
│
└── Frontend/                 # React 19 + TypeScript + Vite
    └── src/
        ├── api/              # Axios HTTP clients
        ├── components/       # UI components (common, layout, posts)
        ├── hooks/            # Custom hooks (useSignalR, useInfiniteScroll, ...)
        ├── pages/            # Routes (auth, home, profile, onboarding)
        ├── services/         # SignalR service
        ├── store/            # Zustand state management
        ├── styles/           # Global CSS
        └── types/            # TypeScript types
```

---

## 🛠️ Tech Stack

### Backend
| Thành phần | Công nghệ |
|---|---|
| Framework | ASP.NET Core 8 |
| Database | PostgreSQL (Supabase) + Entity Framework Core |
| Caching | In-Memory Caching (`IMemoryCache`) — Tối ưu hóa đọc dữ liệu & giảm tải Database |
| Vector Search | pgvector (Semantic Search) |
| Real-time | SignalR WebSocket |
| AI | Google Gemini API (phân tích & embedding bài viết) |
| Xác thực | JWT Bearer + Refresh Token + Google OAuth 2.0 |
| Email | SMTP / Brevo |
| Media Storage | Cloudinary |
| Kiến trúc | Clean Architecture (Domain → Application → Infrastructure → API) |

### Frontend
| Thành phần | Công nghệ |
|---|---|
| Framework | React 19 + TypeScript |
| Build Tool | Vite 8 |
| State Management | Zustand |
| Routing | React Router DOM v7 |
| HTTP Client | Axios |
| Real-time | @microsoft/signalr |
| Auth | @react-oauth/google |

---

## 🤖 Hybrid Recommendation Pipeline

Hệ thống gợi ý nội dung chạy theo **6 nguồn song song**, tổng hợp và xếp hạng:

```
┌─────────────────────────────────────────────────────────┐
│                  CANDIDATE GENERATORS                   │
│                                                         │
│  InterestCandidates    → dựa theo sở thích đăng ký     │
│  FollowingCandidates   → bài viết từ người đang follow  │
│  TrendingCandidates    → bài viết hot trong 24h         │
│  CollaborativeCandidates → Collaborative Filtering      │
│  SemanticCandidates    → Vector similarity (pgvector)   │
│  ExplorationCandidates → khám phá nội dung mới          │
└───────────────────┬─────────────────────────────────────┘
                    │
            CandidateAggregator (dedup + merge)
                    │
          RecommendationRankingService (scoring)
                    │
              DiversityService (MMR algorithm)
                    │
                 Final Feed ✅
```

---

## ⚡ Real-time (SignalR)

Hub endpoint: `/hubs/social`

| Sự kiện | Mô tả |
|---|---|
| `NewPost` | Có bài viết mới từ người đang follow |
| `NewLike` | Bài viết của bạn được like |
| `NewComment` | Bài viết của bạn có comment mới |
| `NewFollower` | Có người mới follow bạn |

---

## 🚀 Hướng Dẫn Cài Đặt

### Yêu Cầu
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- PostgreSQL database (khuyến nghị dùng [Supabase](https://supabase.com/))

---

### 1. Clone Repository

```bash
git clone https://github.com/Thanhdat0212/Social.git
cd Social
```

---

### 2. Cấu Hình Backend

```bash
cd Backend
cp .env.example .env
```

Mở file `.env` và điền các giá trị:

```env
# Database (Supabase PostgreSQL)
ConnectionStrings__DefaultConnection="Host=<host>;Port=5432;Database=postgres;Username=postgres.<ref>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true"

# JWT
Jwt__SigningKey="your-super-secret-key-minimum-32-chars"
Jwt__Issuer="SocialApi"
Jwt__Audience="SocialClient"
Jwt__AccessTokenMinutes=15
Jwt__RefreshTokenDays=7

# Google OAuth
Google__ClientId="your-google-client-id.apps.googleusercontent.com"
Google__ClientSecret="your-google-client-secret"

# Cloudinary
Cloudinary__CloudName="your-cloud-name"
Cloudinary__ApiKey="your-api-key"
Cloudinary__ApiSecret="your-api-secret"

# Google Gemini AI
Gemini__ApiKey="your-gemini-api-key"
Gemini__Model="gemini-2.0-flash"

# Email (SMTP)
Email__Provider="Smtp"
Smtp__Host="smtp.gmail.com"
Smtp__Port=587
Smtp__Username="your-email@gmail.com"
Smtp__Password="your-app-password"

# CORS
App__FrontendBaseUrl="http://localhost:5173"
```

Khởi chạy Backend:

```bash
cd API
dotnet run
```

> API chạy tại: `https://localhost:7xxx` — Swagger UI: `https://localhost:7xxx/swagger`

---

### 3. Cấu Hình Frontend

```bash
cd Frontend
```

Tạo file `.env.local`:

```env
VITE_API_URL=https://localhost:7xxx
VITE_GOOGLE_CLIENT_ID=your-google-client-id.apps.googleusercontent.com
```

Cài dependencies và chạy:

```bash
npm install
npm run dev
```

> Frontend chạy tại: `http://localhost:5173`

---

## 📡 API Endpoints

### Authentication
| Method | Endpoint | Mô tả |
|---|---|---|
| `POST` | `/api/auth/register` | Đăng ký tài khoản |
| `POST` | `/api/auth/login` | Đăng nhập |
| `POST` | `/api/auth/google` | Đăng nhập Google OAuth |
| `POST` | `/api/auth/refresh-token` | Làm mới Access Token |
| `POST` | `/api/auth/logout` | Đăng xuất |
| `POST` | `/api/auth/forgot-password` | Gửi email đặt lại mật khẩu |
| `POST` | `/api/auth/reset-password` | Đặt lại mật khẩu |
| `GET` | `/api/auth/verify-email` | Xác minh email |

### Posts
| Method | Endpoint | Mô tả |
|---|---|---|
| `GET` | `/api/posts/feed` | Lấy feed gợi ý (Hybrid AI) |
| `POST` | `/api/posts` | Tạo bài viết mới |
| `DELETE` | `/api/posts/{id}` | Xóa bài viết |
| `POST` | `/api/posts/{id}/like` | Like bài viết |
| `DELETE` | `/api/posts/{id}/like` | Bỏ like |
| `GET` | `/api/posts/{id}/comments` | Lấy danh sách comment |
| `POST` | `/api/posts/{id}/comments` | Thêm comment |

### Users & Profile
| Method | Endpoint | Mô tả |
|---|---|---|
| `GET` | `/api/profile/{username}` | Xem hồ sơ người dùng |
| `PUT` | `/api/profile` | Cập nhật hồ sơ |
| `POST` | `/api/profile/avatar` | Đổi avatar |
| `GET` | `/api/users/search` | Tìm kiếm người dùng |
| `POST` | `/api/follows/{userId}` | Follow người dùng |
| `DELETE` | `/api/follows/{userId}` | Unfollow |

### Interests & Onboarding
| Method | Endpoint | Mô tả |
|---|---|---|
| `GET` | `/api/interests` | Lấy danh sách sở thích |
| `POST` | `/api/interests/user` | Lưu sở thích người dùng |

---

## 📁 Cấu Trúc Backend Chi Tiết

```
Backend/Infrastructure/
├── AI/                   # GeminiService, GeminiEmbeddingService, PostAiBackgroundWorker
├── Caching/              # MemoryCacheService, CacheKeys, CacheInvalidation
├── Email/                # SmtpEmailSender, BrevoEmailSender, ConsoleEmailSender
├── Identity/             # CurrentUserService, PasswordHasherService, JwtTokenService
├── Media/                # CloudinaryAvatarService, CloudinaryPostMediaService
├── Persistence/          # SocialDbContext, Repositories, Migrations
├── Realtime/             # SocialHub (SignalR), RealtimeNotificationService
├── Recommendation/       # 6 CandidateGenerators + Aggregator + Ranking + Diversity
├── Security/             # Các dịch vụ bảo mật
└── Services/             # AuthService, PostService, FeedService, LikeService, ...
```

---

## 🐳 Docker

```bash
cd Backend
docker build -t social-api .
docker run -p 8080:8080 --env-file .env social-api
```

---

---

## 🛒 Lộ Trình Triển Khai Gian Hàng Số (Marketplace Tracking)

> Hệ thống theo dõi tiến độ tích hợp module **Gian hàng hàng số (Digital Marketplace)** vào nền tảng Social.

### 📊 Bảng Trạng Thái Tổng Quan

| Chặng | Giai đoạn | Mô tả trọng tâm | Trạng thái |
|---|---|---|:---:|
| **Chặng 1** | **M0 - M1** | Nền tảng Admin Role, Bảng Notification, Memory Cache (`IMemoryCache`), Quản lý Shop (Tạo, Duyệt, Sanction) | `[x] Đã hoàn thành` |
| **Chặng 2** | **M2 - M4** | Danh mục tinh gọn (Cache Tree), Đăng sản phẩm (Cache chi tiết công khai) & Kho số bảo mật (AES-GCM, Account credentials) | `[x] Đã hoàn thành` |
| **Chặng 3** | **M5** | Giỏ hàng, Checkout giữ chỗ (`SKIP LOCKED`), Mock Payment & Tự động giao hàng | `[x] Đã hoàn thành` |
| **Chặng 4** | **M6 - M9** | Cổng thanh toán thật (PayOS/VietQR), Sổ cái (Ledger), Khiếu nại, Payout & Đối soát hệ thống | `[x] Đã hoàn thành` |

---

### 📌 Chi Tiết Từng Chặng & Checklist Nghiệm Thu

#### 🔹 CHẶNG 1: Nền tảng & Quản trị Gian hàng (Foundation & Shop Management)
* **Trạng thái**: `[x] Đã hoàn thành`
* **Nhiệm vụ chính**:
  - [x] **M0.1**: Bổ sung `Role` (`Admin`/`Member`) và `Status` (`Active`/`Suspended`) vào `User`. Gắn claim Role vào `JwtTokenService`. Tạo seed tài khoản Admin (`admin@social.com`).
  - [x] **M0.2**: Thiết lập cấu trúc thư mục module `Marketplace` (Shops, Catalog, Orders, Payments, Ledger, Disputes, Payouts) xuyên suốt Domain, Application, Infrastructure, API.
  - [x] **M0.3**: Tạo các lớp dùng chung: `IClock`/`SystemClock`, Value Object `Money` (long VND), Entity `Notification` lưu database + tích hợp phát realtime qua `SocialHub`.
  - [x] **M0.4**: Tạo Entity `IdempotencyRecord` hỗ trợ khóa chống lặp idempotency cho các tác vụ nhạy cảm.
  - [x] **M0.7**: Thiết lập tầng Memory Cache (`IMemoryCache` / `ICacheService`) dùng chung cho toàn bộ hệ thống (danh sách sở thích Interests, cây danh mục Categories, cấu hình hệ thống, hồ sơ shop/user), giảm tối đa số lần đọc từ Supabase PostgreSQL.
  - [x] **M1.1 - M1.2**: Tạo Entity `Shop`, `ShopReview`, `ShopSanction` kế thừa `BaseEntity` (`CreatedAtUtc`), cấu hình EF Core & Migration `AddShops`.
  - [x] **M1.3 - M1.7**: Viết Service tính `ShopEffectiveStatus`, API Seller đăng ký/mở/đóng Shop (`/api/shops`, `/api/shop`), API Admin duyệt/từ chối/phạt Shop (`/api/admin/shops`).
  - [x] **M1.8**: Frontend: Trang đăng ký mở Shop, quản lý trạng thái Shop cá nhân, giao diện Admin duyệt Shop.
* **Tiêu chuẩn nghiệm thu**:
  - User thường gọi API Admin bị trả `403 Forbidden`.
  - Một tài khoản chỉ tạo tối đa 1 Shop; bấm đúp bị chặn ở tầng DB Unique Index.
  - Tầng Memory Cache hoạt động chuẩn xác: các truy vấn dữ liệu ít biến động (Interests, Categories) trả lời tức thì (< 2ms) mà không cần query lại DB.
  - Admin từ chối bắt buộc có lý do; chủ shop nhận được thông báo trong hệ thống.
  - Khóa tài khoản User thì Shop tự chuyển sang trạng thái tạm ngưng (`Suspended/UserUnavailable`).

---

#### 🔹 CHẶNG 2: Danh mục & Kho hàng số bảo mật (Catalog & Digital Inventory)
* **Trạng thái**: `[x] Đã hoàn thành`
* **Nhiệm vụ chính**:
  - [x] **M2**: Seed sẵn cây danh mục hàng số 1-2 cấp (Key bản quyền, Tài khoản học tập/phần mềm, Tài liệu/Source code). Tích hợp Memory Cache cho `GET /api/categories/tree` và tự động evict khi Admin sửa danh mục.
  - [x] **M3**: Entity `Product`, `ProductVariant` (giá, SKU, tồn kho nằm ở Variant). Quản lý vòng đời (`Draft` -> `Listed` <-> `Hidden` -> `Archived`). Caching trang chi tiết sản phẩm (`/products/{slug}`) và thông tin shop công khai.
  - [x] **M4.1 - M4.3**: Entity `DigitalCode` cho kho số. Mã hóa `ValueEncrypted` (AES-GCM) và đối chiếu băm `ValueHash` (SHA-256) chống nạp trùng mã.
  - [x] **M4.4**: Hỗ trợ sản phẩm dạng File tải về qua Cloudinary/Storage riêng tư với link chữ ký hết hạn ngắn.
  - [x] **M4.5 [Đã chốt]**: Khóa học dùng chung kho số `DigitalCode` (chia sẻ tài khoản & mật khẩu học tập), không làm LMS/video.
  - [x] **M3.5 & M4.6**: Frontend: Form Seller tạo sản phẩm & giao diện nạp kho số; trang công khai xem chi tiết sản phẩm.
* **Tiêu chuẩn nghiệm thu**:
  - Trong DB: cột `ValueEncrypted` hoàn toàn là chuỗi mã hóa bảo mật.
  - Seller nạp 100 mã có 5 mã trùng -> Hệ thống nạp thành công 95 mã, báo cáo đúng 5 mã trùng.
  - Khóa Shop -> Tất cả sản phẩm tự động ẩn khỏi danh sách công khai.
  - Truy vấn cây danh mục và chi tiết sản phẩm công khai lấy từ Memory Cache; khi Seller cập nhật giá/thông tin hoặc Admin sửa danh mục, cache được xóa ngay lập tức (không hiển thị dữ liệu cũ).

---

#### 🔹 CHẶNG 3: Giỏ hàng, Concurrency Lock & Luồng mua hàng (Cart, Checkout & Mock Delivery)
* **Trạng thái**: `[x] Đã hoàn thành`
* **Nhiệm vụ chính**:
  - [x] **M5.1**: Entity `Cart`, `CartItem` (giỏ không giữ hàng, giá luôn đọc lại từ DB).
  - [x] **M5.2 - M5.3**: Tạo phiên `Checkout (Open, 15 phút)`. Giữ kho bằng lệnh `UPDATE digital_codes ... FOR UPDATE SKIP LOCKED` trong Transaction.
  - [x] **M5.4**: Service `CompleteCheckoutPaymentAsync`: Chuyển mã `Reserved -> Delivered`, tạo `ShopOrder` + `OrderItem` snapshot giá.
  - [x] **M5.5**: API Mock Payment (`/api/dev/checkouts/{id}/mock-pay`) dành riêng cho môi trường Development để test luồng.
  - [x] **M5.6**: Job quét hủy Checkout quá hạn 15 phút, trả mã về lại `Available`.
  - [x] **M5.8**: Frontend: Giỏ hàng, Checkout đếm ngược 15 phút, màn hình thanh toán thử nghiệm (Dev), trang đơn hàng và xem mã đã mua.
* **Tiêu chuẩn nghiệm thu**:
  - Kho chỉ còn 1 mã: Hai khách cùng bấm mua 1 giây -> Đúng 1 người thành công, 1 người báo hết hàng (không bao giờ bán trùng).
  - Gọi thanh toán giả 2 lần cùng một mã giao dịch -> Idempotent chỉ sinh 1 đơn hàng.
  - Người mua xem lại được mã đã giao trong lịch sử đơn hàng của mình.

---

#### 🔹 CHẶNG 4: Cổng thanh toán thực tế, Sổ cái & Rút tiền (Payment Gateway, Ledger & Payout)
* **Trạng thái**: `[x] Đã hoàn thành`
* **Nhiệm vụ chính**:
  - [x] **M6.1 - M6.2**: Tích hợp cổng thanh toán QR (VietQR / PayOS). Xử lý Webhook an toàn có xác thực chữ ký.
  - [x] **M6.3 - M6.6**: Hệ thống Sổ cái kép (`LedgerEntry` append-only). Phân bổ doanh thu: Tiền sàn, Tiền Shop tạm giữ (`ShopPending`).
  - [x] **M7**: Xử lý Khiếu nại (Complaint) trong 48h: Giữ tiền khiếu nại, hỗ trợ Shop đổi mã mới hoặc hoàn tiền.
  - [x] **M8**: Rút tiền (Payout) an toàn: Shop yêu cầu rút -> Khóa số dư khả dụng -> Admin chuyển khoản ngoài và xác nhận.
  - [x] **M9**: Job định kỳ đối soát tự phục hồi (`DailyReconciliationWorker`), API Audit kiểm toán sổ cái kép, Rate limiting.
  - [x] Frontend: Dashboard tài chính của Shop, sao kê sổ cái biến động Nợ/Có, quản lý tài khoản ngân hàng và yêu cầu rút tiền.
* **Tiêu chuẩn nghiệm thu**:
  - Mọi bút toán sổ cái: Tổng Nợ luôn bằng Tổng Có (`Debit == Credit`).
  - Webhook gửi trùng 5 lần -> Chỉ ghi sổ và xử lý đơn đúng 1 lần.
  - Shop không thể rút vượt quá số dư khả dụng (`ShopAvailable`).

---

### 💡 Ghi Chú Các Quyết Định Nghiệp Vụ Cốt Lõi
1. **Khóa học**: Thực chất là hình thức chia sẻ **tài khoản & mật khẩu** (account credentials/license access). Giao tự động từ kho `DigitalCode`, không dựng video streaming hay LMS.
2. **Quy ước Entity**: Kế thừa `BaseEntity` với `CreatedAtUtc` và `UpdatedAtUtc` để tương thích toàn bộ hệ thống Social hiện có.
3. **Quản lý quyền**: Module Shop yêu cầu User có `Role = Admin` đối với API phê duyệt/phạt, và `Status = Active` đối với Seller/Buyer.
4. **Chiến lược Memory Cache (`IMemoryCache`)**:
   - Áp dụng kỹ thuật Cache-Aside cho các dữ liệu có tần suất đọc vượt trội so với ghi: Sở thích (Interests), Cây danh mục (Categories Tree), Thuộc tính danh mục (Category Attributes), Chi tiết sản phẩm công khai (Product detail), Thông tin Shop công khai.
   - Cache Invalidation chủ động: Bắt buộc xóa cache khi có sự kiện thay đổi dữ liệu (Write/Update/Delete) để đảm bảo tính nhất quán.
   - Không cache các luồng tài chính & giao dịch: Giỏ hàng, Checkout, Giữ mã số `SKIP LOCKED`, Ghi sổ cái Ledger, và Rút tiền Payout luôn đọc/ghi trực tiếp trên Database trong Transaction an toàn.

---

## 👤 Tác Giả

**Thanhdat0212** — [GitHub](https://github.com/Thanhdat0212)

