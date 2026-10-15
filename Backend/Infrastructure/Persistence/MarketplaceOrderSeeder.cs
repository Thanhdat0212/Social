using Application.Common;
using Domain.Entities;
using Domain.Entities.Marketplace.Categories;
using Domain.Entities.Marketplace.Inventory;
using Domain.Entities.Marketplace.Orders;
using Domain.Entities.Marketplace.Payments;
using Domain.Entities.Marketplace.Products;
using Domain.Entities.Marketplace.Shops;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

public static class MarketplaceOrderSeeder
{
    private const string TargetUserEmail = "Thanhdatvv05@gmail.com";

    public static async Task<int> SeedOrdersForUserAsync(
        SocialDbContext context,
        ICryptoService cryptoService,
        ILogger logger,
        string targetEmail = TargetUserEmail,
        int targetOrderCount = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await context.Users.FirstOrDefaultAsync(
                u => u.Email.ToLower() == targetEmail.ToLower(),
                cancellationToken);

            if (user == null)
            {
                logger.LogWarning("Không tìm thấy người dùng với email {Email} để seed đơn hàng.", targetEmail);
                return 0;
            }

            logger.LogInformation("Bắt đầu chuẩn bị sản phẩm và seed dữ liệu cho: {Email} ({UserId})...", user.Email, user.Id);

            // 1. Đảm bảo có ít nhất 1 Shop đối tác (TechZone Official) để bán cho người dùng
            var partnerShop = await EnsurePartnerShopAsync(context, logger, cancellationToken);
            var userShop = await context.Shops.FirstOrDefaultAsync(s => s.OwnerUserId == user.Id, cancellationToken);

            // 2. Đảm bảo toàn bộ sản phẩm mẫu ở các thể loại có mặt trên sàn và có sẵn số lượng mua được (Stock > 0)
            var seededProducts = await EnsureSeedProductsAsync(context, partnerShop, userShop, cryptoService, logger, cancellationToken);

            // 3. Kiểm tra số lượng đơn hàng hiện có của người dùng
            var existingOrdersCount = await context.ShopOrders.CountAsync(o => o.BuyerUserId == user.Id, cancellationToken);
            if (existingOrdersCount >= targetOrderCount)
            {
                logger.LogInformation("Người dùng {Email} đã có đủ {Count} đơn hàng, bỏ qua seed đơn hàng mới.", targetEmail, existingOrdersCount);
                return existingOrdersCount;
            }

            // 4. Tạo bộ đơn hàng mẫu phong phú với đa dạng thể loại và trạng thái (khoảng 100 đơn)
            var createdCount = await CreateSeedOrdersAsync(context, user, partnerShop, userShop, seededProducts, cryptoService, logger, targetOrderCount, cancellationToken);
            var totalOrders = await context.ShopOrders.CountAsync(o => o.BuyerUserId == user.Id, cancellationToken);
            logger.LogInformation("Đã hoàn tất seed {Count} đơn hàng mới. Tổng số đơn hàng của {Email} hiện tại: {Total}!", createdCount, targetEmail, totalOrders);
            return totalOrders;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi seed đơn hàng cho {Email}: {Message}", targetEmail, ex.Message);
            return 0;
        }
    }

    private static async Task<Shop> EnsurePartnerShopAsync(SocialDbContext context, ILogger logger, CancellationToken cancellationToken)
    {
        const string partnerShopSlug = "techzone-official";
        var shop = await context.Shops.FirstOrDefaultAsync(s => s.Slug == partnerShopSlug, cancellationToken);
        if (shop != null) return shop;

        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Admin, cancellationToken)
            ?? await context.Users.FirstAsync(cancellationToken);

        shop = new Shop(
            ownerUserId: adminUser.Id,
            name: "TechZone Official Software & Accounts",
            slug: partnerShopSlug,
            contactEmail: "support@techzone.vn",
            description: "Gian hàng chính hãng uy tín phân phối bản quyền phần mềm, tài khoản giải trí số và các khóa học lập trình cao cấp bảo hành 100%.",
            logoUrl: "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=400&auto=format&fit=crop&q=80"
        );
        shop.Approve();
        context.Shops.Add(shop);
        await context.SaveChangesAsync(cancellationToken);

        var balance = new Domain.Entities.Marketplace.Ledger.ShopBalanceCache(shop.Id);
        context.ShopBalanceCaches.Add(balance);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Đã tạo Shop đối tác: {Name}", shop.Name);
        return shop;
    }

    public class ProductSeedSpec
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string CategorySlug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string VariantLabel { get; set; } = string.Empty;
        public long Price { get; set; }
        public long ComparePrice { get; set; }
        public DeliveryType DeliveryType { get; set; } = DeliveryType.CodePool;
    }

    private static readonly List<ProductSeedSpec> AllProductSpecs = new()
    {
        // ------------------ THỂ LOẠI 1: TÀI KHOẢN & DỊCH VỤ SỐ - GIẢI TRÍ ------------------
        new()
        {
            Key = "netflix",
            Name = "Tài khoản Netflix Premium 4K UHD Ultra (Gói 1 Tháng)",
            Slug = "tai-khoan-netflix-premium-4k-uhd-1-thang",
            CategorySlug = "giai-tri-xem-phim",
            Description = "Tài khoản Netflix Premium hỗ trợ xem phim 4K HDR mượt mà trên Smart TV, PC, Điện thoại. Xem không giới hạn kho phim bom tấn, bảo hành trọn thời gian sử dụng.",
            ImageUrl = "https://images.unsplash.com/photo-1574375927938-d5a98e8ffe85?w=600&auto=format&fit=crop&q=80",
            Sku = "NETFLIX-PREM-4K-1M",
            VariantLabel = "1 Tháng (4K UHD)",
            Price = 79000,
            ComparePrice = 120000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "spotify",
            Name = "Tài khoản Spotify Premium Chính Chủ Cá Nhân (6 Tháng)",
            Slug = "tai-khoan-spotify-premium-chinh-chu-6-thang",
            CategorySlug = "giai-tri-xem-phim",
            Description = "Nâng cấp tài khoản Spotify Premium trên chính email cá nhân. Nghe nhạc chất lượng cao 320kbps không quảng cáo, tải nhạc offline thoải mái.",
            ImageUrl = "https://images.unsplash.com/photo-1614680376593-902f749f7ffc?w=600&auto=format&fit=crop&q=80",
            Sku = "SPOTIFY-PREM-6M",
            VariantLabel = "6 Tháng cá nhân",
            Price = 180000,
            ComparePrice = 295000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "youtube",
            Name = "Gói Nâng Cấp YouTube Premium Chính Chủ Không Quảng Cáo (1 Năm)",
            Slug = "goi-nang-cap-youtube-premium-chinh-chu-1-nam",
            CategorySlug = "giai-tri-xem-phim",
            Description = "Trải nghiệm YouTube & YouTube Music không quảng cáo, nghe dưới nền khi tắt màn hình, tải video Full HD offline.",
            ImageUrl = "https://images.unsplash.com/photo-1611162617213-7d7a39e9b1d7?w=600&auto=format&fit=crop&q=80",
            Sku = "YOUTUBE-PREM-1Y",
            VariantLabel = "1 Năm chính chủ",
            Price = 280000,
            ComparePrice = 450000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "disney",
            Name = "Tài khoản Disney+ Hotstar VIP 4K Xem Phim Marvel (1 Năm)",
            Slug = "tai-khoan-disney-hotstar-vip-4k-1-nam",
            CategorySlug = "giai-tri-xem-phim",
            Description = "Kho phim bom tấn Disney, Marvel, Pixar, Star Wars chất lượng 4K Dolby Atmos sắc nét.",
            ImageUrl = "https://images.unsplash.com/photo-1534447677768-be436bb09401?w=600&auto=format&fit=crop&q=80",
            Sku = "DISNEY-PLUS-1Y",
            VariantLabel = "1 Năm Full 4K",
            Price = 199000,
            ComparePrice = 390000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "applemusic",
            Name = "Gói Gia Hạn Apple Music Nghe Nhạc Lossless & Dolby Atmos (1 Năm)",
            Slug = "goi-gia-han-apple-music-lossless-1-nam",
            CategorySlug = "giai-tri-xem-phim",
            Description = "Thưởng thức hơn 100 triệu bài hát chất lượng Lossless và Spatial Audio trên iPhone, iPad, Android và Mac.",
            ImageUrl = "https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=600&auto=format&fit=crop&q=80",
            Sku = "APPLE-MUSIC-1Y",
            VariantLabel = "1 Năm Cá nhân",
            Price = 240000,
            ComparePrice = 420000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "hbomax",
            Name = "Tài khoản HBO Max VIP 4K Xem Phim Chiếu Rạp (6 Tháng)",
            Slug = "tai-khoan-hbo-max-vip-4k-6-thang",
            CategorySlug = "giai-tri-xem-phim",
            Description = "Độc quyền xem các series kinh điển House of the Dragon, Game of Thrones, The Last of Us chất lượng 4K HDR.",
            ImageUrl = "https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=600&auto=format&fit=crop&q=80",
            Sku = "HBOMAX-VIP-6M",
            VariantLabel = "6 Tháng VIP 4K",
            Price = 165000,
            ComparePrice = 320000,
            DeliveryType = DeliveryType.CodePool
        },

        // ------------------ THỂ LOẠI 2: TÀI KHOẢN & DỊCH VỤ SỐ - HỌC TẬP & LÀM VIỆC ------------------
        new()
        {
            Key = "chatgpt",
            Name = "Tài khoản ChatGPT Plus (GPT-4o & Canvas) Riêng Tư (1 Tháng)",
            Slug = "tai-khoan-chatgpt-plus-gpt-4o-1-thang",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Tài khoản ChatGPT Plus bản quyền OpenAI độc quyền 1 người dùng. Truy cập GPT-4o không giới hạn tốc độ, DALL-E 3, phân tích dữ liệu chuyên sâu và Voice Mode.",
            ImageUrl = "https://images.unsplash.com/photo-1620712943543-bcc4688e7485?w=800&auto=format&fit=crop&q=80",
            Sku = "CHATGPT-PLUS-4O-1M",
            VariantLabel = "1 Tháng riêng tư",
            Price = 250000,
            ComparePrice = 500000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "claude",
            Name = "Tài khoản Claude Pro Anthropic (Sonnet 3.5 & Artifacts) (1 Tháng)",
            Slug = "tai-khoan-claude-pro-anthropic-sonnet-1-thang",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Mô hình ngôn ngữ đỉnh cao cho lập trình và phân tích văn bản dài 200k tokens. Viết code chuẩn xác và giao diện Artifacts tương tác cao.",
            ImageUrl = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=600&auto=format&fit=crop&q=80",
            Sku = "CLAUDE-PRO-1M",
            VariantLabel = "1 Tháng Pro VIP",
            Price = 260000,
            ComparePrice = 520000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "midjourney",
            Name = "Tài khoản Midjourney AI Standard Plan Vẽ Ảnh Nghệ Thuật (1 Tháng)",
            Slug = "tai-khoan-midjourney-ai-standard-1-thang",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Tạo ảnh AI siêu thực v6.0 với 15 giờ tạo ảnh nhanh Fast Hours mỗi tháng, không giới hạn Relax Hours, thương mại tự do.",
            ImageUrl = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=600&auto=format&fit=crop&q=80",
            Sku = "MIDJOURNEY-STD-1M",
            VariantLabel = "1 Tháng Standard",
            Price = 290000,
            ComparePrice = 580000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "canva",
            Name = "Tài khoản Canva Pro Giáo Dục Bản Quyền Mở Khóa Đầy Đủ (Vĩnh Viễn)",
            Slug = "tai-khoan-canva-pro-giao-duc-vinh-vien",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Mở khóa kho tài nguyên triệu ảnh, font chữ cao cấp, tính năng xóa phông một chạm và Magic Studio AI.",
            ImageUrl = "https://images.unsplash.com/photo-1626785774573-4b799315345d?w=600&auto=format&fit=crop&q=80",
            Sku = "CANVA-PRO-LIFETIME",
            VariantLabel = "Vĩnh viễn trọn đời",
            Price = 120000,
            ComparePrice = 280000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "grammarly",
            Name = "Tài khoản Grammarly Premium Sửa Lỗi Ngữ Pháp & AI Viết (1 Năm)",
            Slug = "tai-khoan-grammarly-premium-1-nam",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Hỗ trợ viết luận tiếng Anh, email chuyên nghiệp, phát hiện đạo văn và tinh chỉnh văn phong theo ngữ cảnh.",
            ImageUrl = "https://images.unsplash.com/photo-1455390582262-044cdead277a?w=600&auto=format&fit=crop&q=80",
            Sku = "GRAMMARLY-PREM-1Y",
            VariantLabel = "1 Năm Premium",
            Price = 210000,
            ComparePrice = 450000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "notion",
            Name = "Tài khoản Notion AI Plus Không Giới Hạn Quản Lý Dự Án (1 Năm)",
            Slug = "tai-khoan-notion-ai-plus-1-nam",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Không gian làm việc cộng tác all-in-one tích hợp AI tóm tắt tài liệu, brainstorm ý tưởng và quản lý task biểu đồ Kanban.",
            ImageUrl = "https://images.unsplash.com/photo-1484480974693-6ca0a78fb36b?w=600&auto=format&fit=crop&q=80",
            Sku = "NOTION-AI-PLUS-1Y",
            VariantLabel = "1 Năm Unlimited",
            Price = 320000,
            ComparePrice = 650000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "duolingo",
            Name = "Gói Duolingo Super Học Ngoại Ngữ Trái Tim Vô Hạn (1 Năm)",
            Slug = "goi-duolingo-super-hoc-ngoai-ngu-1-nam",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Học ngoại ngữ không giới hạn trái tim, không bị quảng cáo làm phiền, mở khóa luyện tập phát âm chuyên sâu.",
            ImageUrl = "https://images.unsplash.com/photo-1546410531-bb4caa6b424d?w=600&auto=format&fit=crop&q=80",
            Sku = "DUOLINGO-SUPER-1Y",
            VariantLabel = "1 Năm không giới hạn",
            Price = 190000,
            ComparePrice = 380000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "coursera",
            Name = "Tài khoản Coursera Plus Học Hàng Ngàn Khóa Học & Cấp Bằng (1 Năm)",
            Slug = "tai-khoan-coursera-plus-cap-bang-1-nam",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Học và nhận chứng chỉ quốc tế từ Google, IBM, Stanford, Yale không giới hạn số lượng khóa học trong 12 tháng.",
            ImageUrl = "https://images.unsplash.com/photo-1501504905252-473c47e087f8?w=600&auto=format&fit=crop&q=80",
            Sku = "COURSERA-PLUS-1Y",
            VariantLabel = "1 Năm Full Certificate",
            Price = 450000,
            ComparePrice = 900000,
            DeliveryType = DeliveryType.CodePool
        },

        // ------------------ THỂ LOẠI 3: TÀI KHOẢN & DỊCH VỤ SỐ - VPN & TIỆN ÍCH MẠNG ------------------
        new()
        {
            Key = "nordvpn",
            Name = "Tài khoản VPN NordVPN Cao Cấp 10 Thiết Bị Tốc Độ Cao (1 Năm)",
            Slug = "tai-khoan-vpn-nordvpn-10-thiet-bi-1-nam",
            CategorySlug = "vpn-tien-ich-mang",
            Description = "Bảo vệ danh tính duyệt web an toàn tuyệt đối với NordVPN. Hơn 5000 máy chủ toàn cầu, vượt tường lửa, xem Netflix Mỹ, tốc độ siêu nhanh không giật lag.",
            ImageUrl = "https://images.unsplash.com/photo-1563986768609-322da13575f3?w=600&auto=format&fit=crop&q=80",
            Sku = "NORDVPN-PREM-1Y",
            VariantLabel = "1 Năm (10 thiết bị)",
            Price = 299000,
            ComparePrice = 750000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "expressvpn",
            Name = "Tài khoản ExpressVPN Tốc Độ Cao Lightway Protocol (1 Năm)",
            Slug = "tai-khoan-expressvpn-lightway-1-nam",
            CategorySlug = "vpn-tien-ich-mang",
            Description = "Băng thông không giới hạn, kết nối 105 quốc gia, độ trễ thấp tối ưu cho game thủ và stream video 4K.",
            ImageUrl = "https://images.unsplash.com/photo-1550751827-4bd374c3f58b?w=600&auto=format&fit=crop&q=80",
            Sku = "EXPRESSVPN-1Y",
            VariantLabel = "1 Năm tốc độ cao",
            Price = 380000,
            ComparePrice = 850000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "surfshark",
            Name = "Bản quyền Surfshark VPN Không Giới Hạn Thiết Bị Đồng Thời (2 Năm)",
            Slug = "ban-quyen-surfshark-vpn-khong-gioi-han-2-nam",
            CategorySlug = "vpn-tien-ich-mang",
            Description = "Kết nối không giới hạn thiết bị, tích hợp CleanWeb chặn quảng cáo và mã độc từ máy chủ.",
            ImageUrl = "https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?w=600&auto=format&fit=crop&q=80",
            Sku = "SURFSHARK-2Y",
            VariantLabel = "2 Năm Unlimited Devices",
            Price = 340000,
            ComparePrice = 790000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "adguard",
            Name = "Key Bản Quyền AdGuard Premium Chặn Quảng Cáo Toàn Diện (Vĩnh Viễn / 3 Thiết Bị)",
            Slug = "key-ban-quyen-adguard-premium-vinh-vien-3-thiet-bi",
            CategorySlug = "vpn-tien-ich-mang",
            Description = "Chặn đứng mọi banner, quảng cáo YouTube, pop-up phiền toái trên Windows, Mac, Android và iOS.",
            ImageUrl = "https://images.unsplash.com/photo-1563089145-599997674d42?w=600&auto=format&fit=crop&q=80",
            Sku = "ADGUARD-LIFETIME-3PC",
            VariantLabel = "Vĩnh viễn 3 máy",
            Price = 190000,
            ComparePrice = 420000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "protonvpn",
            Name = "Tài khoản ProtonVPN Plus Bảo Mật Chuẩn Quân Đội Thụy Sĩ (1 Năm)",
            Slug = "tai-khoan-protonvpn-plus-thuy-si-1-nam",
            CategorySlug = "vpn-tien-ich-mang",
            Description = "Chính sách No-Logs tuyệt đối, bảo mật Secure Core định tuyến qua các hầm ngầm tại Thụy Sĩ và Iceland.",
            ImageUrl = "https://images.unsplash.com/photo-1510511459019-5dda7724fd87?w=600&auto=format&fit=crop&q=80",
            Sku = "PROTON-PLUS-1Y",
            VariantLabel = "1 Năm Secure Plus",
            Price = 270000,
            ComparePrice = 600000,
            DeliveryType = DeliveryType.CodePool
        },

        // ------------------ THỂ LOẠI 4: KEY BẢN QUYỀN PHẦN MỀM - HỆ ĐIỀU HÀNH ------------------
        new()
        {
            Key = "win11",
            Name = "Key Bản Quyền Windows 11 Pro 64-bit Retail Vĩnh Viễn",
            Slug = "key-ban-quyen-windows-11-pro-vinh-vien",
            CategorySlug = "he-dieu-hanh",
            Description = "Key bản quyền số Windows 11 Pro chính hãng Microsoft. Kích hoạt trực tiếp trong Settings, hỗ trợ cài lại Windows và nâng cấp cập nhật trọn đời máy.",
            ImageUrl = "https://images.unsplash.com/photo-1517694712202-14dd9538aa97?w=600&auto=format&fit=crop&q=80",
            Sku = "WIN11-PRO-RETAIL-LIFETIME",
            VariantLabel = "Vĩnh viễn (1 PC)",
            Price = 149000,
            ComparePrice = 350000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "win10",
            Name = "Key Bản Quyền Windows 10 Pro OEM Kích Hoạt Online (Vĩnh Viễn)",
            Slug = "key-ban-quyen-windows-10-pro-oem-vinh-vien",
            CategorySlug = "he-dieu-hanh",
            Description = "Kích hoạt bản quyền số Windows 10 Pro vĩnh viễn gắn liền bo mạch chủ, cập nhật vá lỗi bảo mật liên tục.",
            ImageUrl = "https://images.unsplash.com/photo-1525547719571-a2d4ac8945e2?w=600&auto=format&fit=crop&q=80",
            Sku = "WIN10-PRO-OEM-1PC",
            VariantLabel = "Vĩnh viễn OEM",
            Price = 129000,
            ComparePrice = 280000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "winserver",
            Name = "Key Bản Quyền Windows Server 2022 Standard (16 Core Bản Quyền Số)",
            Slug = "key-ban-quyen-windows-server-2022-standard",
            CategorySlug = "he-dieu-hanh",
            Description = "Bản quyền chuẩn doanh nghiệp cho máy chủ vật lý hoặc Hyper-V ảo hóa, kích hoạt trực tiếp từ Microsoft VLSC.",
            ImageUrl = "https://images.unsplash.com/photo-1558494949-ef010cbdcc31?w=600&auto=format&fit=crop&q=80",
            Sku = "WIN-SERVER-2022-STD",
            VariantLabel = "16 Core Enterprise",
            Price = 450000,
            ComparePrice = 1200000,
            DeliveryType = DeliveryType.CodePool
        },

        // ------------------ THỂ LOẠI 5: KEY BẢN QUYỀN - ỨNG DỤNG VĂN PHÒNG & ĐỒ HỌA ------------------
        new()
        {
            Key = "office365",
            Name = "Tài khoản Office 365 ProPlus Bản Quyền + 1TB OneDrive (1 Năm)",
            Slug = "tai-khoan-office-365-proplus-1tb-onedrive",
            CategorySlug = "ung-dung-van-phong",
            Description = "Bộ ứng dụng Microsoft Office 365 bản quyền (Word, Excel, PowerPoint...) kích hoạt tối đa 5 thiết bị (PC, Mac, iPad) kèm ổ cứng lưu trữ đám mây 1TB OneDrive tốc độ cao.",
            ImageUrl = "https://images.unsplash.com/photo-1542744094-3a31f272c490?w=600&auto=format&fit=crop&q=80",
            Sku = "OFFICE-365-1TB-1Y",
            VariantLabel = "1 Năm (5 thiết bị)",
            Price = 199000,
            ComparePrice = 450000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "office2021",
            Name = "Key Bản Quyền Microsoft Office 2021 Professional Plus (Vĩnh Viễn)",
            Slug = "key-ban-quyen-office-2021-pro-plus-vinh-vien",
            CategorySlug = "ung-dung-van-phong",
            Description = "Cài đặt và kích hoạt vĩnh viễn Word, Excel, PowerPoint, Outlook, Access 2021 không cần gia hạn hàng năm.",
            ImageUrl = "https://images.unsplash.com/photo-1531403009284-440f080d1e12?w=600&auto=format&fit=crop&q=80",
            Sku = "OFFICE-2021-PROPLUS-KEY",
            VariantLabel = "Vĩnh viễn 1 PC",
            Price = 299000,
            ComparePrice = 690000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "acrobat",
            Name = "Bản Quyền Adobe Acrobat Pro DC Ký & Biên Tập PDF Chuyên Nghiệp (1 Năm)",
            Slug = "ban-quyen-adobe-acrobat-pro-dc-1-nam",
            CategorySlug = "ung-dung-van-phong",
            Description = "Chỉnh sửa text, chèn chữ ký điện tử, xuất PDF sang Word/Excel và bảo mật tài liệu doanh nghiệp.",
            ImageUrl = "https://images.unsplash.com/photo-1586281380349-632531db7ed4?w=600&auto=format&fit=crop&q=80",
            Sku = "ADOBE-ACROBAT-PRO-1Y",
            VariantLabel = "1 Năm cá nhân",
            Price = 350000,
            ComparePrice = 750000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "idm",
            Name = "Key Bản Quyền Internet Download Manager (IDM) Chính Hãng (Trọn Đời)",
            Slug = "key-ban-quyen-idm-chinh-hang-tron-doi",
            CategorySlug = "ung-dung-van-phong",
            Description = "Tăng tốc độ tải file lên gấp 5 lần, tự động bắt link video, cập nhật phiên bản mới nhất vĩnh viễn.",
            ImageUrl = "https://images.unsplash.com/photo-1544197150-b99a580bb7a8?w=600&auto=format&fit=crop&q=80",
            Sku = "IDM-LIFETIME-1PC",
            VariantLabel = "Trọn đời 1 PC",
            Price = 399000,
            ComparePrice = 550000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "camtasia",
            Name = "Key Bản Quyền TechSmith Camtasia 2024 Quay Màn Hình & Dựng Video (Vĩnh Viễn)",
            Slug = "key-ban-quyen-camtasia-2024-vinh-vien",
            CategorySlug = "ung-dung-van-phong",
            Description = "Phần mềm biên tập video và quay màn hình trực quan hàng đầu, hỗ trợ hiệu ứng zoom pan, xuất video 4K 60fps.",
            ImageUrl = "https://images.unsplash.com/photo-1574717024653-61fd2cf4d44d?w=600&auto=format&fit=crop&q=80",
            Sku = "CAMTASIA-2024-KEY",
            VariantLabel = "Vĩnh viễn Windows/Mac",
            Price = 490000,
            ComparePrice = 1100000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "jetbrains",
            Name = "Key Bản Quyền JetBrains All Products Pack (1 Năm Giáo Dục / Cá Nhân)",
            Slug = "key-jetbrains-all-products-pack-1-nam",
            CategorySlug = "ung-dung-van-phong",
            Description = "Trọn bộ IDE lập trình đỉnh cao của JetBrains: IntelliJ IDEA Ultimate, Rider, PyCharm Pro, WebStorm, CLion, DataGrip... Kích hoạt chính chủ 1 năm.",
            ImageUrl = "https://images.unsplash.com/photo-1555066931-4365d14bab8c?w=600&auto=format&fit=crop&q=80",
            Sku = "JETBRAINS-ALL-1Y",
            VariantLabel = "1 Năm Full Suite",
            Price = 350000,
            ComparePrice = 690000,
            DeliveryType = DeliveryType.CodePool
        },

        // ------------------ THỂ LOẠI 6: KEY BẢN QUYỀN - BẢO MẬT & ANTIVIRUS ------------------
        new()
        {
            Key = "kaspersky",
            Name = "Key Bản Quyền Kaspersky Total Security Diệt Virus Đa Nền Tảng (1 Năm / 3 Máy)",
            Slug = "key-ban-quyen-kaspersky-total-security-1-nam-3-may",
            CategorySlug = "bao-mat-antivirus",
            Description = "Bảo vệ toàn diện trước mã độc tống tiền Ransomware, giao dịch ngân hàng an toàn Safe Money và tường lửa bảo mật.",
            ImageUrl = "https://images.unsplash.com/photo-1563986768494-4dee2763ff3f?w=600&auto=format&fit=crop&q=80",
            Sku = "KASPERSKY-TOTAL-3PC-1Y",
            VariantLabel = "1 Năm (3 Thiết bị)",
            Price = 210000,
            ComparePrice = 490000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "bitdefender",
            Name = "Key Bản Quyền Bitdefender Total Security Tường Lửa Thông Minh (1 Năm)",
            Slug = "key-ban-quyen-bitdefender-total-security-1-nam",
            CategorySlug = "bao-mat-antivirus",
            Description = "Phần mềm bảo mật xếp hạng số 1 thế giới bởi AV-Comparatives, nhẹ máy không gây lag khi chơi game.",
            ImageUrl = "https://images.unsplash.com/photo-1555949963-ff9fe0c870eb?w=600&auto=format&fit=crop&q=80",
            Sku = "BITDEFENDER-TOTAL-1Y",
            VariantLabel = "1 Năm bảo vệ",
            Price = 230000,
            ComparePrice = 550000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "malwarebytes",
            Name = "Key Bản Quyền Malwarebytes Premium Quét Mã Độc Chuyên Sâu (1 Năm)",
            Slug = "key-ban-quyen-malwarebytes-premium-1-nam",
            CategorySlug = "bao-mat-antivirus",
            Description = "Tiêu diệt tận gốc spyware, adware và các phần mềm độc hại cứng đầu mà các antivirus thông thường bỏ sót.",
            ImageUrl = "https://images.unsplash.com/photo-1614064641938-3bbee52942c7?w=600&auto=format&fit=crop&q=80",
            Sku = "MALWAREBYTES-PREM-1Y",
            VariantLabel = "1 Năm 1 PC",
            Price = 195000,
            ComparePrice = 420000,
            DeliveryType = DeliveryType.CodePool
        },

        // ------------------ THỂ LOẠI 7: KHÓA HỌC & TÀI LIỆU SỐ - LẬP TRÌNH & CNTT ------------------
        new()
        {
            Key = "dotnetcourse",
            Name = "Khóa học Fullstack .NET 8 & React VIP Masterclass (Video & Source)",
            Slug = "khoa-hoc-fullstack-dotnet-react-vip-masterclass",
            CategorySlug = "lap-trinh-cntt",
            Description = "Khóa học thực chiến xây dựng hệ thống Microservices, Clean Architecture, CQRS, SignalR realtime, Redis Caching và xác thực JWT hoàn chỉnh từ A đến Z.",
            ImageUrl = "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?w=600&auto=format&fit=crop&q=80",
            Sku = "COURSE-DOTNET8-REACT",
            VariantLabel = "Trọn đời + Hỗ trợ Q&A",
            Price = 499000,
            ComparePrice = 1200000,
            DeliveryType = DeliveryType.Course
        },
        new()
        {
            Key = "microservices",
            Name = "Khóa Học Thiết Kế Kiến Trúc Microservices Với Docker & Kubernetes (Thực Chiến)",
            Slug = "khoa-hoc-thiet-ke-kien-truc-microservices-docker-k8s",
            CategorySlug = "lap-trinh-cntt",
            Description = "Học cách thiết kế hệ thống chịu tải cao, Message Broker RabbitMQ, Kafka, API Gateway Ocelot và triển khai Kubernetes cluster.",
            ImageUrl = "https://images.unsplash.com/photo-1607799279861-4dd421887fb3?w=600&auto=format&fit=crop&q=80",
            Sku = "COURSE-K8S-MICROSERVICES",
            VariantLabel = "Trọn đời + Lab thực hành",
            Price = 590000,
            ComparePrice = 1500000,
            DeliveryType = DeliveryType.Course
        },
        new()
        {
            Key = "devops",
            Name = "Khóa Học Thực Chiến DevOps, CI/CD Pipeline & Triển Khai AWS Cloud Toàn Diện",
            Slug = "khoa-hoc-thuc-chien-devops-cicd-aws-cloud",
            CategorySlug = "lap-trinh-cntt",
            Description = "Nắm vững GitHub Actions, GitLab CI, Terraform IaC, AWS ECS, EKS và giám sát hệ thống với Prometheus Grafana.",
            ImageUrl = "https://images.unsplash.com/photo-1667372393119-3d4c48d07fc9?w=600&auto=format&fit=crop&q=80",
            Sku = "COURSE-DEVOPS-AWS",
            VariantLabel = "Khóa học Video 4K + Tài liệu",
            Price = 650000,
            ComparePrice = 1600000,
            DeliveryType = DeliveryType.Course
        },

        // ------------------ THỂ LOẠI 8: KHÓA HỌC & TÀI LIỆU SỐ - NGOẠI NGỮ ------------------
        new()
        {
            Key = "ielts",
            Name = "Bộ Video & Tài Liệu Luyện Thi IELTS 8.0 Listening & Reading Masterclass",
            Slug = "bo-video-tai-lieu-luyen-thi-ielts-8-0-masterclass",
            CategorySlug = "ngoai-ngu",
            Description = "Trọn bộ 500 bài giảng chuyên sâu từ các cựu giám khảo chấm thi IDP/British Council kèm 100 đề thi mẫu sát thực tế.",
            ImageUrl = "https://images.unsplash.com/photo-1434030216411-0b793f4b4173?w=600&auto=format&fit=crop&q=80",
            Sku = "IELTS-80-MASTERCLASS",
            VariantLabel = "Trọn bộ tài liệu số",
            Price = 320000,
            ComparePrice = 800000,
            DeliveryType = DeliveryType.Course
        },

        // ------------------ THỂ LOẠI 9: SOURCE CODE & TEMPLATE - WEBSITE ------------------
        new()
        {
            Key = "sourceclean",
            Name = "Source Code Mạng Xã Hội Microservices Clean Architecture .NET & React",
            Slug = "source-code-mang-xa-hoi-clean-architecture",
            CategorySlug = "website-web-app",
            Description = "Mã nguồn hoàn chỉnh 100% của nền tảng mạng xã hội kết hợp thương mại điện tử hàng số. Đầy đủ phân quyền, cổng thanh toán VietQR, AI gợi ý nội dung.",
            ImageUrl = "https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?w=600&auto=format&fit=crop&q=80",
            Sku = "SRC-SOCIAL-NET8",
            VariantLabel = "Source code Full Commercial",
            Price = 890000,
            ComparePrice = 2500000,
            DeliveryType = DeliveryType.CodePool
        },
        new()
        {
            Key = "sourcenextjs",
            Name = "Template Dashboard Quản Trị Doanh Nghiệp Next.js 14, Tailwind & Shadcn UI",
            Slug = "template-dashboard-quan-tri-nextjs-14-shadcn",
            CategorySlug = "website-web-app",
            Description = "Giao diện Dashboard cao cấp hỗ trợ Dark Mode, Dark/Light theme, biểu đồ Chart.js trực quan, Authentication NextAuth.js.",
            ImageUrl = "https://images.unsplash.com/photo-1507238691740-187a5b1d37b8?w=600&auto=format&fit=crop&q=80",
            Sku = "SRC-NEXTJS-DASHBOARD",
            VariantLabel = "Full Source code + Figma",
            Price = 450000,
            ComparePrice = 1200000,
            DeliveryType = DeliveryType.CodePool
        },

        // ------------------ THỂ LOẠI 10: SOURCE CODE & TEMPLATE - MOBILE APP ------------------
        new()
        {
            Key = "sourcemobile",
            Name = "Source Code App Thương Mại Điện Tử React Native Expo & Node.js API",
            Slug = "source-code-app-thuong-mai-dien-tu-react-native-expo",
            CategorySlug = "mobile-app",
            Description = "Mã nguồn ứng dụng bán hàng trên iOS và Android, tích hợp thanh toán thẻ, push notification Firebase và giỏ hàng thời gian thực.",
            ImageUrl = "https://images.unsplash.com/photo-1512941937669-90a1b58e7e9c?w=600&auto=format&fit=crop&q=80",
            Sku = "SRC-RN-ECOMMERCE",
            VariantLabel = "Full Mobile App Source",
            Price = 680000,
            ComparePrice = 1800000,
            DeliveryType = DeliveryType.CodePool
        },

        // ------------------ THỂ LOẠI 11: SOURCE CODE & TEMPLATE - BOT TỰ ĐỘNG ------------------
        new()
        {
            Key = "sourcebot",
            Name = "Bot Tự Động Hóa Cảnh Báo Tín Hiệu Giao Dịch Crypto & Chứng Khoán Telegram",
            Slug = "bot-tu-dong-hoa-canh-bao-crypto-telegram",
            CategorySlug = "bot-script-tu-dong",
            Description = "Bot NodeJS kết nối Binance WebSocket & TradingView webhook, tự động quét chỉ báo RSI, MACD và gửi thông báo tín hiệu mua bán realtime.",
            ImageUrl = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=600&auto=format&fit=crop&q=80",
            Sku = "SRC-TELEGRAM-BOT-TRADING",
            VariantLabel = "Source code + Hướng dẫn setup",
            Price = 390000,
            ComparePrice = 950000,
            DeliveryType = DeliveryType.CodePool
        }
    };

    private static async Task<Dictionary<string, (Product Product, ProductVariant Variant)>> EnsureSeedProductsAsync(
        SocialDbContext context,
        Shop partnerShop,
        Shop? userShop,
        ICryptoService cryptoService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, (Product Product, ProductVariant Variant)>();
        var allCategories = await context.Categories.ToListAsync(cancellationToken);

        foreach (var spec in AllProductSpecs)
        {
            var existingProduct = await context.Products
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Slug == spec.Slug, cancellationToken);

            var category = allCategories.FirstOrDefault(c => c.Slug == spec.CategorySlug)
                ?? allCategories.FirstOrDefault(c => c.Name.Contains("Tài khoản") || c.Name.Contains("Key"))
                ?? allCategories.FirstOrDefault();

            var assignShop = (spec.Key.StartsWith("source") && userShop != null) ? userShop : partnerShop;

            if (existingProduct == null)
            {
                existingProduct = new Product(
                    shopId: assignShop.Id,
                    name: spec.Name,
                    slug: spec.Slug,
                    categoryId: category?.Id,
                    description: spec.Description
                );
                existingProduct.Publish();
                existingProduct.UpdatePriceCache(spec.Price, spec.Price);
                existingProduct.UpdateRating(4.9, 142);
                context.Products.Add(existingProduct);
                await context.SaveChangesAsync(cancellationToken);

                var variant = new ProductVariant(
                    productId: existingProduct.Id,
                    shopId: assignShop.Id,
                    sku: spec.Sku,
                    combinationKey: "default",
                    price: spec.Price,
                    compareAtPrice: spec.ComparePrice,
                    deliveryType: spec.DeliveryType,
                    trackInventory: true
                );
                context.ProductVariants.Add(variant);

                var img = new ProductImage(existingProduct.Id, spec.ImageUrl, 0, isCover: true);
                context.ProductImages.Add(img);

                await context.SaveChangesAsync(cancellationToken);
                result[spec.Key] = (existingProduct, variant);
            }
            else
            {
                var variant = existingProduct.Variants.FirstOrDefault()
                    ?? await context.ProductVariants.FirstOrDefaultAsync(v => v.ProductId == existingProduct.Id, cancellationToken);

                if (variant == null)
                {
                    variant = new ProductVariant(
                        productId: existingProduct.Id,
                        shopId: existingProduct.ShopId,
                        sku: spec.Sku,
                        combinationKey: "default",
                        price: spec.Price,
                        compareAtPrice: spec.ComparePrice,
                        deliveryType: spec.DeliveryType,
                        trackInventory: true
                    );
                    context.ProductVariants.Add(variant);
                    await context.SaveChangesAsync(cancellationToken);
                }

                // Cập nhật hoặc đảm bảo ảnh đại diện sản phẩm luôn hợp lệ và không bị 404
                var coverImg = existingProduct.Images.FirstOrDefault(i => i.IsCover)
                    ?? await context.ProductImages.FirstOrDefaultAsync(i => i.ProductId == existingProduct.Id && i.IsCover, cancellationToken);

                if (coverImg == null)
                {
                    coverImg = new ProductImage(existingProduct.Id, spec.ImageUrl, 0, isCover: true);
                    context.ProductImages.Add(coverImg);
                    await context.SaveChangesAsync(cancellationToken);
                }
                else if (coverImg.Url.Contains("photo-1677442136019") || string.IsNullOrWhiteSpace(coverImg.Url))
                {
                    context.ProductImages.Remove(coverImg);
                    await context.SaveChangesAsync(cancellationToken);
                    var newImg = new ProductImage(existingProduct.Id, spec.ImageUrl, 0, isCover: true);
                    context.ProductImages.Add(newImg);
                    await context.SaveChangesAsync(cancellationToken);
                }

                result[spec.Key] = (existingProduct, variant);
            }
        }

        // Đảm bảo mỗi biến thể luôn có sẵn ít nhất 15 mã trong kho để người dùng có thể mua được trực tiếp
        foreach (var entry in result.Values)
        {
            var variant = entry.Variant;
            var availableCodes = await context.DigitalCodes.CountAsync(
                c => c.VariantId == variant.Id && c.Status == DigitalCodeStatus.Available,
                cancellationToken);

            if (availableCodes < 15)
            {
                var codesToAdd = 15 - availableCodes;
                for (int i = 1; i <= codesToAdd; i++)
                {
                    var rawCode = $"{variant.Sku}-KEY-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
                    var enc = cryptoService.Encrypt(rawCode);
                    var hash = cryptoService.ComputeHash(rawCode);
                    var masked = cryptoService.MaskValue(rawCode);
                    var code = new DigitalCode(variant.ShopId, variant.Id, enc, hash, masked);
                    context.DigitalCodes.Add(code);
                }
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        return result;
    }

    private static async Task<int> CreateSeedOrdersAsync(
        SocialDbContext context,
        User buyerUser,
        Shop partnerShop,
        Shop? userShop,
        Dictionary<string, (Product Product, ProductVariant Variant)> products,
        ICryptoService cryptoService,
        ILogger logger,
        int targetTotalOrders,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var specKeys = AllProductSpecs.Select(s => s.Key).ToList();
        int count = 0;

        for (int i = 1; i <= targetTotalOrders; i++)
        {
            var orderNo = $"ORD-202610-{(i < 10 ? "00" + i : i < 100 ? "0" + i : i.ToString())}";

            // Kiểm tra xem mã đơn này đã tồn tại chưa
            var existingOrder = await context.ShopOrders.FirstOrDefaultAsync(
                o => o.OrderNo == orderNo,
                cancellationToken);

            if (existingOrder != null)
            {
                continue;
            }

            // Chọn sản phẩm xoay vòng theo danh sách để bao phủ toàn bộ các thể loại
            var productKey = specKeys[(i - 1) % specKeys.Count];
            if (!products.TryGetValue(productKey, out var prodInfo))
            {
                continue;
            }

            var product = prodInfo.Product;
            var variant = prodInfo.Variant;
            var shopId = product.ShopId;

            // Tính toán thời gian tạo đơn trải dài trong 90 ngày qua
            // 75% đơn hoàn tất (Completed), 18% đơn mới giao (Delivered), 7% đơn hủy (Cancelled)
            OrderStatus status;
            DateTime orderDate;

            if (i % 15 == 0)
            {
                status = OrderStatus.Cancelled;
                orderDate = now.AddDays(-1 * (i % 60 + 5)).AddHours(-1 * (i % 24));
            }
            else if (i % 6 == 0 || i > 85)
            {
                status = OrderStatus.Delivered;
                orderDate = now.AddHours(-1 * (i % 36 + 1));
            }
            else
            {
                status = OrderStatus.Completed;
                orderDate = now.AddDays(-1 * (i % 80 + 3)).AddHours(-1 * (i % 24));
            }

            // Sinh thông tin mã số / tài khoản bàn giao số thực tế
            string plainCredential = GenerateDeliveryCredential(productKey, variant.Sku, i);

            // A. Khởi tạo Checkout
            var checkoutNo = $"CHK-{orderNo[4..]}";
            var total = variant.Price;
            var checkout = new Checkout(
                checkoutNo: checkoutNo,
                buyerUserId: buyerUser.Id,
                itemsTotal: total,
                discountTotal: 0,
                grandTotal: total,
                expiresAtUtc: orderDate.AddMinutes(15),
                idempotencyKey: Guid.NewGuid().ToString("N")
            );
            checkout.MarkPaid();
            context.Checkouts.Add(checkout);
            await context.SaveChangesAsync(cancellationToken);

            // B. Thêm CheckoutItem
            var commissionRateBp = 500; // 5%
            var commissionAmount = (long)(total * 0.05);
            var shopNet = total - commissionAmount;

            var checkoutItem = new CheckoutItem(
                checkoutId: checkout.Id,
                shopId: shopId,
                variantId: variant.Id,
                productId: product.Id,
                categoryId: product.CategoryId,
                quantity: 1,
                unitPrice: variant.Price,
                lineTotal: total,
                productNameSnapshot: product.Name,
                variantLabelSnapshot: variant.Sku,
                skuSnapshot: variant.Sku,
                commissionRateBp: commissionRateBp,
                commissionAmount: commissionAmount,
                shopNet: shopNet,
                deliveryType: variant.DeliveryType
            );
            context.CheckoutItems.Add(checkoutItem);

            // C. Khởi tạo Payment
            var payment = new Payment(
                checkoutId: checkout.Id,
                provider: PaymentProvider.PayOs,
                providerTxnId: $"PAYOS_{orderNo}",
                amount: total,
                method: "VietQR_NAPAS247",
                currency: "VND"
            );
            payment.MarkSucceeded(orderDate.AddMinutes(1));
            context.Payments.Add(payment);

            // D. Khởi tạo ShopOrder
            var complaintWindow = status == OrderStatus.Delivered ? now.AddDays(2) : orderDate.AddDays(2);
            var shopOrder = new ShopOrder(
                orderNo: orderNo,
                checkoutId: checkout.Id,
                shopId: shopId,
                buyerUserId: buyerUser.Id,
                total: total,
                shopGross: total,
                commissionTotal: commissionAmount,
                shopNet: shopNet,
                deliveredAtUtc: orderDate.AddMinutes(2),
                complaintWindowEndsAtUtc: complaintWindow
            );

            if (status == OrderStatus.Completed)
            {
                shopOrder.MarkCompleted();
            }
            else if (status == OrderStatus.Cancelled)
            {
                shopOrder.MarkCancelled();
            }

            context.ShopOrders.Add(shopOrder);
            await context.SaveChangesAsync(cancellationToken);

            // E. Thêm OrderItem
            var orderItem = new OrderItem(
                shopOrderId: shopOrder.Id,
                variantId: variant.Id,
                productId: product.Id,
                categoryId: product.CategoryId,
                productName: product.Name,
                variantLabel: variant.Sku,
                sku: variant.Sku,
                unitPrice: variant.Price,
                quantity: 1,
                lineTotal: total,
                shopGross: total,
                commissionRateBp: commissionRateBp,
                commissionAmount: commissionAmount,
                shopNet: shopNet,
                deliveryType: variant.DeliveryType
            );
            context.OrderItems.Add(orderItem);
            await context.SaveChangesAsync(cancellationToken);

            // F. Lịch sử trạng thái đơn hàng (Audit trail)
            var history1 = new OrderStatusHistory(
                shopOrderId: shopOrder.Id,
                fromStatus: null,
                toStatus: OrderStatus.Delivered,
                actorType: OrderActorType.System,
                reason: "Thanh toán VietQR thành công, hệ thống xuất kho hàng số tự động."
            );
            context.OrderStatusHistories.Add(history1);

            if (status == OrderStatus.Completed)
            {
                var history2 = new OrderStatusHistory(
                    shopOrderId: shopOrder.Id,
                    fromStatus: OrderStatus.Delivered,
                    toStatus: OrderStatus.Completed,
                    actorType: OrderActorType.System,
                    reason: "Đơn hàng đã hết thời hạn khiếu nại 48h, hệ thống tự động hoàn tất và quyết toán."
                );
                context.OrderStatusHistories.Add(history2);
            }
            else if (status == OrderStatus.Cancelled)
            {
                var history2 = new OrderStatusHistory(
                    shopOrderId: shopOrder.Id,
                    fromStatus: OrderStatus.Delivered,
                    toStatus: OrderStatus.Cancelled,
                    actorType: OrderActorType.Buyer,
                    reason: "Khách hàng gửi yêu cầu hủy đơn hàng và hệ thống đã hoàn tiền đầy đủ."
                );
                context.OrderStatusHistories.Add(history2);
            }

            // G. Xuất giao hàng số (Fulfillment & FulfillmentItem)
            var fulfillment = new Fulfillment(shopOrder.Id, FulfillmentStatus.Fulfilled);
            context.Fulfillments.Add(fulfillment);
            await context.SaveChangesAsync(cancellationToken);

            var fulfillmentItem = new FulfillmentItem(fulfillment.Id, orderItem.Id, 1);
            context.FulfillmentItems.Add(fulfillmentItem);

            // H. Thêm DigitalCode bảo mật với mã hóa AES-GCM
            var encrypted = cryptoService.Encrypt(plainCredential);
            var hash = cryptoService.ComputeHash(plainCredential);
            var masked = cryptoService.MaskValue(plainCredential);

            var digitalCode = new DigitalCode(shopId, variant.Id, encrypted, hash, masked);
            digitalCode.MarkDelivered(orderItem.Id);
            context.DigitalCodes.Add(digitalCode);

            await context.SaveChangesAsync(cancellationToken);
            count++;
        }

        return count;
    }

    private static string GenerateDeliveryCredential(string key, string sku, int index)
    {
        return key switch
        {
            "netflix" => $"TÀI KHOẢN NETFLIX 4K ULTRA (HỘ GIA ĐÌNH):\nEmail: netflix.stream.{index}@premiumstream.io\nMật khẩu: StreamNetVip{index}!@#\nProfile: Slot {index % 4 + 1} (Mã PIN: {1000 + index})\nLưu ý: Không đổi email/mật khẩu, xem đúng profile của bạn.",
            "spotify" => $"LIÊN KẾT GIA HẠN SPOTIFY PREMIUM CHÍNH CHỦ:\nhttps://www.spotify.com/vn-vi/family/join/invite/SpotifyVipToken{index}{Guid.NewGuid().ToString("N")[..6]}\nĐịa chỉ xác thực: 128 Nguyễn Trãi, Thanh Xuân, Hà Nội",
            "youtube" => $"LỜI MỜI GIA HẠN YOUTUBE PREMIUM FAMILY:\nhttps://families.google.com/join/invite/{Guid.NewGuid().ToString("N")[..16]}\nChấp nhận thư mời từ tài khoản Google của bạn để nhận 1 năm Premium.",
            "disney" => $"TÀI KHOẢN DISNEY+ HOTSTAR VIP 4K:\nEmail: disney.vip.{index}@streamfast.net\nMật khẩu: DisneyHotstarPass{index}!\nXem mượt mà trên Smart TV và điện thoại.",
            "applemusic" => $"MÃ QUÀ TẶNG APPLE MUSIC 1 NĂM:\nAPPL-MUS-VN-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}\nNhập mã tại App Store -> Đổi mã quà tặng hoặc mã khuyến mại.",
            "hbomax" => $"TÀI KHOẢN MAX (HBO MAX) 4K HDR:\nEmail: max.streamer.{index}@hbomedia.org\nMật khẩu: HboMaxUltra{index}#\nProfile riêng: Khách hàng {index}",
            "chatgpt" => $"TÀI KHOẢN CHATGPT PLUS (GPT-4o & CANVAS):\nEmail: openai.vip.{index}@aihub.co\nMật khẩu: OpenAIPlus2026@{index}\nĐã kích hoạt gói 20$/tháng. Hỗ trợ voice mode và data analysis.",
            "claude" => $"TÀI KHOẢN CLAUDE PRO ANTHROPIC:\nEmail: claude.pro.{index}@anthropic-ai.org\nMật khẩu: ClaudeProVip{index}$!\nGói Pro không giới hạn tokens, truy cập Claude 3.5 Sonnet Artifacts.",
            "midjourney" => $"TÀI KHOẢN DISCORD MIDJOURNEY STANDARD:\nEmail: midjourney.art.{index}@creatives.io\nMật khẩu: MidArtGen2026@{index}\nTruy cập bot @Midjourney để tạo ảnh 15h Fast Hours.",
            "canva" => $"LIÊN KẾT THAM GIA CANVA PRO EDU VĨNH VIỄN:\nhttps://www.canva.com/brand/join?token={Guid.NewGuid().ToString("N")}\nTruy cập link để tự động kích hoạt gói Pro cho tài khoản cá nhân.",
            "grammarly" => $"TÀI KHOẢN GRAMMARLY PREMIUM BUSINESS:\nEmail: grammarly.vip.{index}@writewell.net\nMật khẩu: GrammarProPass{index}!\nĐăng nhập extension trình duyệt để sửa lỗi và paraphrase văn bản.",
            "notion" => $"LIÊN KẾT KÍCH HOẠT NOTION AI PLUS:\nhttps://www.notion.so/invite/{Guid.NewGuid().ToString("N")[..20]}\nĐã kích hoạt Notion AI không giới hạn số lượng prompts.",
            "duolingo" => $"MÃ KÍCH HOẠT DUOLINGO SUPER 1 NĂM:\nDUO-SUPER-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}\nKích hoạt tại: https://www.duolingo.com/redeem",
            "coursera" => $"TÀI KHOẢN COURSERA PLUS CHÍNH CHỦ:\nEmail: coursera.student.{index}@eduglobal.org\nMật khẩu: CourseraLearnPass{index}!\nTruy cập hơn 7000 khóa học và nhận chứng chỉ không giới hạn.",
            "nordvpn" => $"TÀI KHOẢN NORDVPN PREMIER 1 NĂM:\nEmail: nordvpn.user.{index}@vpnshield.net\nMật khẩu: ShieldVpn2026#{index}\nSử dụng tối đa 10 thiết bị cùng lúc, hỗ trợ Double VPN.",
            "expressvpn" => $"MÃ KÍCH HOẠT EXPRESSVPN 1 NĂM:\nEXPR-{Guid.NewGuid().ToString("N")[..16].ToUpperInvariant()}\nDán mã kích hoạt trực tiếp vào ứng dụng ExpressVPN trên PC/Điện thoại.",
            "surfshark" => $"TÀI KHOẢN SURFSHARK VPN UNLIMITED:\nEmail: surfshark.{index}@safeconnect.io\nMật khẩu: SurfSafe2026@{index}\nKhông giới hạn số lượng thiết bị kết nối.",
            "adguard" => $"KEY BẢN QUYỀN ADGUARD PREMIUM 3 PC:\nADG-PRO-{Guid.NewGuid().ToString("N")[..16].ToUpperInvariant()}\nVào AdGuard -> Cấp phép -> Nhập mã kích hoạt ở trên.",
            "protonvpn" => $"TÀI KHOẢN PROTONVPN PLUS THỤY SĨ:\nEmail: proton.member.{index}@protonmail.ch\nMật khẩu: SwissSecure{index}!\nTốc độ 10Gbps, kết nối máy chủ ngầm Thụy Sĩ.",
            "win11" => $"KEY BẢN QUYỀN WINDOWS 11 PRO (RETAIL):\nVK7JG-NPHTM-C97JM-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}\nHướng dẫn kích hoạt: Settings -> System -> Activation -> Change product key.",
            "win10" => $"KEY BẢN QUYỀN WINDOWS 10 PRO OEM:\nW269N-WFGWX-YVC9B-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}\nKích hoạt trực tuyến bản quyền số gắn vĩnh viễn theo mainboard.",
            "winserver" => $"KEY BẢN QUYỀN WINDOWS SERVER 2022 STD:\nWX4NM-KYWYW-QJJR4-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}\nBản quyền số 16 Cores cho máy chủ Hyper-V và vật lý.",
            "office365" => $"TÀI KHOẢN MICROSOFT 365 + 1TB ONEDRIVE:\nEmail: thanhdat.{index}@ms365cloud.org\nMật khẩu khởi tạo: OfficeVipPass#{1000 + index}\nĐăng nhập tại: https://portal.office.com và đổi mật khẩu lần đầu.",
            "office2021" => $"KEY BẢN QUYỀN OFFICE 2021 PRO PLUS:\nFXYTK-NJ27C-K2269-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}\nCài đặt từ setup.office.com và nhập key để kích hoạt vĩnh viễn.",
            "acrobat" => $"TÀI KHOẢN ADOBE ACROBAT PRO DC:\nEmail: adobe.acrobat.{index}@creativesuite.org\nMật khẩu: AdobePdfPass{index}!\nĐăng nhập Adobe Creative Cloud để sử dụng full tính năng.",
            "idm" => $"SERIAL KEY BẢN QUYỀN IDM TRỌN ĐỜI:\nIDM-VIP-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}\nTên đăng ký: Thanh Dat\nNhập key trong mục Registration -> Registration.",
            "camtasia" => $"SOFTWARE KEY TECHSMITH CAMTASIA 2024:\nCAMT-2024-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}\nKích hoạt vĩnh viễn không giới hạn watermark.",
            "jetbrains" => $"MÃ KÍCH HOẠT JETBRAINS ALL PRODUCTS PACK:\nJB-LIC-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}\nHỗ trợ full IDE: IntelliJ, Rider, PyCharm, WebStorm, CLion, DataGrip.",
            "kaspersky" => $"MÃ KÍCH HOẠT KASPERSKY TOTAL SECURITY 3PC:\nKASP-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}\nNhập trong ứng dụng Kaspersky để nhận 365 ngày bản quyền.",
            "bitdefender" => $"MÃ BẢN QUYỀN BITDEFENDER TOTAL SECURITY:\nBD-SEC-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}\nKích hoạt qua cổng Bitdefender Central để bảo vệ thiết bị.",
            "malwarebytes" => $"KEY BẢN QUYỀN MALWAREBYTES PREMIUM:\nMBAM-ID: {Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()} | KEY: {Guid.NewGuid().ToString("N")[..16].ToUpperInvariant()}\nBảo vệ realtime khỏi mã độc và ransomware.",
            "dotnetcourse" => $"KHÓA HỌC FULLSTACK .NET 8 & REACT VIP MASTERCLASS:\nLink Google Drive Video 4K & Mã nguồn bài tập: https://drive.google.com/drive/folders/fullstack-dotnet8-react-masterclass-{index}\nKey mời Discord VIP hỏi đáp 1-1: DISCORD-VIP-NET8-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            "microservices" => $"KHÓA HỌC MICROSERVICES VỚI DOCKER & KUBERNETES:\nLink kho tài liệu & Video thực hành bài lab: https://drive.google.com/drive/folders/k8s-microservices-masterclass-{index}\nMã kích hoạt quyền xem video không giới hạn: K8S-LAB-KEY-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            "devops" => $"KHÓA HỌC DEVOPS, CI/CD PIPELINE & AWS CLOUD:\nLink Google Drive giáo trình chuẩn quốc tế: https://drive.google.com/drive/folders/devops-aws-cicd-course-{index}\nKèm bộ template Terraform và GitHub Actions workflow thực chiến.",
            "ielts" => $"BỘ TÀI LIỆU SỐ LUYỆN THI IELTS 8.0 MASTERCLASS:\nLink tải trọn bộ 500 bài giảng Audio & PDF: https://drive.google.com/drive/folders/ielts-80-masterclass-materials-{index}\nKèm 100 đề thi thật Cambridge có giải thích chi tiết.",
            "sourceclean" => $"SOURCE CODE SOCIAL COMMERCE CLEAN ARCHITECTURE:\nLink kho mã nguồn GitHub Private: https://github.com/social-platform/social-commerce-clean-{index}\nToken truy cập cá nhân: ghp_SocialDevVipTokenPrivate{Guid.NewGuid().ToString("N")[..18]}",
            "sourcenextjs" => $"SOURCE CODE DASHBOARD NEXT.JS 14 & SHADCN UI:\nLink kho mã nguồn GitHub Private: https://github.com/ui-templates/nextjs14-enterprise-dashboard-{index}\nKèm file thiết kế Figma bản thương mại đầy đủ components.",
            "sourcemobile" => $"SOURCE CODE APP E-COMMERCE REACT NATIVE EXPO:\nLink kho mã nguồn GitHub Private: https://github.com/mobile-apps/ecommerce-react-native-expo-{index}\nĐầy đủ cấu hình build iOS và Android APK / AAB.",
            "sourcebot" => $"SOURCE CODE BOT TELEGRAM SIGNAL TRADING:\nLink kho mã nguồn GitHub Private: https://github.com/crypto-bots/telegram-signal-alert-{index}\nKèm file .env mẫu và script deploy Docker một chạm.",
            _ => $"{sku}-LICENSE-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}"
        };
    }
}
