# 🌐 Social — Mạng Xã Hội Thông Minh & Sàn Giao Dịch Hàng Số (Social Commerce)

<div align="center">

![.NET 8](https://img.shields.io/badge/.NET_8-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![React 19](https://img.shields.io/badge/React_19-20232A?style=for-the-badge&logo=react&logoColor=61DAFB)
![TypeScript](https://img.shields.io/badge/TypeScript_5-3178C6?style=for-the-badge&logo=typescript&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white)
![Vite](https://img.shields.io/badge/Vite_8-646CFF?style=for-the-badge&logo=vite&logoColor=white)
![SignalR](https://img.shields.io/badge/SignalR-WebSocket-512BD4?style=for-the-badge&logoColor=white)
![Google Gemini](https://img.shields.io/badge/Google_Gemini-AI-4285F4?style=for-the-badge&logo=google&logoColor=white)

Nền tảng mạng xã hội kết hợp thương mại điện tử hàng số (**Social Commerce**) full-stack hiện đại: tích hợp hệ thống **gợi ý nội dung lai (Hybrid AI Recommendation)**, **AI phân tích nội dung (Gemini)**, **real-time WebSocket (SignalR)**, **chợ sản phẩm số bảo mật cao (AES-256-GCM)**, **cổng thanh toán VietQR (PayOS)** và **sổ cái kế toán kép (Double-Entry Ledger)**.

[Tính Năng](#-tính-năng-nổi-bật) • [Kiến Trúc](#-kiến-trúc-hệ-thống) • [Công Nghệ](#️-công-nghệ-sử-dụng-tech-stack) • [Quy Trình Nghiệp Vụ](#-quy-trình-nghiệp-vụ-gian-hàng-số-marketplace-workflow) • [API Reference](#-danh-mục-api-endpoints) • [Cài Đặt](#-hướng-dẫn-cài-đặt--chạy-cục-bộ-local-setup)

</div>

---

## ✨ Tính Năng Nổi Bật

| Phân hệ | Tính năng chi tiết |
|---|---|
| 🔐 **Xác thực & Phân quyền** | Đăng ký/Đăng nhập, JWT (Access + Refresh Token), Google OAuth 2.0, Xác minh email, Phân quyền 3 cấp (`Member`, `Seller`, `Admin`), Quản lý trạng thái tài khoản (`Active`, `Suspended`). |
| 👤 **Hồ sơ cá nhân** | Cập nhật avatar (Cloudinary), bio, username, thông tin liên hệ, onboarding lựa chọn sở thích cá nhân hóa cho thuật toán gợi ý. |
| 📝 **Mạng xã hội & AI** | Tạo/xóa bài viết, đính kèm đa phương tiện (ảnh, video), tự động phân tích nhãn/chủ đề và trích xuất vector ngữ nghĩa bằng Google Gemini AI, gắn thẻ sở thích. |
| 🏷️ **Social Commerce (Gắn thẻ sản phẩm)** | Đính kèm sản phẩm số trực tiếp vào bài đăng mạng xã hội. Người dùng xem bảng tin có thể xem thẻ tóm tắt và bấm chuyển thẳng đến trang mua hàng ngay. |
| ❤️ **Tương tác & Real-time** | Like, bình luận lồng nhau, theo dõi (Follow/Unfollow). Đẩy thông báo tức thì qua SignalR WebSocket (like, comment, follow, cập nhật đơn hàng). |
| 🤖 **Gợi ý thông minh (Hybrid Feed)** | Pipeline 6 nguồn đề xuất chạy song song: Sở thích (Interests) · Người theo dõi (Following) · Xu hướng 24h (Trending) · Lọc cộng tác (Collaborative) · Tìm kiếm ngữ nghĩa vector (pgvector) · Khám phá (Exploration). |
| 🛍️ **Chợ Hàng Số (Digital Marketplace)** | Khám phá sản phẩm số (Key bản quyền, tài khoản phần mềm, mã nguồn...), bộ lọc đa tiêu chí, phân loại cây danh mục 3 cấp, tìm kiếm và sắp xếp giá linh hoạt. |
| 📦 **Kho Số Bảo Mật (AES-256-GCM)** | Tự động hóa giao hàng số tức thì. Mã thẻ/tài khoản được mã hóa AES-GCM 256-bit, băm SHA-256 chống nạp trùng mã. Hỗ trợ tài liệu/file tải về qua Signed URL có thời hạn. |
| 🔒 **Chống Bán Trùng (Concurrency Lock)** | Phiên Checkout giữ chỗ 15 phút với kỹ thuật `FOR UPDATE SKIP LOCKED` trong Database Transaction, đảm bảo tuyệt đối không xảy ra hiện tượng bán vượt tồn kho (overselling) khi nhiều người mua cùng lúc. |
| 💳 **Thanh Toán Tự Động (VietQR / PayOS)** | Tích hợp cổng thanh toán trực tuyến PayOS qua mã VietQR chuẩn NAPAS 247. Xử lý Webhook bảo mật với chữ ký Checksum Key; có cổng Mock Payment giả lập cho môi trường phát triển local. |
| 📒 **Sổ Cái Kế Toán Kép (Double-Entry Ledger)** | Quản lý dòng tiền minh bạch chuẩn kế toán: Mỗi giao dịch sinh bút toán cân bằng (`Total Debit == Total Credit`). Phân bổ doanh thu sàn, tiền tạm giữ (`ShopPending`), tiền khả dụng (`ShopAvailable`). |
| 🏪 **Cổng Người Bán (Seller Portal)** | Đăng ký gian hàng, tạo sản phẩm & biến thể SKU, nạp kho số mã hóa hàng loạt, theo dõi đơn hàng, quản lý dòng tiền, sao kê sổ cái, liên kết ngân hàng và gửi yêu cầu rút tiền (Payouts). |
| ⚖️ **Khiếu Nại & Đánh Giá (Disputes & Reviews)** | Người mua đánh giá sao sau khi hoàn tất đơn hàng; gửi khiếu nại trong vòng 48h nếu mã lỗi/sai thông tin. Người bán hỗ trợ đổi mã mới hoặc hoàn tiền tự động qua sổ cái. |
| 🛡️ **Bảng Quản Trị (Admin Dashboard)** | Xét duyệt gian hàng, kiểm duyệt danh mục & sản phẩm, phân xử tranh chấp khiếu nại, duyệt lệnh rút tiền Payout của Seller, kiểm toán & đối soát sổ cái tự động. |
| ⚙️ **Hệ Thống Tác Vụ Ngầm (Background Workers)** | Chạy định kỳ tự động: Quét hủy phiên Checkout quá hạn, tự động giải ngân tiền tạm giữ sang khả dụng sau 48h, tự động leo thang khiếu nại quá hạn, đối soát số dư toàn sàn hàng ngày. |
| ⚡ **Hiệu Năng Cao & Cache Tầng** | In-Memory Cache (`IMemoryCache`) với kỹ thuật Cache-Aside cho Cây danh mục, Chi tiết sản phẩm, Hồ sơ Shop, Sở thích người dùng; chủ động xóa cache khi có cập nhật. |

---

## 🏗️ Kiến Trúc Hệ Thống

Dự án áp dụng mô hình **Clean Architecture theo Domain-Driven Modular Design** (Backend) kết hợp **Component-Driven Modular Architecture** (Frontend):

```
Social/
├── Backend/                                # ASP.NET Core 8 — Clean Architecture & Modular DI
│   ├── API/                                # Presentation Layer
│   │   ├── Controllers/                    # API Endpoints được phân tách theo Domain:
│   │   │   ├── Admin/                      # Quản trị hệ thống, tài khoản, vai trò
│   │   │   │   └── AdminController.cs
│   │   │   ├── Social/                     # Mạng xã hội: Auth, Posts, Comments, Follows, Interests...
│   │   │   │   ├── AuthController.cs
│   │   │   │   ├── PostsController.cs
│   │   │   │   ├── CommentsController.cs
│   │   │   │   ├── FollowsController.cs
│   │   │   │   ├── ProfileController.cs
│   │   │   │   ├── NotificationsController.cs
│   │   │   │   └── ...
│   │   │   └── Marketplace/                # Phân hệ Sàn Giao Dịch Hàng Số:
│   │   │       ├── Customer/               # Dành cho khách mua hàng:
│   │   │       │   ├── CatalogController.cs         # Khám phá sản phẩm, tìm kiếm, lọc
│   │   │       │   ├── CategoriesController.cs      # Cây danh mục, thuộc tính
│   │   │       │   ├── ProductsController.cs        # Chi tiết sản phẩm & biến thể
│   │   │       │   ├── CartController.cs            # Quản lý giỏ hàng
│   │   │       │   ├── CheckoutsController.cs       # Phiên checkout giữ chỗ kho số
│   │   │       │   ├── PaymentsController.cs        # Khởi tạo thanh toán VietQR / PayOS
│   │   │       │   ├── PaymentWebhooksController.cs # Webhook nhận tiền từ PayOS
│   │   │       │   ├── OrdersController.cs          # Đơn hàng, nhận mã đã giải mã
│   │   │       │   ├── ProductReviewsController.cs  # Đánh giá sao sản phẩm
│   │   │       │   ├── ComplaintsController.cs      # Khiếu nại đơn hàng
│   │   │       │   └── DevCheckoutsController.cs    # Mock payment kiểm thử local
│   │   │       ├── Seller/                 # Cổng Người Bán (Shop Owner):
│   │   │       │   ├── ShopsController.cs           # Đăng ký & quản lý gian hàng
│   │   │       │   ├── ShopOrdersController.cs      # Quản lý đơn hàng của shop
│   │   │       │   ├── ShopFinanceController.cs     # Ví tiền, sổ cái & biến động số dư
│   │   │       │   ├── ShopPayoutsController.cs     # Yêu cầu rút tiền về ngân hàng
│   │   │       │   └── ShopComplaintsController.cs  # Xử lý khiếu nại (Đổi mã/Hoàn tiền)
│   │   │       └── Admin/                  # Cổng Quản Trị Viên Sàn:
│   │   │           ├── AdminShopsController.cs      # Duyệt gian hàng, xử phạt vi phạm
│   │   │           ├── AdminCategoriesController.cs # Quản trị cây danh mục & thuộc tính
│   │   │           ├── AdminProductsController.cs   # Kiểm duyệt và gỡ bỏ sản phẩm
│   │   │           ├── AdminPayoutsController.cs    # Xét duyệt lệnh rút tiền
│   │   │           ├── AdminComplaintsController.cs # Phân xử khiếu nại tranh chấp
│   │   │           ├── AdminReportsController.cs    # Xử lý báo cáo vi phạm
│   │   │           └── AdminAuditController.cs      # Đối soát sổ cái kép toàn sàn
│   │   ├── Extensions/                     # Extension methods đăng ký middleware & services
│   │   └── Middlewares/                    # Global Exception Handler, JWT Validation Middleware
│   │
│   ├── Application/                        # Core Business Logic Layer
│   │   ├── Common/                         # Base models, paging, exceptions, result patterns
│   │   ├── DTOs/                           # Data Transfer Objects (Auth, Social, Marketplace)
│   │   ├── Interfaces/                     # Abstraction interfaces cho Services & Repositories
│   │   ├── Settings/                       # Strongly-typed configuration options
│   │   └── Validators/                     # FluentValidation rules cho từng request DTO
│   │
│   ├── Domain/                             # Enterprise Domain Layer (Entities & Rules)
│   │   ├── Entities/                       # Thực thể nghiệp vụ có vòng đời rõ ràng:
│   │   │   ├── Social/                     # User, Post, Comment, Follow, Interaction, Notification...
│   │   │   └── Marketplace/                # Shop, Product, Variant, DigitalCode, Cart, Order,
│   │   │                                   # PaymentTransaction, LedgerEntry, Complaint, Payout...
│   │   ├── Enums/                          # Trạng thái (ShopStatus, OrderStatus, ComplaintStatus...)
│   │   └── ValueObjects/                   # Value objects (Money VND, Address...)
│   │
│   └── Infrastructure/                     # Implementation & External Services (Modular DI)
│       ├── Common/                         # MemoryCacheService (Cache-Aside pattern)
│       ├── DependencyInjection.cs          # Master DI Coordinator nạp các Module độc lập
│       ├── External/                       # ExternalModule: AI (Gemini), Media (Cloudinary),
│       │                                   # Security (AES-GCM, SignedURL), Realtime (SignalR)
│       ├── Identity/                       # JwtTokenService, CurrentUserService
│       ├── Marketplace/                    # MarketplaceModule:
│       │   ├── Categories/, Complaints/, Inventory/, Ledger/, Orders/,
│       │   ├── Payments/, Payouts/, Products/, Reports/, Shops/
│       │   └── Background Workers (CheckoutExpiration, FundRelease, ComplaintEscalation, DailyReconciliation)
│       ├── Persistence/                    # EF Core SocialDbContext, UnitOfWork, Migrations
│       │   └── Configurations/             # Fluent API configs tổ chức theo Marketplace/ & Social/
│       ├── Recommendation/                 # RecommendationModule:
│       │   └── Hybrid Engine (6 Candidate Generators, FeedScorer, Collaborative Filtering)
│       └── Social/                         # SocialModule: Auth, Posts, Comments, Follows, Interests...
│
└── Frontend/                               # React 19 + TypeScript + Vite Modular Architecture
    └── src/
        ├── api/                            # Axios API clients đóng gói theo từng domain
        ├── components/
        │   ├── common/                     # Button, Input, Modal, Dropdown, Skeleton...
        │   ├── layout/                     # Navbar, MobileNav, Footer, NotificationBell
        │   ├── marketplace/                # OrderStatusBadge, ProductCard...
        │   └── posts/                      # PostCard, CreatePostBox, ProductPickerModal
        ├── pages/
        │   ├── admin/                      # AdminDashboardPage & các Tab con tách biệt:
        │   │   └── components/             # ShopsTab, CategoriesTab, PayoutsTab, ComplaintsTab, AuditTab...
        │   ├── seller/                     # SellerPortalPage & các Tab/Modal quản lý:
        │   │   ├── tabs/                   # OverviewTab, ProductsTab, OrdersTab, FinanceTab, ComplaintsTab
        │   │   └── components/             # CreateProductModal, BulkImportCodesModal, InspectCodesModal...
        │   ├── marketplace/                # MarketplacePage, ProductDetailPage, CartPage, CheckoutPage...
        │   │   └── components/             # OrderReviewModal, OrderComplaintModal, OrderItemCredentials...
        │   ├── home/                       # Feed mạng xã hội gợi ý thông minh
        │   ├── profile/                    # Hồ sơ cá nhân, đổi avatar, bài viết cá nhân
        │   ├── auth/                       # Đăng nhập, Đăng ký, Quên mật khẩu, Xác minh email
        │   └── onboarding/                 # Lựa chọn sở thích cá nhân ban đầu
        ├── routes/                         # React Router DOM v7 (Route Guards: AdminRoute, ProtectedRoute...)
        ├── store/                          # Zustand Stores (auth, cart, feed, notification, postModal)
        ├── styles/                         # CSS Module hóa: variables.css, base.css, components.css,
        │                                   # marketplace.css, social.css, index.css
        └── types/                          # TypeScript definitions phân tách rõ ràng
```

---

## 🛠️ Công Nghệ Sử Dụng (Tech Stack)

### Backend
| Phân loại | Công nghệ / Thư viện | Vai trò |
|---|---|---|
| **Runtime & Core** | .NET 8 (ASP.NET Core 8) | Nền tảng RESTful Web API hiệu năng cao, Dependency Injection |
| **Kiến trúc** | Clean Architecture & Modular DI | Phân chia module rõ ràng (`SocialModule`, `MarketplaceModule`, `RecommendationModule`, `ExternalModule`) |
| **Cơ sở dữ liệu** | PostgreSQL (Supabase) + EF Core | Lưu trữ quan hệ chuẩn ACID, quản lý Migration tự động |
| **Vector Search** | pgvector | Tìm kiếm tương đồng vector cho thuật toán gợi ý Semantic Feed |
| **Caching** | `Microsoft.Extensions.Caching.Memory` | In-Memory Caching (Cache-Aside pattern) tối ưu tốc độ đọc |
| **Mã hóa bảo mật** | AES-256-GCM + SHA-256 | Mã hóa an toàn kho số và đối chiếu băm kiểm tra chống trùng mã |
| **Cổng thanh toán** | PayOS SDK (VietQR NAPAS 247) | Tạo mã VietQR thanh toán tự động, xác thực Webhook bảo mật |
| **Real-time** | ASP.NET Core SignalR | WebSocket đẩy thông báo tức thì hai chiều (like, comment, đơn hàng) |
| **Trí tuệ nhân tạo** | Google Gemini API (`gemini-3.8-flash`) | Phân tích nội dung bài viết, gắn nhãn tự động, trích xuất vector embeddings |
| **Lưu trữ đám mây** | Cloudinary API | Tải lên và tối ưu hóa hình ảnh, avatar, video đa phương tiện |
| **Xác thực** | JWT Bearer + Google OAuth 2.0 | Xác thực token không trạng thái (Stateless), Google Sign-In |
| **Tiến trình nền** | `BackgroundService` / Hosted Workers | 4 worker tự động: Hủy checkout, Giải ngân ví, Leo thang khiếu nại, Đối soát sổ cái |

### Frontend
| Phân loại | Công nghệ / Thư viện | Vai trò |
|---|---|---|
| **Framework** | React 19 + TypeScript 5 | Xây dựng giao diện reactive, component-driven, an toàn kiểu dữ liệu |
| **Build Tool** | Vite 8 | Tốc độ biên dịch và Hot Module Replacement (HMR) tức thì |
| **State Management**| Zustand | Quản lý trạng thái gọn nhẹ, tối ưu re-render (Auth, Cart, Realtime Notifications) |
| **Định tuyến** | React Router DOM v7 | Điều hướng client-side, Route Guards (`AdminRoute`, `ProtectedRoute`, `GuestRoute`) |
| **HTTP Client** | Axios | Tương tác REST API, tự động gắn Bearer Token & Interceptors bắt lỗi |
| **Real-time Client**| `@microsoft/signalr` | Kết nối WebSocket nhận sự kiện trực tiếp từ máy chủ |
| **Xác thực Google** | `@react-oauth/google` | Đăng nhập Google One-Tap & OAuth button |
| **Styling** | Vanilla CSS Modular & Design Tokens | Hệ màu Dark Theme hiện đại, responsive, không phụ thuộc thư viện CSS cồng kềnh |

---

## 🛒 Quy Trình Nghiệp Vụ Gian Hàng Số (Marketplace Workflow)

```
[Người Bán (Seller)]
       │
       ├── 1. Mở Shop & Chờ Admin phê duyệt
       ├── 2. Tạo sản phẩm & phân loại biến thể (Key bản quyền, Tài khoản, Mã nguồn)
       └── 3. Nạp kho số hàng loạt (Hệ thống tự động mã hóa AES-256-GCM)
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
                       Admin đối soát và xác nhận chuyển khoản thành công
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

### 2. Mạng xã hội & Tương tác (Social & Interactions)
| Phương thức | Đường dẫn | Quyền hạn | Mô tả |
|---|---|---|---|
| `GET` | `/api/posts/feed` | Authorized | Lấy bảng tin gợi ý thông minh (Hybrid AI Recommendation) |
| `POST` | `/api/posts` | Authorized | Đăng bài viết mới (hỗ trợ đính kèm sản phẩm số) |
| `DELETE` | `/api/posts/{id}` | Authorized | Xóa bài viết của chính mình |
| `POST` | `/api/posts/{id}/like` | Authorized | Thả tim bài viết (bắn SignalR thông báo cho tác giả) |
| `DELETE` | `/api/posts/{id}/like` | Authorized | Bỏ thả tim bài viết |
| `GET` | `/api/posts/{id}/comments` | Authorized | Lấy danh sách bình luận của bài viết |
| `POST` | `/api/posts/{id}/comments` | Authorized | Thêm bình luận mới (hỗ trợ bình luận lồng nhau) |
| `POST` | `/api/follows/{userId}` | Authorized | Theo dõi người dùng khác |
| `DELETE` | `/api/follows/{userId}` | Authorized | Hủy theo dõi người dùng |
| `GET` | `/api/notifications` | Authorized | Xem danh sách thông báo cá nhân |
| `PUT` | `/api/notifications/{id}/read` | Authorized | Đánh dấu thông báo đã đọc |

### 3. Khách mua hàng & Khám phá sản phẩm (Marketplace - Customer)
| Phương thức | Đường dẫn | Quyền hạn | Mô tả |
|---|---|---|---|
| `GET` | `/api/categories/tree` | Public (Cached) | Lấy toàn bộ cây danh mục sản phẩm 3 cấp |
| `GET` | `/api/categories/{id}/attributes` | Public (Cached) | Lấy danh sách thuộc tính chuẩn theo danh mục |
| `GET` | `/api/marketplace/products` | Public | Tìm kiếm, lọc đa tiêu chí và phân trang sản phẩm |
| `GET` | `/api/marketplace/products/{slug}` | Public (Cached) | Chi tiết sản phẩm, các biến thể SKU và giá bán |
| `GET` | `/api/marketplace/products/{id}/reviews`| Public | Lấy danh sách đánh giá của khách hàng |
| `POST` | `/api/marketplace/products/{id}/reports`| Authorized | Báo cáo sản phẩm vi phạm chính sách |
| `GET` | `/api/cart` | Authorized | Lấy danh sách sản phẩm trong giỏ hàng hiện tại |
| `POST` | `/api/cart/items` | Authorized | Thêm một biến thể sản phẩm vào giỏ |
| `PUT` | `/api/cart/items/{id}` | Authorized | Cập nhật số lượng sản phẩm trong giỏ |
| `DELETE` | `/api/cart/items/{id}` | Authorized | Xóa sản phẩm khỏi giỏ hàng |
| `POST` | `/api/checkouts` | Authorized | Tạo phiên Checkout và khóa giữ mã kho (`SKIP LOCKED` 15p) |
| `GET` | `/api/checkouts/{id}` | Authorized | Lấy chi tiết phiên checkout và thời gian đếm ngược |
| `POST` | `/api/payments/payos/create` | Authorized | Tạo liên kết thanh toán VietQR PayOS |
| `POST` | `/api/payments/payos/webhook`| Public (Verify HMAC) | Webhook nhận xác nhận thanh toán tự động từ PayOS |
| `POST` | `/api/dev/checkouts/{id}/mock-pay` | Authorized (Dev only) | Giả lập thanh toán tức thì phục vụ kiểm thử |
| `GET` | `/api/orders` | Authorized | Danh sách các đơn hàng đã mua |
| `GET` | `/api/orders/{id}` | Authorized | Chi tiết đơn hàng và hiển thị mã số đã giải mã |
| `POST` | `/api/orders/{id}/reviews` | Authorized | Gửi đánh giá sao & nhận xét sau khi nhận hàng |
| `POST` | `/api/orders/{id}/complaints` | Authorized | Gửi khiếu nại báo lỗi mã/hàng hỏng trong vòng 48h |

### 4. Cổng Người Bán (Marketplace - Seller)
| Phương thức | Đường dẫn | Quyền hạn | Mô tả |
|---|---|---|---|
| `POST` | `/api/seller/shops` | Authorized | Nộp hồ sơ đăng ký mở gian hàng mới |
| `GET` | `/api/seller/shops/my-shop` | Authorized | Xem trạng thái và thông tin gian hàng của mình |
| `PUT` | `/api/seller/shops/my-shop` | Seller | Cập nhật thông tin, logo, banner, mô tả gian hàng |
| `GET` | `/api/seller/products` | Seller | Danh sách sản phẩm của gian hàng |
| `POST` | `/api/seller/products` | Seller | Đăng sản phẩm mới kèm các biến thể SKU |
| `PUT` | `/api/seller/products/{id}` | Seller | Chỉnh sửa thông tin, giá, trạng thái sản phẩm |
| `POST` | `/api/seller/inventory/codes` | Seller | Nạp danh sách mã số (Mã hóa tự động AES-256-GCM) |
| `GET` | `/api/seller/orders` | Seller | Quản lý các đơn hàng khách đã mua từ shop |
| `GET` | `/api/seller/finance` | Seller | Xem số dư ví: Khả dụng (`Available`), Tạm giữ (`Pending`) |
| `GET` | `/api/seller/finance/ledger` | Seller | Xem sao kê chi tiết mọi bút toán Nợ/Có của Shop |
| `POST` | `/api/seller/payouts` | Seller | Tạo yêu cầu rút tiền về tài khoản ngân hàng liên kết |
| `GET` | `/api/seller/complaints` | Seller | Danh sách các khiếu nại từ người mua |
| `POST` | `/api/seller/complaints/{id}/resolve`| Seller | Xử lý khiếu nại (Đổi mã mới hoặc Hoàn tiền vào ví khách) |

### 5. Bảng Điều Khiển Quản Trị (Marketplace & System - Admin)
| Phương thức | Đường dẫn | Quyền hạn | Mô tả |
|---|---|---|---|
| `GET` | `/api/admin/users` | Admin | Quản lý danh sách người dùng toàn sàn |
| `PUT` | `/api/admin/users/{id}/role` | Admin | Phân quyền vai trò (`Member`, `Seller`, `Admin`) |
| `GET` | `/api/admin/shops/pending` | Admin | Danh sách các gian hàng chờ xét duyệt |
| `POST` | `/api/admin/shops/{id}/review` | Admin | Duyệt (`Approve`) hoặc Từ chối (`Reject`) gian hàng |
| `POST` | `/api/admin/shops/{id}/sanctions` | Admin | Áp dụng hình phạt shop: Cảnh cáo, Khóa tạm thời, Đình chỉ |
| `POST` | `/api/admin/categories` | Admin | Thêm mới danh mục vào cây danh mục 3 cấp |
| `PUT` | `/api/admin/categories/{id}` | Admin | Sửa tên, thứ tự hiển thị, thuộc tính danh mục |
| `GET` | `/api/admin/payouts` | Admin | Danh sách yêu cầu rút tiền của các Seller |
| `POST` | `/api/admin/payouts/{id}/approve` | Admin | Xác nhận đã giải ngân chuyển khoản thành công |
| `POST` | `/api/admin/payouts/{id}/reject` | Admin | Từ chối lệnh rút tiền và hoàn lại số dư cho Seller |
| `GET` | `/api/admin/complaints` | Admin | Danh sách khiếu nại quá hạn hoặc leo thang lên sàn |
| `POST` | `/api/admin/complaints/{id}/arbitrate`| Admin | Phân xử khiếu nại: Chấp thuận hoàn tiền hoặc Bác bỏ |
| `GET` | `/api/admin/audit/reconciliation` | Admin | Báo cáo kiểm toán & đối soát cân bằng sổ cái kép toàn sàn |

---

## ⚡ Sự Kiện Real-time (SignalR WebSocket)

Endpoint kết nối: `/hubs/social`

| Tên sự kiện | Đối tượng nhận | Nội dung sự kiện |
|---|---|---|
| `NewPost` | Followers | Thông báo khi người đang theo dõi vừa đăng bài viết mới |
| `NewLike` | Tác giả bài viết | Thông báo khi bài viết nhận được một lượt thích mới |
| `NewComment` | Tác giả bài viết | Thông báo khi có bình luận mới trên bài viết |
| `NewFollower` | Người được theo dõi | Thông báo khi có người dùng mới bắt đầu theo dõi bạn |
| `NotificationReceived` | Người dùng đích | Đẩy thông báo hệ thống trực tiếp (Duyệt gian hàng, Đơn hàng thanh toán thành công, Cập nhật khiếu nại, Duyệt lệnh rút tiền) |

---

## 🚀 Hướng Dẫn Cài Đặt & Chạy Cục Bộ (Local Setup)

### Yêu Cầu Môi Trường
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/) & `npm`
- Cơ sở dữ liệu PostgreSQL 15+ (Khuyến nghị dùng [Supabase](https://supabase.com/))

---

### Bước 1: Clone Repository

```bash
git clone https://github.com/Thanhdat0212/Social.git
cd Social
```

---

### Bước 2: Thiết Lập & Khởi Chạy Backend

1. Di chuyển vào thư mục `Backend/API` và tạo file cấu hình `.env` từ mẫu:
   ```bash
   cd Backend/API
   cp .env.example .env
   ```

2. Cập nhật các biến môi trường thực tế trong file `.env`:
   ```env
   # 1. Chuỗi kết nối Database Supabase PostgreSQL
   ConnectionStrings__DefaultConnection="Host=<your-supabase-host>;Port=5432;Database=postgres;Username=postgres.<project-ref>;Password=<db-password>;SSL Mode=Require;Trust Server Certificate=true"

   # 2. Khóa bí mật ký JWT (tối thiểu 32 ký tự ngẫu nhiên)
   Jwt__SigningKey="your-super-secret-jwt-signing-key-minimum-32-chars-long"
   Jwt__Issuer="SocialApi"
   Jwt__Audience="SocialClient"
   Jwt__AccessTokenMinutes=15
   Jwt__RefreshTokenDays=7

   # 3. Google OAuth 2.0 (Dùng cho tính năng Đăng nhập bằng Google)
   Google__ClientId="your-google-client-id.apps.googleusercontent.com"
   Google__ClientSecret="your-google-client-secret"

   # 4. Cloudinary (Lưu trữ ảnh avatar, ảnh/video bài viết)
   Cloudinary__CloudName="your-cloudinary-cloud-name"
   Cloudinary__ApiKey="your-cloudinary-api-key"
   Cloudinary__ApiSecret="your-cloudinary-api-secret"

   # 5. Google Gemini AI (Gợi ý nội dung & Vector Embeddings)
   Gemini__ApiKey="your-gemini-api-key"
   Gemini__Model="gemini-3.8-flash"

   # 6. Cổng thanh toán PayOS (VietQR NAPAS 247)
   PayOs__ClientId="your-payos-client-id"
   PayOs__ApiKey="your-payos-api-key"
   PayOs__ChecksumKey="your-payos-checksum-key"

   # 7. Cấu hình Email SMTP (Xác minh tài khoản & Khôi phục mật khẩu)
   Email__Provider="Smtp"
   Smtp__Host="smtp.gmail.com"
   Smtp__Port=587
   Smtp__Username="your-email@gmail.com"
   Smtp__Password="your-app-password"
   Smtp__EnableSsl=true

   # 8. Cấu hình Frontend URL cho CORS
   App__FrontendBaseUrl="http://localhost:5173"
   ```

3. Khởi chạy Backend:
   ```bash
   dotnet run
   ```
   * 📡 Backend REST API chạy tại: `https://localhost:7126` (hoặc `http://localhost:5126`)
   * 📑 Tài liệu kiểm thử Swagger UI: `https://localhost:7126/swagger`

---

### Bước 3: Thiết Lập & Khởi Chạy Frontend

1. Di chuyển vào thư mục Frontend:
   ```bash
   cd ../../Frontend
   cp .env.example .env
   ```

2. Kiểm tra file `.env`:
   ```env
   VITE_API_BASE_URL=/api
   VITE_BACKEND_URL=http://localhost:5126
   VITE_GOOGLE_CLIENT_ID=your-google-client-id.apps.googleusercontent.com
   VITE_APP_NAME=Social Platform
   VITE_APP_ENV=development
   ```

3. Cài đặt các gói phụ thuộc và khởi động máy chủ phát triển:
   ```bash
   npm install
   npm run dev
   ```
   * 💻 Ứng dụng Frontend truy cập tại: `http://localhost:5173`

---

## 🐳 Triển Khai Với Docker

```bash
# Build Docker image cho Backend
cd Backend
docker build -t social-api -f API/Dockerfile .

# Chạy container
docker run -d -p 8080:8080 --env-file API/.env --name social-api-app social-api
```

---

## 🔒 Bảo Mật & Quy Ước Mã Nguồn

- **Không đưa Secrets lên Git**: Toàn bộ chuỗi kết nối, API key (Google, Gemini, Cloudinary, PayOS, JWT) đều nằm trong file `.env` được loại trừ bởi `.gitignore`.
- **Mã Hóa Kho Số**: Dữ liệu tài khoản/key số của người bán được mã hóa chuẩn **AES-256-GCM** trước khi lưu vào DB, chỉ được giải mã khi người mua hoàn tất thanh toán.
- **Chống Bán Vượt Tồn Kho**: Áp dụng Transaction kết hợp `SELECT ... FOR UPDATE SKIP LOCKED` cho phiên Checkout nhằm xử lý an toàn vấn đề tranh chấp kho hàng số.
- **Kế Toán Minh Bạch**: Sổ cái kép đảm bảo tổng Nợ bằng tổng Có cho mọi giao dịch thanh toán, giải ngân và hoàn tiền.

---

## 👤 Tác Giả & Bản Quyền

* **Tác giả**: [Thanhdat0212](https://github.com/Thanhdat0212)
* **Dự án**: Nền tảng Mạng Xã Hội Thông Minh & Thương Mại Điện Tử Hàng Số (Social Platform)
* **Bản quyền**: © 2026. Mọi quyền được bảo lưu.
