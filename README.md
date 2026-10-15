# 🌐 Social — Mạng Xã Hội Thông Minh & Sàn Giao Dịch Hàng Số (Social Commerce)

Nền tảng mạng xã hội kết hợp thương mại điện tử hàng số (**Social Commerce**) full-stack hiện đại: tích hợp hệ thống **gợi ý nội dung lai (Hybrid AI Recommendation)**, **AI phân tích nội dung (Gemini)**, **real-time WebSocket (SignalR)**, **chợ sản phẩm số bảo mật cao (AES-GCM)**, **cổng thanh toán VietQR (PayOS)** và **sổ cái kế toán kép (Double-Entry Ledger)**.

---

## ✨ Tính Năng Nổi Bật

| Phân hệ | Tính năng chi tiết |
|---|---|
| 🔐 **Xác thực & Phân quyền** | Đăng ký/Đăng nhập, JWT (Access + Refresh Token), Google OAuth 2.0, Xác minh email, Phân quyền đa cấp (`Member`, `Seller`, `Admin`), Quản lý trạng thái (`Active`, `Suspended`). |
| 👤 **Hồ sơ cá nhân** | Cập nhật avatar (Cloudinary), bio, username, thông tin liên hệ, onboarding chọn sở thích cá nhân hóa. |
| 📝 **Mạng xã hội & AI** | Tạo/xóa bài viết, đính kèm đa phương tiện (ảnh, video), tự động phân tích nhãn/chủ đề và trích xuất vector ngữ nghĩa bằng Google Gemini AI, gắn thẻ sở thích. |
| 🏷️ **Social Commerce (Gắn thẻ sản phẩm)** | Đính kèm sản phẩm số trực tiếp vào bài viết. Người dùng đọc bài có thể xem thông tin tóm tắt và bấm chuyển thẳng đến trang sản phẩm để mua ngay. |
| ❤️ **Tương tác & Real-time** | Like, bình luận lồng nhau, theo dõi (Follow/Unfollow). Thông báo tức thì qua SignalR WebSocket (like, comment, follow, cập nhật đơn hàng). |
| 🤖 **Gợi ý thông minh (Hybrid Feed)** | Pipeline 6 nguồn song song: Sở thích (Interests) · Người theo dõi (Following) · Xu hướng 24h (Trending) · Lọc cộng tác (Collaborative) · Tìm kiếm ngữ nghĩa vector (pgvector) · Khám phá (Exploration). |
| 🛍️ **Chợ Hàng Số (Digital Marketplace)** | Khám phá sản phẩm số (Key bản quyền, phần mềm, tài khoản học tập, mã nguồn), bộ lọc đa tiêu chí, phân loại danh mục cây 3 cấp, tìm kiếm và sắp xếp giá linh hoạt. |
| 📦 **Kho Số Bảo Mật (AES-256-GCM)** | Tự động hóa giao hàng số tức thì. Mã thẻ/tài khoản được mã hóa AES-GCM 256-bit, đối chiếu băm SHA-256 chống nạp trùng mã. Hỗ trợ tài liệu/source code tải về qua Signed URL có thời hạn. |
| 🔒 **Chống Bán Trùng (Concurrency Lock)** | Phiên Checkout giữ chỗ 15 phút với kỹ thuật `FOR UPDATE SKIP LOCKED` trong Database Transaction, đảm bảo tuyệt đối không bao giờ xảy ra tình trạng bán vượt tồn kho (overselling) khi nhiều người mua cùng lúc. |
| 💳 **Thanh Toán Tự Động (VietQR / PayOS)** | Tích hợp cổng thanh toán trực tuyến PayOS qua mã VietQR chuẩn NAPAS 247. Xử lý Webhook bảo mật với chữ ký Checksum Key; hỗ trợ cổng Mock Payment giả lập cho môi trường phát triển local. |
| 📒 **Sổ Cái Kế Toán Kép (Double-Entry Ledger)** | Quản lý dòng tiền minh bạch chuẩn kế toán quốc tế: Mỗi giao dịch sinh bút toán cân bằng (`Total Debit == Total Credit`). Phân bổ doanh thu sàn, tiền tạm giữ của Shop (`ShopPending`), tiền khả dụng (`ShopAvailable`). |
| 🏪 **Cổng Người Bán (Seller Portal)** | Đăng ký gian hàng, tạo sản phẩm & biến thể SKU, nạp kho số hàng loạt, theo dõi đơn hàng, quản lý dòng tiền, sao kê biến động sổ cái, liên kết tài khoản ngân hàng và gửi yêu cầu rút tiền (Payouts). |
| ⚖️ **Khiếu Nại & Đánh Giá (Disputes & Reviews)** | Người mua đánh giá sao sau khi hoàn tất đơn hàng; gửi khiếu nại trong vòng 48h nếu mã lỗi/sai thông tin. Người bán hỗ trợ đổi mã mới hoặc hoàn tiền tự động qua sổ cái. |
| 🛡️ **Bảng Điều Khiển Quản Trị (Admin Dashboard)** | Xét duyệt gian hàng, kiểm duyệt danh mục/sản phẩm toàn sàn, phân xử tranh chấp khiếu nại, duyệt lệnh rút tiền Payout của Seller, chạy công cụ đối soát sổ cái tự động. |
| ⚙️ **Hệ Thống Tự Động Hóa (Background Workers)** | Các tác vụ ngầm chạy định kỳ: Quét hủy phiên Checkout quá hạn, tự động giải ngân tiền tạm giữ sang khả dụng sau 48h, tự động leo thang khiếu nại quá hạn, đối soát số dư toàn sàn hàng ngày. |
| ⚡ **Hiệu Năng Cao & Cache Tầng** | In-Memory Cache (`IMemoryCache`) với kỹ thuật Cache-Aside cho Cây danh mục, Chi tiết sản phẩm, Hồ sơ Shop, Sở thích người dùng; tự động xóa cache chủ động khi có thay đổi. |

---

## 🏗️ Kiến Trúc Hệ Thống

Dự án được xây dựng theo mô hình **Clean Architecture** (Backend) kết hợp **Component-Driven Modular Architecture** (Frontend):

```
Social/
├── Backend/                                # ASP.NET Core 8 — Clean Architecture
│   ├── API/                                # Presentation Layer
│   │   ├── Controllers/                    # API Endpoints (Auth, Posts, Users, Social...)
│   │   │   └── Marketplace/                # Các Controller chuyên trách Gian hàng số:
│   │   │       ├── CatalogController.cs    # Khám phá sản phẩm, tìm kiếm, lọc
│   │   │       ├── CategoriesController.cs # Cây danh mục, thuộc tính
│   │   │       ├── ProductsController.cs   # Chi tiết sản phẩm & biến thể
│   │   │       ├── CartController.cs       # Quản lý giỏ hàng
│   │   │       ├── CheckoutsController.cs  # Phiên checkout giữ chỗ kho số
│   │   │       ├── PaymentsController.cs   # Khởi tạo thanh toán VietQR / PayOS
│   │   │       ├── PaymentWebhooksController.cs # Xử lý Webhook thanh toán an toàn
│   │   │       ├── OrdersController.cs     # Đơn hàng, nhận mã số, tải file
│   │   │       ├── ProductReviewsController.cs # Đánh giá sản phẩm
│   │   │       ├── ComplaintsController.cs # Khiếu nại của người mua
│   │   │       ├── InventoryController.cs  # Nạp kho số mã hóa (Seller)
│   │   │       ├── ShopOrdersController.cs # Quản lý đơn hàng (Seller)
│   │   │       ├── ShopFinanceController.cs# Sổ cái & số dư ví (Seller)
│   │   │       ├── ShopPayoutsController.cs# Yêu cầu rút tiền (Seller)
│   │   │       ├── AdminShopsController.cs # Duyệt gian hàng (Admin)
│   │   │       ├── AdminPayoutsController.cs # Duyệt rút tiền (Admin)
│   │   │       ├── AdminAuditController.cs # Đối soát sổ cái kép (Admin)
│   │   │       └── ...
│   │   ├── Extensions/                     # Dependency Injection extensions
│   │   └── Middlewares/                    # Exception handling, JWT validation
│   │
│   ├── Application/                        # Core Business Logic Layer
│   │   ├── DTOs/                           # Data Transfer Objects
│   │   ├── Interfaces/                     # Service Interfaces & Abstractions
│   │   └── Validators/                     # FluentValidation rules
│   │
│   ├── Domain/                             # Enterprise Domain Layer
│   │   ├── Entities/                       # User, Post, Comment, Like, Follow, Notification
│   │   │   └── Marketplace/                # Các thực thể nghiệp vụ E-Commerce:
│   │   │       ├── Shops/                  # Shop, ShopReview, ShopSanction
│   │   │       ├── Categories/             # Category, CategoryAttribute
│   │   │       ├── Products/               # Product, ProductVariant, ProductReview
│   │   │       ├── Inventory/              # DigitalCode (AES Encrypted)
│   │   │       ├── Orders/                 # Cart, CartItem, Checkout, ShopOrder, OrderItem
│   │   │       ├── Payments/               # PaymentTransaction, PaymentWebhookLog
│   │   │       ├── Ledger/                 # LedgerEntry, LedgerAccount (Kế toán kép)
│   │   │       ├── Complaints/             # Complaint, ComplaintTimeline
│   │   │       └── Payouts/                # PayoutRequest, ShopBankAccount
│   │   ├── Enums/                          # Trạng thái vòng đời của thực thể
│   │   └── ValueObjects/                   # Money (long VND)
│   │
│   └── Infrastructure/                     # Implementation & External Services
│       ├── AI/                             # Google Gemini AI & Vector Embeddings
│       ├── Caching/                        # MemoryCacheService & Invalidation
│       ├── Identity/                       # JwtTokenService, CurrentUserService
│       ├── Media/                          # Cloudinary media upload
│       ├── Persistence/                    # EF Core SocialDbContext, Migrations, Repositories
│       ├── Realtime/                       # SignalR SocialHub, RealtimeNotificationService
│       ├── Recommendation/                 # 6 Candidate Generators & Ranking pipeline
│       ├── Security/                       # AesGcmCryptoService, SignedUrlService
│       └── Services/Marketplace/           # 12+ Marketplace Business Services & 4 Background Workers:
│           ├── Orders/CheckoutExpirationWorker.cs    # Tự động hủy checkout quá hạn
│           ├── Orders/FundReleaseWorker.cs           # Tự động chuyển tiền tạm giữ sang khả dụng
│           ├── Complaints/ComplaintEscalationWorker.cs # Tự động chuyển khiếu nại lên Admin
│           └── Audit/DailyReconciliationWorker.cs    # Đối soát tự động số dư sổ cái
│
└── Frontend/                               # React 19 + TypeScript + Vite
    └── src/
        ├── api/                            # Axios API Clients (auth, posts, marketplace, admin...)
        ├── components/
        │   ├── common/                     # Button, Input, Modal, Badge, Dropdown, Skeleton...
        │   ├── layout/                     # Navbar, MobileNav, Footer, NotificationBell
        │   └── posts/                      # PostCard, CreatePostBox, ProductPickerModal
        ├── pages/
        │   ├── auth/                       # Đăng nhập, Đăng ký, Quên/Đặt lại mật khẩu
        │   ├── home/                       # Feed mạng xã hội gợi ý thông minh
        │   ├── profile/                    # Hồ sơ cá nhân, đổi avatar, bài viết cá nhân
        │   ├── marketplace/                # Chợ hàng số, Chi tiết SP, Giỏ hàng, Checkout, Đơn hàng
        │   ├── seller/                     # Cổng Người Bán (Quản lý SP, Kho số, Đơn hàng, Ví, Rút tiền)
        │   └── admin/                      # Bảng Quản Trị Sàn (Duyệt shop, Duyệt tiền, Kiểm toán)
        ├── routes/                         # React Router DOM v7 (AdminRoute, ProtectedRoute, GuestRoute)
        ├── store/                          # Zustand Stores (auth, cart, feed, notification, postModal)
        ├── styles/                         # Vanilla CSS & Design Tokens (Dark Theme, Marketplace UI)
        └── types/                          # TypeScript definitions (auth, post, marketplace, admin...)
```

---

## 🛠️ Công Nghệ Sử Dụng (Tech Stack)

### Backend
| Phân loại | Công nghệ / Thư viện | Vai trò |
|---|---|---|
| **Runtime & Core** | .NET 8 (ASP.NET Core 8) | RESTful API, DI Container, Configuration |
| **Kiến trúc** | Clean Architecture | Đảm bảo tính mở rộng, bảo trì và phân tách trách nhiệm |
| **Cơ sở dữ liệu** | PostgreSQL (Supabase) + EF Core | Lưu trữ quan hệ ACID, Migration quản lý bằng mã C# |
| **Vector Search** | pgvector | Tìm kiếm tương đồng vector cho thuật toán gợi ý Semantic |
| **Caching** | `Microsoft.Extensions.Caching.Memory` | In-Memory Caching (Cache-Aside pattern) tối ưu tốc độ đọc |
| **Mã hóa bảo mật** | AES-256-GCM + SHA-256 | Mã hóa an toàn kho số và đối chiếu kiểm tra trùng lặp |
| **Cổng thanh toán** | PayOS SDK (VietQR NAPAS 247) | Tạo mã thanh toán ngân hàng tự động, Webhook bảo mật |
| **Real-time** | ASP.NET Core SignalR | WebSocket đẩy thông báo tức thì (like, comment, đơn hàng) |
| **Trí tuệ nhân tạo** | Google Gemini API | Phân tích bài viết, sinh vector embeddings |
| **Lưu trữ tệp** | Cloudinary API | Tải lên và tối ưu hóa hình ảnh, avatar, video |
| **Xác thực** | JWT Bearer + Google OAuth 2.0 | Xác thực token không trạng thái (Stateless), Google Sign-In |
| **Tiến trình nền** | `BackgroundService` / Hosted Workers | Chạy tác vụ ngầm tự động (Hủy checkout, Giải ngân, Đối soát) |

### Frontend
| Phân loại | Công nghệ / Thư viện | Vai trò |
|---|---|---|
| **Framework** | React 19 + TypeScript | Xây dựng giao diện người dùng reactive và type-safe |
| **Build Tool** | Vite 8 | Tốc độ biên dịch và Hot Module Replacement (HMR) cực nhanh |
| **State Management**| Zustand | Quản lý trạng thái gọn nhẹ (Auth, Cart, Realtime Notifications) |
| **Định tuyến** | React Router DOM v7 | Điều hướng client-side, Route Guards (`AdminRoute`, `ProtectedRoute`) |
| **HTTP Client** | Axios | Tương tác REST API, tự động gắn Bearer Token & Interceptors |
| **Real-time Client**| `@microsoft/signalr` | Nhận sự kiện trực tiếp từ máy chủ WebSocket |
| **Xác thực Google** | `@react-oauth/google` | Đăng nhập Google One-Tap & OAuth button |
| **Styling** | Vanilla CSS + CSS Variables | Hệ màu Dark Theme hiện đại, responsive, không phụ thuộc UI lib cồng kềnh |

---

## 🛒 Quy Trình Nghiệp Vụ Gian Hàng Số (Marketplace Workflow)

```
[Người Bán (Seller)]
       │
       ├── 1. Mở Shop & Chờ Admin phê duyệt
       ├── 2. Tạo sản phẩm & phân loại biến thể (Key, Tài khoản, File)
       └── 3. Nạp kho số hàng loạt (Hệ thống tự động mã hóa AES-GCM 256-bit)
                                 │
                                 ▼
[Người Mua (Buyer)] ◄────── [Chợ Sản Phẩm Số] ◄────── [Gắn Thẻ Bài Viết Feed]
       │
       ├── 1. Thêm sản phẩm vào giỏ hàng (Cart)
       ├── 2. Bấm Checkout ──► Hệ thống tạo phiên 15 phút & KHÓA KHO BẰNG:
       │                       `SELECT ... FOR UPDATE SKIP LOCKED`
       │
       ├── 3. Quét mã VietQR chuyển khoản (Cổng thanh toán PayOS)
       │                       │
       │                       ▼ (Webhook trả về có chữ ký Checksum)
       ├── 4. TỰ ĐỘNG GIAO HÀNG TỨC THÌ:
       │       ├── Chuyển trạng thái mã: `Reserved` -> `Delivered`
       │       ├── Người mua xem trực tiếp mã số đã giải mã tại trang đơn hàng
       │       └── Hoặc tải file tài liệu đính kèm qua link bảo mật Signed URL
       │
       └── 5. GHI SỔ CÁI KÉP (Ledger Entry):
               ├── Nợ: Tài khoản trung gian sàn thu tiền
               ├── Có: Doanh thu phí sàn
               └── Có: Tài khoản tạm giữ của Shop (`ShopPending`)
                                 │
                                 ▼ (Sau 48 giờ không có khiếu nại)
                       `FundReleaseWorker` tự động giải ngân:
                         `ShopPending` ──► `ShopAvailable` (Khả dụng)
                                 │
                                 ▼
                       Người bán gửi yêu cầu rút tiền (Payout)
                                 │
                                 ▼
                       Admin đối soát và chuyển khoản thành công
```

---

## 📡 Danh Mục API Endpoints

### 1. Xác thực & Tài khoản (Authentication & Profile)
| Phương thức | Đường dẫn | Quyền hạn | Mô tả |
|---|---|---|---|
| `POST` | `/api/auth/register` | Public | Đăng ký tài khoản người dùng mới |
| `POST` | `/api/auth/login` | Public | Đăng nhập bằng Email/Password, nhận JWT |
| `POST` | `/api/auth/google` | Public | Đăng nhập bằng Google ID Token |
| `POST` | `/api/auth/refresh-token` | Public | Làm mới Access Token bằng Refresh Token |
| `POST` | `/api/auth/logout` | Authorized | Đăng xuất và vô hiệu hóa Refresh Token |
| `POST` | `/api/auth/forgot-password` | Public | Yêu cầu gửi email khôi phục mật khẩu |
| `POST` | `/api/auth/reset-password` | Public | Đặt lại mật khẩu với token xác minh |
| `GET` | `/api/auth/verify-email` | Public | Xác minh địa chỉ email |
| `GET` | `/api/profile/{username}` | Authorized | Xem thông tin hồ sơ người dùng |
| `PUT` | `/api/profile` | Authorized | Cập nhật tên, tiểu sử cá nhân |
| `POST` | `/api/profile/avatar` | Authorized | Tải lên và thay đổi ảnh đại diện |

### 2. Mạng xã hội & Tương tác (Posts & Interactions)
| Phương thức | Đường dẫn | Quyền hạn | Mô tả |
|---|---|---|---|
| `GET` | `/api/posts/feed` | Authorized | Lấy bảng tin gợi ý thông minh (Hybrid Recommendation) |
| `POST` | `/api/posts` | Authorized | Đăng bài viết mới (hỗ trợ gắn thẻ sản phẩm số) |
| `DELETE` | `/api/posts/{id}` | Authorized | Xóa bài viết của chính mình |
| `POST` | `/api/posts/{id}/like` | Authorized | Thả tim bài viết (bắn SignalR cho tác giả) |
| `DELETE` | `/api/posts/{id}/like` | Authorized | Bỏ thả tim bài viết |
| `GET` | `/api/posts/{id}/comments` | Authorized | Lấy danh sách bình luận của bài viết |
| `POST` | `/api/posts/{id}/comments` | Authorized | Thêm bình luận mới |
| `POST` | `/api/follows/{userId}` | Authorized | Theo dõi người dùng khác |
| `DELETE` | `/api/follows/{userId}` | Authorized | Hủy theo dõi người dùng |

### 3. Khám phá Sản phẩm & Danh mục (Catalog & Categories)
| Phương thức | Đường dẫn | Quyền hạn | Mô tả |
|---|---|---|---|
| `GET` | `/api/categories/tree` | Public (Cached) | Lấy toàn bộ cây danh mục sản phẩm 3 cấp |
| `GET` | `/api/categories/{id}/attributes` | Public (Cached) | Lấy danh sách thuộc tính tiêu chuẩn theo danh mục |
| `GET` | `/api/marketplace/products` | Public | Tìm kiếm, lọc và phân trang danh sách sản phẩm |
| `GET` | `/api/marketplace/products/{slug}` | Public (Cached) | Chi tiết sản phẩm, biến thể SKU và giá bán |
| `GET` | `/api/marketplace/products/{id}/reviews`| Public | Lấy danh sách đánh giá của khách hàng |
| `GET` | `/api/shops/{slug}` | Public (Cached) | Xem thông tin công khai và các sản phẩm của Shop |

### 4. Mua hàng & Thanh toán (Cart, Checkout & Payments)
| Phương thức | Đường dẫn | Quyền hạn | Mô tả |
|---|---|---|---|
| `GET` | `/api/cart` | Authorized | Lấy danh sách sản phẩm trong giỏ hàng hiện tại |
| `POST` | `/api/cart/items` | Authorized | Thêm một biến thể sản phẩm vào giỏ |
| `PUT` | `/api/cart/items/{id}` | Authorized | Cập nhật số lượng sản phẩm trong giỏ |
| `DELETE` | `/api/cart/items/{id}` | Authorized | Xóa sản phẩm khỏi giỏ hàng |
| `POST` | `/api/checkouts` | Authorized | Tạo phiên Checkout và khóa giữ mã kho (`SKIP LOCKED` 15p) |
| `GET` | `/api/checkouts/{id}` | Authorized | Lấy chi tiết phiên checkout và đếm ngược thời gian |
| `POST` | `/api/payments/payos/create` | Authorized | Tạo liên kết thanh toán VietQR PayOS |
| `POST` | `/api/payments/payos/webhook`| Public (Verify HMAC) | Webhook nhận xác nhận chuyển khoản từ PayOS |
| `POST` | `/api/dev/checkouts/{id}/mock-pay` | Authorized (Dev only) | Giả lập thanh toán tức thì phục vụ kiểm thử |
| `GET` | `/api/orders` | Authorized | Danh sách các đơn hàng cá nhân đã mua |
| `GET` | `/api/orders/{id}` | Authorized | Chi tiết đơn hàng và hiển thị mã số đã giải mã |
| `POST` | `/api/orders/{id}/reviews` | Authorized | Gửi đánh giá sao & nhận xét sau khi mua hàng |
| `POST` | `/api/orders/{id}/complaints` | Authorized | Gửi yêu cầu khiếu nại (báo lỗi mã/hàng hỏng trong 48h) |

### 5. Cổng Người Bán (Seller Portal)
| Phương thức | Đường dẫn | Quyền hạn | Mô tả |
|---|---|---|---|
| `POST` | `/api/shops` | Authorized | Gửi hồ sơ đăng ký mở gian hàng mới |
| `GET` | `/api/shops/my-shop` | Authorized | Xem trạng thái và thông tin gian hàng của mình |
| `PUT` | `/api/shops/my-shop` | Seller | Cập nhật thông tin, logo, mô tả gian hàng |
| `GET` | `/api/seller/products` | Seller | Danh sách sản phẩm của gian hàng |
| `POST` | `/api/seller/products` | Seller | Đăng sản phẩm mới kèm các biến thể SKU |
| `PUT` | `/api/seller/products/{id}` | Seller | Chỉnh sửa thông tin, giá, trạng thái sản phẩm |
| `POST` | `/api/seller/inventory/codes` | Seller | Nạp danh sách mã số (Mã hóa tự động AES-GCM) |
| `GET` | `/api/seller/orders` | Seller | Quản lý các đơn hàng khách đã mua từ shop |
| `GET` | `/api/seller/finance` | Seller | Xem số dư ví: Khả dụng (`Available`), Tạm giữ (`Pending`) |
| `GET` | `/api/seller/finance/ledger` | Seller | Xem sao kê chi tiết mọi biến động Nợ/Có của Shop |
| `POST` | `/api/seller/payouts` | Seller | Tạo yêu cầu rút tiền về tài khoản ngân hàng |
| `GET` | `/api/seller/complaints` | Seller | Danh sách các khiếu nại từ người mua |
| `POST` | `/api/seller/complaints/{id}/resolve`| Seller | Xử lý khiếu nại (Đổi mã mới hoặc Hoàn tiền) |

### 6. Bảng Điều Khiển Quản Trị (Admin Dashboard)
| Phương thức | Đường dẫn | Quyền hạn | Mô tả |
|---|---|---|---|
| `GET` | `/api/admin/shops/pending` | Admin | Danh sách các gian hàng chờ xét duyệt |
| `POST` | `/api/admin/shops/{id}/review` | Admin | Duyệt (`Approve`) hoặc Từ chối (`Reject`) gian hàng |
| `POST` | `/api/admin/shops/{id}/sanctions` | Admin | Áp dụng hình phạt: Cảnh cáo, Khóa tạm thời hoặc Đình chỉ |
| `POST` | `/api/admin/categories` | Admin | Thêm mới danh mục vào cây danh mục 3 cấp |
| `PUT` | `/api/admin/categories/{id}` | Admin | Sửa tên, thứ tự hiển thị, thuộc tính danh mục |
| `GET` | `/api/admin/payouts` | Admin | Danh sách yêu cầu rút tiền của các Seller |
| `POST` | `/api/admin/payouts/{id}/approve` | Admin | Xác nhận đã chuyển khoản thành công |
| `POST` | `/api/admin/payouts/{id}/reject` | Admin | Từ chối lệnh rút tiền và hoàn lại số dư khả dụng |
| `GET` | `/api/admin/audit/reconciliation` | Admin | Kiểm toán tổng đối soát sổ cái toàn hệ thống |

---

## ⚡ Sự Kiện Real-time (SignalR WebSocket)

Hub Endpoint: `/hubs/social`

| Tên sự kiện | Người nhận | Nội dung sự kiện |
|---|---|---|
| `NewPost` | Followers | Thông báo khi có người bạn theo dõi vừa đăng bài |
| `NewLike` | Chủ bài viết | Thông báo khi bài viết nhận được một lượt thích |
| `NewComment` | Chủ bài viết | Thông báo khi bài viết có thêm một bình luận mới |
| `NewFollower` | Người được theo dõi | Thông báo khi có người dùng mới nhấn theo dõi |
| `NotificationReceived` | Người dùng cụ thể | Đẩy thông báo hệ thống (Duyệt shop, Đơn hàng thành công, Khiếu nại, Rút tiền) |

---

## 🚀 Hướng Dẫn Cài Đặt & Chạy Cục Bộ (Local Setup)

### Yêu Cầu Môi Trường
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/) & `npm`
- Cơ sở dữ liệu PostgreSQL (Khuyến nghị dùng [Supabase](https://supabase.com/))

---

### Bước 1: Clone Repository

```bash
git clone https://github.com/Thanhdat0212/Social.git
cd Social
```

---

### Bước 2: Thiết Lập & Khởi Chạy Backend

1. Di chuyển vào thư mục Backend và sao chép cấu hình mẫu:
   ```bash
   cd Backend
   cp .env.example .env
   ```

2. Cập nhật các thông số quan trọng trong file `.env`:
   ```env
   # 1. Chuỗi kết nối Database Supabase PostgreSQL
   ConnectionStrings__DefaultConnection="Host=<your-supabase-host>;Port=5432;Database=postgres;Username=postgres.<project-ref>;Password=<db-password>;SSL Mode=Require;Trust Server Certificate=true"

   # 2. Khóa bảo mật ký JWT (tối thiểu 32 ký tự)
   Jwt__SigningKey="your-super-secret-jwt-signing-key-minimum-32-chars-long"

   # 3. Google OAuth 2.0 (Tùy chọn)
   Google__ClientId="your-google-client-id.apps.googleusercontent.com"
   Google__ClientSecret="your-google-client-secret"

   # 4. Cloudinary (Lưu trữ ảnh & file)
   Cloudinary__CloudName="your-cloud-name"
   Cloudinary__ApiKey="your-api-key"
   Cloudinary__ApiSecret="your-api-secret"

   # 5. Google Gemini AI (Gợi ý nội dung & Embeddings)
   Gemini__ApiKey="your-gemini-api-key"

   # 6. Cổng thanh toán PayOS (VietQR NAPAS 247)
   PayOs__ClientId="your-payos-client-id"
   PayOs__ApiKey="your-payos-api-key"
   PayOs__ChecksumKey="your-payos-checksum-key"

   # 7. Cấu hình Frontend CORS
   App__FrontendBaseUrl="http://localhost:5173"
   ```

3. Chạy Backend:
   ```bash
   cd API
   dotnet run
   ```
   * Backend REST API khởi chạy tại: `https://localhost:7126` (hoặc `http://localhost:5126`)
   * Tài liệu Swagger UI: `https://localhost:7126/swagger`

---

### Bước 3: Thiết Lập & Khởi Chạy Frontend

1. Di chuyển vào thư mục Frontend:
   ```bash
   cd ../../Frontend
   cp .env.example .env
   ```

2. Kiểm tra file `.env` đã trỏ đúng cổng của Backend:
   ```env
   VITE_API_BASE_URL=/api
   VITE_BACKEND_URL=http://localhost:5126
   VITE_GOOGLE_CLIENT_ID=your-google-client-id.apps.googleusercontent.com
   ```

3. Cài đặt các gói phụ thuộc và khởi động máy chủ Dev:
   ```bash
   npm install
   npm run dev
   ```
   * Ứng dụng Frontend truy cập tại: `http://localhost:5173`

---

## 🐳 Triển Khai Với Docker

```bash
cd Backend
docker build -t social-api .
docker run -p 8080:8080 --env-file .env social-api
```

---

## 👤 Tác Giả & Bản Quyền

* **Tác giả**: [Thanhdat0212](https://github.com/Thanhdat0212)
* **Dự án**: Nền tảng Mạng Xã Hội Thông Minh & Thương Mại Điện Tử Hàng Số (Social Platform)
