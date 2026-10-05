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

## 🌐 Triển Khai (Production)

| Thành phần | Platform |
|---|---|
| Backend API | [Render](https://render.com/) |
| Frontend | [Vercel](https://vercel.com/) |
| Database | [Supabase](https://supabase.com/) (PostgreSQL + pgvector) |
| Media Storage | [Cloudinary](https://cloudinary.com/) |
| Email | SMTP Gmail / [Brevo](https://www.brevo.com/) |

---

## 👤 Tác Giả

**Thanhdat0212** — [GitHub](https://github.com/Thanhdat0212)
