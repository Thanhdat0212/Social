using Application.Common;
using Domain.Entities.Marketplace.Categories;
using Domain.Entities.Marketplace.Inventory;
using Domain.Entities.Marketplace.Products;
using Domain.Entities.Marketplace.Shops;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

/// <summary>
/// Bộ nạp dữ liệu cào trực tiếp từ sàn dịch vụ số TapHoaMMO (taphoammo.vn)
/// Bao gồm các thể loại: Tài khoản MMO, Phần mềm bản quyền, Tool tự động & Workflow n8n
/// </summary>
public static class TapHoaMmoSeeder
{
    public class MmoProductItem
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string CategorySlug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public long Price { get; set; }
        public long ComparePrice { get; set; }
    }

    private static readonly List<MmoProductItem> Items = new()
    {
        new()
        {
            Name = "Tài khoản Muse AI có sẵn 1 tỷ token – cấp sẵn, dùng riêng",
            Slug = "tai-khoan-muse-ai-co-san-1-ty-token-cap-san-dung-rieng",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Sản phẩm Tài khoản Muse AI có sẵn 1 tỷ token – cấp sẵn, dùng riêng chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/up/2026/10/tai-khoan-muse-ai-co-san-1-ty-token-cap-san-dung-rieng-7eebd3-400.webp",
            Sku = "MMO-001-TAIKHOAN",
            Price = 60000,
            ComparePrice = 90000
        },
        new()
        {
            Name = "Dịch Vụ Đăng Ký Tên Miền Cho Doanh Nghiệp – .vn · .com · .net · .org",
            Slug = "dich-vu-dang-ky-ten-mien-cho-doanh-nghiep-vn-com-net-org",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Dịch Vụ Đăng Ký Tên Miền Cho Doanh Nghiệp – .vn · .com · .net · .org chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/dich-vu-dang-ky-ten-mien-cho-doanh-nghiep-vn-·-com-·-net-·-o-3e004daa-400.webp",
            Sku = "MMO-002-DICHVUDA",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Dịch Vụ Phục Hồi Dữ Liệu Ổ Cứng Chuyên Nghiệp",
            Slug = "dich-vu-phuc-hoi-du-lieu-o-cung-chuyen-nghiep",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Dịch Vụ Phục Hồi Dữ Liệu Ổ Cứng Chuyên Nghiệp chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/dich-vu-phuc-hoi-du-lieu-o-cung-chuyen-nghiep-e52c8b03-400.webp",
            Sku = "MMO-003-DICHVUPH",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Web Hosting NVMe Tốc Độ Cao – Moma Stack",
            Slug = "web-hosting-nvme-toc-do-cao-moma-stack",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Web Hosting NVMe Tốc Độ Cao – Moma Stack chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/xanh-tim-hien-dai-tuong-lai-hinh-anh-sale-deal-uu-dai-san-pham-do-cong-nghe-inst-15-400.webp",
            Sku = "MMO-004-WEBHOSTIN",
            Price = 35000,
            ComparePrice = 52500
        },
        new()
        {
            Name = "GPU Cho AI Nhẹ – Chatbot · Sinh Ảnh · Inference (Moma Forge)",
            Slug = "moma-forge-gpu-cho-ai-nhe-chatbot-sinh-anh-inference",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Sản phẩm GPU Cho AI Nhẹ – Chatbot · Sinh Ảnh · Inference (Moma Forge) chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/xanh-tim-hien-dai-tuong-lai-hinh-anh-sale-deal-uu-dai-san-pham-do-cong-nghe-inst-17-400.webp",
            Sku = "MMO-005-MOMAFORGE",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Chatbot Builder Lite (Tạo chatbot tự động không cần lập trình)",
            Slug = "chatbot-builder-lite-tao-chatbot-tu-dong-khong-can-lap-trinh",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Chatbot Builder Lite (Tạo chatbot tự động không cần lập trình) chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/chatbot-builder-lite-tao-chatbot-tu-dong-khong-can-lap-trinh-f728b491-400.webp",
            Sku = "MMO-006-CHATBOTBU",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Hỗ Trợ WordPress Cơ Bản Trong Từ 24 Đến 48 Tiếng",
            Slug = "ho-tro-wordpress-co-ban-trong-tu-24-tieng-den-48-tieng",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Hỗ Trợ WordPress Cơ Bản Trong Từ 24 Đến 48 Tiếng chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/ho-tro-wordpress-co-ban-trong-tu-24-tieng-den-48-tieng-ea53239f-400.webp",
            Sku = "MMO-007-HOTROWOR",
            Price = 300000,
            ComparePrice = 450000
        },
        new()
        {
            Name = "Dịch Vụ Email Doanh Nghiệp Theo Tên Miền",
            Slug = "dich-vu-email-doanh-nghiep-theo-ten-mien",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Sản phẩm Dịch Vụ Email Doanh Nghiệp Theo Tên Miền chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/dich-vu-email-doanh-nghiep-theo-ten-mien-8cf3f84e-400.webp",
            Sku = "MMO-008-DICHVUEM",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Moma CRM – Phần mềm quản lý doanh nghiệp: đơn hàng, gia hạn, CSKH, chấm công đội nhóm",
            Slug = "moma-crm-phan-mem-quan-ly-shop-mmo-don-hang-gia-han-cskh-cham-cong-doi-nhom",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Moma CRM – Phần mềm quản lý doanh nghiệp: đơn hàng, gia hạn, CSKH, chấm công đội nhóm chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/up/2026/10/moma-crm-phan-mem-quan-ly-shop-mmo-don-hang-gia-han-cskh-cham-cong-doi-3d11c4-400.webp",
            Sku = "MMO-009-MOMACRMP",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Moma Pilot — Quản trị, cài đặt và chăm sóc máy chủ trọn gói",
            Slug = "moma-pilot-quan-tri-may-chu-tron-goi",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Moma Pilot — Quản trị, cài đặt và chăm sóc máy chủ trọn gói chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/moma-pilot-quan-tri-may-chu-tron-goi-d9d39456-400.webp",
            Sku = "MMO-010-MOMAPILOT",
            Price = 150000,
            ComparePrice = 225000
        },
        new()
        {
            Name = "Moma WP — Hosting WordPress cài sẵn, lên web trong vài phút",
            Slug = "moma-wp-hosting-wordpress-cai-san",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Moma WP — Hosting WordPress cài sẵn, lên web trong vài phút chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/moma-wp-hosting-wordpress-cai-san-b21cf3da-400.webp",
            Sku = "MMO-011-MOMAWPHO",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Moma Vault — Lưu trữ dung lượng lớn cho backup và dữ liệu",
            Slug = "moma-vault-luu-tru-dung-luong-lon",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Moma Vault — Lưu trữ dung lượng lớn cho backup và dữ liệu chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/moma-vault-luu-tru-dung-luong-lon-9be7555d-400.webp",
            Sku = "MMO-012-MOMAVAULT",
            Price = 150000,
            ComparePrice = 225000
        },
        new()
        {
            Name = "Moma Titan — Máy chủ hiệu năng cao Xeon cho hệ thống tải lớn",
            Slug = "moma-titan-may-chu-hieu-nang-cao",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Moma Titan — Máy chủ hiệu năng cao Xeon cho hệ thống tải lớn chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/moma-titan-may-chu-hieu-nang-cao-f44a57b3-400.webp",
            Sku = "MMO-013-MOMATITAN",
            Price = 990000,
            ComparePrice = 1485000
        },
        new()
        {
            Name = "Workflow viết mô tả sản phẩm chuẩn SEO n8n",
            Slug = "workflow-viet-mo-ta-san-pham-chuan-seo",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow viết mô tả sản phẩm chuẩn SEO n8n chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-viet-mo-ta-san-pham-chuan-seo-f4e13d35-400.webp",
            Sku = "MMO-014-WORKFLOWV",
            Price = 580000,
            ComparePrice = 870000
        },
        new()
        {
            Name = "Workflow n8n — Chuyển video, cuộc họp thành phụ đề, biên bản và bài viết",
            Slug = "workflow-video-thanh-bai-viet",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow n8n — Chuyển video, cuộc họp thành phụ đề, biên bản và bài viết chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-video-thanh-bai-viet-6349499d-400.webp",
            Sku = "MMO-015-WORKFLOWV",
            Price = 590000,
            ComparePrice = 885000
        },
        new()
        {
            Name = "Workflow n8n — Loạt video AI giữ nhân vật thương hiệu",
            Slug = "workflow-video-ai-nhan-vat-thuong-hieu",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow n8n — Loạt video AI giữ nhân vật thương hiệu chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-video-ai-nhan-vat-thuong-hieu-51141b5a-400.webp",
            Sku = "MMO-016-WORKFLOWV",
            Price = 1790000,
            ComparePrice = 2685000
        },
        new()
        {
            Name = "Tạo Meta Title/Description — Chuẩn SEO bằng AI, tối ưu click-through rate, Dùng ngay!",
            Slug = "mien-phi-tao-meta-title-description-chuan-seo-bang-ai-toi-uu-click-through-rate-dung-ngay",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Tạo Meta Title/Description — Chuẩn SEO bằng AI, tối ưu click-through rate, Dùng ngay! chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/mien-phi-tao-meta-title-description-chuan-seo-bang-ai-toi-uu-5365f076-400.webp",
            Sku = "MMO-017-MIENPHIT",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Tool Check Domain Hàng Loạt Kiểm Tra HTTP, DNS, SSL, Chặn VN Và Tuổi Domain, Dùng Ngay!",
            Slug = "mien-phi-tool-check-domain-hang-loat",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Sản phẩm Tool Check Domain Hàng Loạt Kiểm Tra HTTP, DNS, SSL, Chặn VN Và Tuổi Domain, Dùng Ngay! chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/mien-phi-tool-check-domain-hang-loat-193d5c4a-400.webp",
            Sku = "MMO-018-MIENPHIT",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Tool Spin Nội Dung AI Viết Lại Văn Bản , Chống Trùng Lặp Nội Dung Hàng, Dùng Ngay!",
            Slug = "mien-phi-tool-spin-noi-dung-ai-viet-lai-van-ban",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Sản phẩm Tool Spin Nội Dung AI Viết Lại Văn Bản , Chống Trùng Lặp Nội Dung Hàng, Dùng Ngay! chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/mien-phi-tool-spin-noi-dung-ai-viet-lai-van-ban-c9cf15f8-400.webp",
            Sku = "MMO-019-MIENPHIT",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Fake User-Agent Generator— Tạo UA giả đa thiết bị cho proxy &amp; scraping, Dùng ngay!",
            Slug = "mien-phi-fake-user-agent-generator-tao-ua-gia-da-thiet-bi-cho-proxy-scraping-dung-ngay",
            CategorySlug = "vpn-tien-ich-mang",
            Description = "Sản phẩm Fake User-Agent Generator— Tạo UA giả đa thiết bị cho proxy &amp; scraping, Dùng ngay! chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/mien-phi-fake-user-agent-generator-tao-ua-gia-da-thiet-bi-ch-8991c077-400.webp",
            Sku = "MMO-020-MIENPHIF",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Viết Caption AI Tự Động Tạo Mô Tả Bài Viết Thu Hút, Dùng Ngay!",
            Slug = "tool-viet-caption-ai-tu-dong-tao-mo-ta-bai-viet",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Sản phẩm Viết Caption AI Tự Động Tạo Mô Tả Bài Viết Thu Hút, Dùng Ngay! chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/tool-viet-caption-ai-tu-dong-tao-mo-ta-bai-viet-9f8df4b3-400.webp",
            Sku = "MMO-021-TOOLVIET",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Check IP Blacklist — Kiểm tra IP/domain trong DNS blacklist phổ biến, Dùng ngay!",
            Slug = "mien-phi-check-ip-blacklist-kiem-tra-ip-domain-trong-dns-blacklist-pho-bien-dung-ngay",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Sản phẩm Check IP Blacklist — Kiểm tra IP/domain trong DNS blacklist phổ biến, Dùng ngay! chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/mien-phi-check-ip-blacklist-kiem-tra-ip-domain-trong-dns-bla-0dea3152-400.webp",
            Sku = "MMO-022-MIENPHIC",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Tool Comment Seeding AI Tạo Bình Luận Mồi Tự Nhiên Bằng AI Dùng Ngay!",
            Slug = "tool-comment-seeding-ai-tao-binh-luan-moi-tu-nhien",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Sản phẩm Tool Comment Seeding AI Tạo Bình Luận Mồi Tự Nhiên Bằng AI Dùng Ngay! chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/tool-comment-seeding-ai-tao-binh-luan-moi-tu-nhien-a5d73884-400.webp",
            Sku = "MMO-023-TOOLCOMME",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Tool Bài Viết → Thread – Tự Động Hóa Quy Trình, Dùng Ngay !",
            Slug = "mien-phi-tool-bai-viet-thread-tu-dong-hoa-quy-trinh-dung-ngay",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Tool Bài Viết → Thread – Tự Động Hóa Quy Trình, Dùng Ngay ! chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/mien-phi-tool-bai-viet-→-thread-tu-dong-hoa-quy-trinh-dung-n-b9f1e7e7-400.webp",
            Sku = "MMO-024-MIENPHIT",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Tool Random Thông Tin Người Sinh Dữ Liệu Để Nuôi Tài Khoản, Dùng Ngay !",
            Slug = "tool-random-thong-tin-nguoi-sinh-du-lieu",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Tool Random Thông Tin Người Sinh Dữ Liệu Để Nuôi Tài Khoản, Dùng Ngay ! chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/tool-random-thong-tin-nguoi-sinh-du-lieu-0032b61d-400.webp",
            Sku = "MMO-025-TOOLRANDO",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Tool Lấy Code 2FA TOTP Nhanh Chóng Dùng Ngay !",
            Slug = "mien-phi-tool-lay-code-2fa-totp-nhanh-chong-dung-ngay",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Tool Lấy Code 2FA TOTP Nhanh Chóng Dùng Ngay ! chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/mien-phi-tool-lay-code-2fa-totp-nhanh-chong-dung-ngay-2084a787-400.webp",
            Sku = "MMO-026-MIENPHIT",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Tạo Mật Khẩu Mạnh Bảo Mật Tài Khoản, Dùng Ngay!",
            Slug = "mien-phi-tool-tao-mat-khau-manh-bao-mat-tai-khoan",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Tạo Mật Khẩu Mạnh Bảo Mật Tài Khoản, Dùng Ngay! chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/mien-phi-tool-tao-mat-khau-manh-bao-mat-tai-khoan-d749088c-400.webp",
            Sku = "MMO-027-MIENPHIT",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Lookup IP/ISP — Tra cứu thông tin IP, nhà mạng và vị trí địa lý, Dùng ngay!",
            Slug = "mien-phi-lookup-ip-isp-tra-cuu-thong-tin-ip-nha-mang-va-vi-tri-dia-ly-dung-ngay",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Lookup IP/ISP — Tra cứu thông tin IP, nhà mạng và vị trí địa lý, Dùng ngay! chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/mien-phi-lookup-ip-isp-tra-cuu-thong-tin-ip-nha-mang-va-vi-t-353c08b8-400.webp",
            Sku = "MMO-028-MIENPHIL",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "ClassFlow v1.2.0 — App Quản Lý Thời Khóa Biểu Thông Minh",
            Slug = "tkb-momasolutions-vn-classflow-v1-2-0",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm ClassFlow v1.2.0 — App Quản Lý Thời Khóa Biểu Thông Minh chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/tkb-momasolutions-vn-classflow-v1-2-0-2f2f222b-400.webp",
            Sku = "MMO-029-TKBMOMASO",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Tiện Ích Download Video Trên Trình Duyệt",
            Slug = "tien-ich-download-video-tren-trinh-duyet",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Tiện Ích Download Video Trên Trình Duyệt chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/tien-ich-download-video-tren-trinh-duyet-a99ec9d0-400.webp",
            Sku = "MMO-030-TIENICHD",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Plugin WP Giá Nông Sản Trực Tuyến (Giá nông sản cập nhật hàng ngày)",
            Slug = "plugin-wp-gia-nong-san-truc-tuyen-gia-nong-san-cap-nhat-hang-ngay",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Plugin WP Giá Nông Sản Trực Tuyến (Giá nông sản cập nhật hàng ngày) chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/plugin-wp-gia-nong-san-truc-tuyen-gia-nong-san-cap-nhat-hang-bf2cd2e5-400.webp",
            Sku = "MMO-031-PLUGINWP",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Plugin WP Tooltip Sản Phẩm (Tooltip mô tả dưới tên sản phẩm)",
            Slug = "plugin-wp-tooltip-san-pham-tooltip-mo-ta-duoi-ten-san-pham",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Plugin WP Tooltip Sản Phẩm (Tooltip mô tả dưới tên sản phẩm) chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/plugin-wp-tooltip-san-pham-tooltip-mo-ta-duoi-ten-san-pham-7600a117-400.webp",
            Sku = "MMO-032-PLUGINWP",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Plugin WP Contact Fixed &amp; Tracking (Nút liên hệ cố định, đo lường từng click)",
            Slug = "plugin-wp-contact-fixed-tracking-nut-lien-he-co-dinh-do-luong-tung-click",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Plugin WP Contact Fixed &amp; Tracking (Nút liên hệ cố định, đo lường từng click) chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/plugin-wp-contact-fixed-tracking-nut-lien-he-co-dinh-do-luon-519f2cb1-400.webp",
            Sku = "MMO-033-PLUGINWP",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Plugin WP Khóa Nội Dung WP (Bảo vệ nội dung bằng mật khẩu)",
            Slug = "plugin-wp-khoa-noi-dung-wp-bao-ve-noi-dung-bang-mat-khau",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Plugin WP Khóa Nội Dung WP (Bảo vệ nội dung bằng mật khẩu) chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/plugin-wp-khoa-noi-dung-wp-bao-ve-noi-dung-bang-mat-khau-86fd39b9-400.webp",
            Sku = "MMO-034-PLUGINWP",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Plugin WP Mua Hàng Nhanh (Checkout một bước tăng đơn hàng)",
            Slug = "plugin-wp-mua-hang-nhanh-checkout-mot-buoc-tang-don-hang",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Plugin WP Mua Hàng Nhanh (Checkout một bước tăng đơn hàng) chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/plugin-wp-mua-hang-nhanh-checkout-mot-buoc-tang-don-hang-4dc1ea88-400.webp",
            Sku = "MMO-035-PLUGINWP",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Plugin WP Menu Order Online (Đặt món ngay trên website nhà hàng)",
            Slug = "plugin-wp-menu-order-online-dat-mon-ngay-tren-website-nha-hang",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Plugin WP Menu Order Online (Đặt món ngay trên website nhà hàng) chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/plugin-wp-menu-order-online-dat-mon-ngay-tren-website-nha-ha-465a72b1-400.webp",
            Sku = "MMO-036-PLUGINWP",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Plugin WP Quản Lý Báo Giá &amp; Bảo Hành (Quản lý dịch vụ chuyên nghiệp)",
            Slug = "plugin-wp-quan-ly-bao-gia-bao-hanh-quan-ly-dich-vu-chuyen-nghiep",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Plugin WP Quản Lý Báo Giá &amp; Bảo Hành (Quản lý dịch vụ chuyên nghiệp) chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/plugin-wp-quan-ly-bao-gia-bao-hanh-quan-ly-dich-vu-chuyen-ng-a882f9ae-400.webp",
            Sku = "MMO-037-PLUGINWP",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Plugin WP Affiliate (Xây dựng chương trình affiliate)",
            Slug = "plugin-wp-affiliate-xay-dung-chuong-trinh-affiliate",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Plugin WP Affiliate (Xây dựng chương trình affiliate) chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/plugin-wp-affiliate-xay-dung-chuong-trinh-affiliate-80963313-400.webp",
            Sku = "MMO-038-PLUGINWP",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Plugin WP Tính Chi Phí Xây Dựng (Báo giá xây dựng tự động)",
            Slug = "plugin-wp-tinh-chi-phi-xay-dung-bao-gia-xay-dung-tu-dong",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Plugin WP Tính Chi Phí Xây Dựng (Báo giá xây dựng tự động) chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/plugin-wp-tinh-chi-phi-xay-dung-bao-gia-xay-dung-tu-dong-88a9b735-400.webp",
            Sku = "MMO-039-PLUGINWP",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Plugin WP Woo Review (Đánh giá sản phẩm chuẩn thương mại điện tử)",
            Slug = "plugin-wp-woo-review-danh-gia-san-pham-chuan-thuong-mai-dien-tu",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Plugin WP Woo Review (Đánh giá sản phẩm chuẩn thương mại điện tử) chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/plugin-wp-woo-review-danh-gia-san-pham-chuan-thuong-mai-dien-3499b324-400.webp",
            Sku = "MMO-040-PLUGINWP",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Chatbot AI trả lời khách 24/7",
            Slug = "chatbot-ai-tra-loi-khach-24-7",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Sản phẩm Chatbot AI trả lời khách 24/7 chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/chatbot-ai-tra-loi-khach-24-7-ba9a5c3b-400.webp",
            Sku = "MMO-041-CHATBOTAI",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "AI Gateway – Cổng Token API Thống Nhất",
            Slug = "giai-phap-ai-tu-dong-hoa-ai-gateway-token-api-cong-token-api-thong-nhat",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Sản phẩm AI Gateway – Cổng Token API Thống Nhất chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/xanh-tim-hien-dai-tuong-lai-hinh-anh-sale-deal-uu-dai-san-pham-do-cong-nghe-inst-20-400.webp",
            Sku = "MMO-042-GIAIPHAP",
            Price = 200000,
            ComparePrice = 300000
        },
        new()
        {
            Name = "VPS Cài Sẵn n8n",
            Slug = "giai-phap-ai-tu-dong-hoa-vps-cai-san-n8n",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm VPS Cài Sẵn n8n chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/xanh-tim-hien-dai-tuong-lai-hinh-anh-sale-deal-uu-dai-san-pham-do-cong-nghe-inst-18-400.webp",
            Sku = "MMO-043-GIAIPHAP",
            Price = 150000,
            ComparePrice = 225000
        },
        new()
        {
            Name = "Cloud VPS &amp; Server Hiệu Năng Cao – Moma Nova",
            Slug = "cloud-vps-server-hieu-nang-cao-moma-nova",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Cloud VPS &amp; Server Hiệu Năng Cao – Moma Nova chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/xanh-tim-hien-dai-tuong-lai-hinh-anh-sale-deal-uu-dai-san-pham-do-cong-nghe-inst-16-400.webp",
            Sku = "MMO-044-CLOUDVPS",
            Price = 99000,
            ComparePrice = 148500
        },
        new()
        {
            Name = "N8n Selfhost Siêu Rẻ",
            Slug = "gia-han-n8n-selfhost-sieu-re",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm N8n Selfhost Siêu Rẻ chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2025/gia-han-n8n-selfhost-sieu-re-400.webp",
            Sku = "MMO-045-GIAHANN8",
            Price = 102700,
            ComparePrice = 154050
        },
        new()
        {
            Name = "Workflow n8n — Tách giọng nói và nhạc nền trong video",
            Slug = "workflow-tach-giong-nhac-nen",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow n8n — Tách giọng nói và nhạc nền trong video chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-tach-giong-nhac-nen-91af6cd3-400.webp",
            Sku = "MMO-046-WORKFLOWT",
            Price = 390000,
            ComparePrice = 585000
        },
        new()
        {
            Name = "Workflow n8n — Sao lưu và tải hàng loạt video từ kênh của bạn",
            Slug = "workflow-sao-luu-tai-video-hang-loat",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow n8n — Sao lưu và tải hàng loạt video từ kênh của bạn chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-sao-luu-tai-video-hang-loat-d5b1bb9a-400.webp",
            Sku = "MMO-047-WORKFLOWS",
            Price = 890000,
            ComparePrice = 1335000
        },
        new()
        {
            Name = "Workflow phân tích từ khóa SEO n8n",
            Slug = "workflow-phan-tich-tu-khoa-seo",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow phân tích từ khóa SEO n8n chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-phan-tich-tu-khoa-seo-45921d13-400.webp",
            Sku = "MMO-048-WORKFLOWP",
            Price = 400000,
            ComparePrice = 600000
        },
        new()
        {
            Name = "Workflow n8n — Lồng tiếng AI cho video và bài viết",
            Slug = "workflow-long-tieng-ai",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow n8n — Lồng tiếng AI cho video và bài viết chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-long-tieng-ai-453eaf69-400.webp",
            Sku = "MMO-049-WORKFLOWL",
            Price = 690000,
            ComparePrice = 1035000
        },
        new()
        {
            Name = "Workflow n8n — Dựng video mới theo video mẫu cho sản phẩm của bạn",
            Slug = "workflow-dung-video-theo-video-mau",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow n8n — Dựng video mới theo video mẫu cho sản phẩm của bạn chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-dung-video-theo-video-mau-af5c0f22-400.webp",
            Sku = "MMO-050-WORKFLOWD",
            Price = 1790000,
            ComparePrice = 2685000
        },
        new()
        {
            Name = "Workflow n8n — Dịch video, phụ đề tiếng Việt và lồng tiếng",
            Slug = "workflow-dich-video-phu-de-tieng-viet",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow n8n — Dịch video, phụ đề tiếng Việt và lồng tiếng chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-dich-video-phu-de-tieng-viet-1867e148-400.webp",
            Sku = "MMO-051-WORKFLOWD",
            Price = 1290000,
            ComparePrice = 1935000
        },
        new()
        {
            Name = "Workflow n8n — Đăng lại đa kênh nội dung bạn có quyền",
            Slug = "workflow-dang-lai-noi-dung-da-kenh",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow n8n — Đăng lại đa kênh nội dung bạn có quyền chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-dang-lai-noi-dung-da-kenh-486b446c-400.webp",
            Sku = "MMO-052-WORKFLOWD",
            Price = 1490000,
            ComparePrice = 2235000
        },
        new()
        {
            Name = "Workflow n8n — Đăng bài đa nền tảng từ Google Sheet",
            Slug = "workflow-dang-bai-da-nen-tang-mo-rong",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow n8n — Đăng bài đa nền tảng từ Google Sheet chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-dang-bai-da-nen-tang-mo-rong-71c35c66-400.webp",
            Sku = "MMO-053-WORKFLOWD",
            Price = 1290000,
            ComparePrice = 1935000
        },
        new()
        {
            Name = "Workflow n8n — Cắt video dài của bạn thành video ngắn đa kênh",
            Slug = "workflow-cat-video-dai-thanh-video-ngan",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow n8n — Cắt video dài của bạn thành video ngắn đa kênh chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-cat-video-dai-thanh-video-ngan-f49cfac3-400.webp",
            Sku = "MMO-054-WORKFLOWC",
            Price = 1490000,
            ComparePrice = 2235000
        },
        new()
        {
            Name = "[DỊCH VỤ] Kháng Nghị – Lấy Lại Tài Khoản Facebook Bị Khoá | Chính Chủ | Không Lên Hoàn Tiền 100%",
            Slug = "dich-vu-khang-nghi-lay-lai-tai-khoan-facebook-bi-khoa-chinh-chu-khong-len-hoan-tien-100",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm [DỊCH VỤ] Kháng Nghị – Lấy Lại Tài Khoản Facebook Bị Khoá | Chính Chủ | Không Lên Hoàn Tiền 100% chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/dich-vu-khang-nghi-lay-lai-tai-khoan-facebook-bi-khoa-chinh--5d7ad563-400.webp",
            Sku = "MMO-055-DICHVUKH",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Chứng Chỉ SSL Bảo Mật Website – HTTPS · DV · OV · Wildcard",
            Slug = "chung-chi-ssl-bao-mat-website-https-dv-ov-wildcard",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Chứng Chỉ SSL Bảo Mật Website – HTTPS · DV · OV · Wildcard chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/chung-chi-ssl-bao-mat-website-https-·-dv-·-ov-·-wildcard-7b646af4-400.webp",
            Sku = "MMO-056-CHUNGCHI",
            Price = 390000,
            ComparePrice = 585000
        },
        new()
        {
            Name = "Dịch Vụ Thiết Kế Tiện Ích Plugin/Module/Script MMO",
            Slug = "dich-vu-thiet-ke-tien-ich-plugin-module-script",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Dịch Vụ Thiết Kế Tiện Ích Plugin/Module/Script MMO chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/dich-vu-thiet-kr-tien-ich-plugin-module-spcript-b853b4c9-400.webp",
            Sku = "MMO-057-DICHVUTH",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Dịch Vụ Thiết Kế Website MMO Chuyên Nghiệp",
            Slug = "dich-vu-thiet-ke-website-mmo-chuyen-nghiep",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Dịch Vụ Thiết Kế Website MMO Chuyên Nghiệp chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/dich-vu-thiet-ke-website-mmo-chuyen-nghiep-7ddf1261-400.webp",
            Sku = "MMO-058-DICHVUTH",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Acc Clone TikTok",
            Slug = "acc-tiktok-affiliate-co-san-gio-hang",
            CategorySlug = "giai-tri-xem-phim",
            Description = "Sản phẩm Acc Clone TikTok chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2025/1-4-400.webp",
            Sku = "MMO-059-ACCTIKTOK",
            Price = 35000,
            ComparePrice = 52500
        },
        new()
        {
            Name = "Acc TikTok Livestream",
            Slug = "acc-tiktok-livestream",
            CategorySlug = "giai-tri-xem-phim",
            Description = "Sản phẩm Acc TikTok Livestream chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/1-4-png-400.webp",
            Sku = "MMO-060-ACCTIKTOK",
            Price = 210000,
            ComparePrice = 315000
        },
        new()
        {
            Name = "Antigravity Ultra Chính Chủ– Gói 20X Token [GTW]",
            Slug = "antigravity-ultra-chinh-chu-goi-5x-20x-token-gtw",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Sản phẩm Antigravity Ultra Chính Chủ– Gói 20X Token [GTW] chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/xanh-tim-hien-dai-tuong-lai-hinh-anh-sale-deal-uu-dai-san-pham-do-cong-nghe-inst-12-400.webp",
            Sku = "MMO-061-ANTIGRAVIT",
            Price = 650000,
            ComparePrice = 975000
        },
        new()
        {
            Name = "Capture One Pro Chính Chủ 1 Tháng",
            Slug = "capture-one-pro-chinh-chu-1-thang",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Capture One Pro Chính Chủ 1 Tháng chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/xanh-tim-hien-dai-tuong-lai-hinh-anh-sale-deal-uu-dai-san-pham-do-cong-nghe-inst-28-400.webp",
            Sku = "MMO-062-CAPTUREON",
            Price = 290000,
            ComparePrice = 435000
        },
        new()
        {
            Name = "Codex API Key – Nạp Sẵn Đô, Dùng Ngay [GTW]",
            Slug = "codex-api-key-nap-san-do-dung-ngay-thang-gtw",
            CategorySlug = "he-dieu-hanh",
            Description = "Sản phẩm Codex API Key – Nạp Sẵn Đô, Dùng Ngay [GTW] chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/xanh-tim-hien-dai-tuong-lai-hinh-anh-sale-deal-uu-dai-san-pham-do-cong-nghe-inst-10-400.webp",
            Sku = "MMO-063-CODEXAPI",
            Price = 550000,
            ComparePrice = 825000
        },
        new()
        {
            Name = "Codex Astra 6 Extra  Dùng Riêng [WTG]",
            Slug = "codex-5-6-extra-wtg",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Sản phẩm Codex Astra 6 Extra  Dùng Riêng [WTG] chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/xanh-tim-hien-dai-tuong-lai-hinh-anh-sale-deal-uu-dai-san-pham-do-cong-nghe-inst-7-400.webp",
            Sku = "MMO-064-CODEX56",
            Price = 500000,
            ComparePrice = 750000
        },
        new()
        {
            Name = "Combo Google AI Ultra 20x — Veo3 + Antigravity + Flow + Gemini Ultra 1 tháng, Chính chủ [GTW]",
            Slug = "combo-google-ai-ultra-20x-veo-3-antigravity-flow-gemini-ultra-1-thang-chinh-chu-gtw",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Sản phẩm Combo Google AI Ultra 20x — Veo3 + Antigravity + Flow + Gemini Ultra 1 tháng, Chính chủ [GTW] chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/xanh-tim-hien-dai-tuong-lai-hinh-anh-sale-deal-uu-dai-san-pham-do-cong-nghe-inst-14-400.webp",
            Sku = "MMO-065-COMBOGOOG",
            Price = 720000,
            ComparePrice = 1080000
        },
        new()
        {
            Name = "Figma Professional (gói Edu) — 1 năm, tài khoản cấp sẵn dùng riêng",
            Slug = "figma-professional-goi-edu-1-nam-tai-khoan-cap-san-dung-rieng",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Figma Professional (gói Edu) — 1 năm, tài khoản cấp sẵn dùng riêng chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/figma-1024x853-1-400.webp",
            Sku = "MMO-066-FIGMAPROF",
            Price = 350000,
            ComparePrice = 525000
        },
        new()
        {
            Name = "Gemini Pro [GTW]",
            Slug = "gemini-pro-gtw",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Sản phẩm Gemini Pro [GTW] chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/xanh-tim-hien-dai-tuong-lai-hinh-anh-sale-deal-uu-dai-san-pham-do-cong-nghe-inst-8-400.webp",
            Sku = "MMO-067-GEMINIPRO",
            Price = 139000,
            ComparePrice = 208500
        },
        new()
        {
            Name = "Google One Slot – Cấp Tài Khoản Dùng Riêng 12 Tháng",
            Slug = "google-one-slot-cap-tai-khoan-dung-rieng",
            CategorySlug = "tai-khoan-dich-vu-so",
            Description = "Sản phẩm Google One Slot – Cấp Tài Khoản Dùng Riêng 12 Tháng chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/images-1-400.webp",
            Sku = "MMO-068-GOOGLEONE",
            Price = 220000,
            ComparePrice = 330000
        },
        new()
        {
            Name = "Kling AI 700–1K Credit – Tạo Video AI 4K Chuyên Nghiệp 1 Tháng",
            Slug = "kling-ai-1k1-credit-tao-video-ai-4k-chuyen-nghiep-gtw",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Sản phẩm Kling AI 700–1K Credit – Tạo Video AI 4K Chuyên Nghiệp 1 Tháng chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/xanh-tim-hien-dai-tuong-lai-hinh-anh-sale-deal-uu-dai-san-pham-do-cong-nghe-inst-3-400.webp",
            Sku = "MMO-069-KLINGAI1",
            Price = 250000,
            ComparePrice = 375000
        },
        new()
        {
            Name = "KÊNH TIKTOK LIVE STUDIO",
            Slug = "acc-tiktok-co-gio-hang-lam-affiliate-gio-tu-nhien-khong-lien-ket-seller-co-livestream-chua-co-live-studio",
            CategorySlug = "giai-tri-xem-phim",
            Description = "Sản phẩm KÊNH TIKTOK LIVE STUDIO chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/1-4-png-400.webp",
            Sku = "MMO-070-ACCTIKTOK",
            Price = 220000,
            ComparePrice = 330000
        },
        new()
        {
            Name = "Kênh TikTok Shop Affiliate",
            Slug = "tiktok-nhieu-follow-nhieu-live-tuong-tac-cao",
            CategorySlug = "giai-tri-xem-phim",
            Description = "Sản phẩm Kênh TikTok Shop Affiliate chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/1-4-png-400.webp",
            Sku = "MMO-071-TIKTOKNHI",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Kênh TikTok follow thật — bàn giao full quyền",
            Slug = "tool-xay-kenh-tiktok",
            CategorySlug = "giai-tri-xem-phim",
            Description = "Sản phẩm Kênh TikTok follow thật — bàn giao full quyền chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/1-4-png-400.webp",
            Sku = "MMO-072-TOOLXAYK",
            Price = 8000000,
            ComparePrice = 12000000
        },
        new()
        {
            Name = "Locket Gold Chính Chủ — Chỉ Cần Username, Bảo Hành 12 Tháng (iOS)",
            Slug = "locket-gold-chinh-chu-chi-can-username-bao-hanh-12-thang-ios",
            CategorySlug = "giai-tri-xem-phim",
            Description = "Sản phẩm Locket Gold Chính Chủ — Chỉ Cần Username, Bảo Hành 12 Tháng (iOS) chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/locket-app-icon-01-400.webp",
            Sku = "MMO-073-LOCKETGOL",
            Price = 299000,
            ComparePrice = 448500
        },
        new()
        {
            Name = "Mua Office 365 Giá Rẻ 1 Năm – Chính Chủ",
            Slug = "mua-office-365-gia-re-1-nam-chinh-chu",
            CategorySlug = "ung-dung-van-phong",
            Description = "Sản phẩm Mua Office 365 Giá Rẻ 1 Năm – Chính Chủ chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/images-400.webp",
            Sku = "MMO-074-MUAOFFICE",
            Price = 250000,
            ComparePrice = 375000
        },
        new()
        {
            Name = "Nâng Cấp Gemini AI Pro Chính Chủ",
            Slug = "nang-cap-gemini-ai-pro-chinh-chu",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Sản phẩm Nâng Cấp Gemini AI Pro Chính Chủ chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/xanh-tim-hien-dai-tuong-lai-hinh-anh-sale-deal-uu-dai-san-pham-do-cong-nghe-inst-9-400.webp",
            Sku = "MMO-075-NANGCAPG",
            Price = 230000,
            ComparePrice = 345000
        },
        new()
        {
            Name = "Nâng Cấp Surfshark VPN Chính Chủ",
            Slug = "nang-cap-surfshark-vpn-chinh-chu",
            CategorySlug = "vpn-tien-ich-mang",
            Description = "Sản phẩm Nâng Cấp Surfshark VPN Chính Chủ chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/xanh-tim-hien-dai-tuong-lai-hinh-anh-sale-deal-uu-dai-san-pham-do-cong-nghe-inst-25-400.webp",
            Sku = "MMO-076-NANGCAPS",
            Price = 19000,
            ComparePrice = 28500
        },
        new()
        {
            Name = "Nâng cấp Tài khoản Zoom Pro 100 Chính Chủ",
            Slug = "nang-cap-tai-khoan-zoom-pro-tai-khoan-chinh-chu",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Sản phẩm Nâng cấp Tài khoản Zoom Pro 100 Chính Chủ chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2025/nang-cap-tai-khoan-zoom-pro-1-thang-tai-khoan-chinh-chu-thumbnail-600x600-1-400.webp",
            Sku = "MMO-077-NANGCAPT",
            Price = 70000,
            ComparePrice = 105000
        },
        new()
        {
            Name = "Nâng cấp tài khoản Suno Pro – AI chuyển văn bản thành bài hát",
            Slug = "nang-cap-tai-khoan-suno-pro-ai-chuyen-van-ban-thanh-bai-hat",
            CategorySlug = "giai-tri-xem-phim",
            Description = "Sản phẩm Nâng cấp tài khoản Suno Pro – AI chuyển văn bản thành bài hát chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/35-510x510-1-400.webp",
            Sku = "MMO-078-NANGCAPT",
            Price = 250000,
            ComparePrice = 375000
        },
        new()
        {
            Name = "Fam Canva Edu 1 Năm Cấp Sẵn",
            Slug = "fam-canva-edu-500-slot-1-nam",
            CategorySlug = "ung-dung-van-phong",
            Description = "Sản phẩm Fam Canva Edu 1 Năm Cấp Sẵn chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2025/canva-edu-2-400.webp",
            Sku = "MMO-079-FAMCANVA",
            Price = 199000,
            ComparePrice = 298500
        },
        new()
        {
            Name = "Gemini Ultra X5–X20 – Google AI Ultra Chính Chủ",
            Slug = "tai-khoan-gemini-ultra",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Sản phẩm Gemini Ultra X5–X20 – Google AI Ultra Chính Chủ chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2025/tai-khoan-gemini-ultra-400.webp",
            Sku = "MMO-080-TAIKHOAN",
            Price = 420000,
            ComparePrice = 630000
        },
        new()
        {
            Name = "Google AntiGravity Pro Chính Chủ",
            Slug = "google-antigravity-pro-chinh-chu",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Sản phẩm Google AntiGravity Pro Chính Chủ chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/xanh-tim-hien-dai-tuong-lai-hinh-anh-sale-deal-uu-dai-san-pham-do-cong-nghe-inst-13-400.webp",
            Sku = "MMO-081-GOOGLEANT",
            Price = 300000,
            ComparePrice = 450000
        },
        new()
        {
            Name = "LinkedIn Premium Career – Chính Chủ 3 Tháng",
            Slug = "linkedin-premium-career-chinh-chu-3-thang",
            CategorySlug = "key-ban-quyen-phan-mem",
            Description = "Sản phẩm LinkedIn Premium Career – Chính Chủ 3 Tháng chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2025/round-linkedin-logo-isolated-white-background-469489-985-400.webp",
            Sku = "MMO-082-LINKEDINP",
            Price = 390000,
            ComparePrice = 585000
        },
        new()
        {
            Name = "Mua YouTube Xanh 3 Dòng Giá Rẻ Uy Tín 2024–2026",
            Slug = "mua-youtube-xanh-3-dong-kenh-youtube-da-bat-kiem-tien-gia-re-uy-tin-2024-2026",
            CategorySlug = "giai-tri-xem-phim",
            Description = "Sản phẩm Mua YouTube Xanh 3 Dòng Giá Rẻ Uy Tín 2024–2026 chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2026/z7840617426706-fb5b7a4305dc7f4588d86bc8aa3f409a-400.webp",
            Sku = "MMO-083-MUAYOUTUB",
            Price = 150000,
            ComparePrice = 225000
        },
        new()
        {
            Name = "Nâng Cấp Gamma Chính Chủ Phiên Bản Plus/ Pro",
            Slug = "nang-cap-gamma-chinh-chu-phien-ban-plus-pro",
            CategorySlug = "ung-dung-van-phong",
            Description = "Sản phẩm Nâng Cấp Gamma Chính Chủ Phiên Bản Plus/ Pro chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/nang-cap-gamma-chinh-chu-phien-ban-plus-pro-bf0aa4cf-400.webp",
            Sku = "MMO-084-NANGCAPG",
            Price = 359000,
            ComparePrice = 538500
        },
        new()
        {
            Name = "Nâng Cấp Runway AI 1 Tháng Chính Chủ Pro và Unlimited",
            Slug = "nang-cap-runway-ai-tao-video-bang-van-ban",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Sản phẩm Nâng Cấp Runway AI 1 Tháng Chính Chủ Pro và Unlimited chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/nang-cap-runway-ai-tao-video-bang-van-ban-af472d67-400.webp",
            Sku = "MMO-085-NANGCAPR",
            Price = 590000,
            ComparePrice = 885000
        },
        new()
        {
            Name = "Nâng Cấp Tài Khoản Midjourney 1 Tháng Chính Chủ",
            Slug = "nang-cap-tai-khoan-midjourney-chinh-chu",
            CategorySlug = "hoc-tap-lam-viec",
            Description = "Sản phẩm Nâng Cấp Tài Khoản Midjourney 1 Tháng Chính Chủ chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2025/midjourney-400.webp",
            Sku = "MMO-086-NANGCAPT",
            Price = 299000,
            ComparePrice = 448500
        },
        new()
        {
            Name = "Tài Khoản Autodesk All App Chính Chủ",
            Slug = "tai-khoan-autodesk-all-app-chinh-chu",
            CategorySlug = "ung-dung-van-phong",
            Description = "Sản phẩm Tài Khoản Autodesk All App Chính Chủ chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/tai-khoan-autodesk-all-app-chinh-chu-71dd7468-400.webp",
            Sku = "MMO-087-TAIKHOAN",
            Price = 550000,
            ComparePrice = 825000
        },
        new()
        {
            Name = "Tài Khoản Canva Education Chính Chủ",
            Slug = "tai-khoan-canva-education-chinh-chu",
            CategorySlug = "ung-dung-van-phong",
            Description = "Sản phẩm Tài Khoản Canva Education Chính Chủ chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2025/canva-education-400.webp",
            Sku = "MMO-088-TAIKHOAN",
            Price = 139000,
            ComparePrice = 208500
        },
        new()
        {
            Name = "Tài Khoản Capcut Pro Uy Tín",
            Slug = "tao-tai-khoan-capcut-pro-7-30-ngay-uy-tin",
            CategorySlug = "ung-dung-van-phong",
            Description = "Sản phẩm Tài Khoản Capcut Pro Uy Tín chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/2025/untitled-1-400.webp",
            Sku = "MMO-089-TAOTAIKH",
            Price = 99000,
            ComparePrice = 148500
        },
        new()
        {
            Name = "Workflow Chatbot Facebook AI Agent N8N",
            Slug = "workflow-chatbot-facebook-ai-agent-n8n",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow Chatbot Facebook AI Agent N8N chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-chatbot-facebook-ai-agent-n8n-f516c22f-400.webp",
            Sku = "MMO-090-WORKFLOWC",
            Price = 1350000,
            ComparePrice = 2025000
        },
        new()
        {
            Name = "Workflow Chatbot Telegram AI Agent N8N",
            Slug = "workflow-chatbot-telegram-ai-agent-n8n",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow Chatbot Telegram AI Agent N8N chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-chatbot-telegram-ai-agent-n8n-9fa7271f-400.webp",
            Sku = "MMO-091-WORKFLOWC",
            Price = 750000,
            ComparePrice = 1125000
        },
        new()
        {
            Name = "Workflow Chatbot Zalo AI Agent N8N",
            Slug = "workflow-chatbot-zalo-ai-agent-n8n",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow Chatbot Zalo AI Agent N8N chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-chatbot-zalo-ai-agent-n8n-10100eb8-400.webp",
            Sku = "MMO-092-WORKFLOWC",
            Price = 1670000,
            ComparePrice = 2505000
        },
        new()
        {
            Name = "Workflow Chatbot Zalo Cá Nhân Gửi Tin Nhắn Marketing Tự Động N8N",
            Slug = "workflow-chatbot-zalo-ca-nhan-gui-tin-nhan-marketing",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow Chatbot Zalo Cá Nhân Gửi Tin Nhắn Marketing Tự Động N8N chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-chatbot-zalo-ca-nhan-gui-tin-nhan-marketing-46cf39cd-400.webp",
            Sku = "MMO-093-WORKFLOWC",
            Price = 780000,
            ComparePrice = 1170000
        },
        new()
        {
            Name = "Workflow Trợ Lý AI Thông Minh N8N",
            Slug = "workflow-tro-ly-ai-thong-minh-n8n",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow Trợ Lý AI Thông Minh N8N chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-tro-ly-ai-thong-minh-n8n-e7872811-400.webp",
            Sku = "MMO-094-WORKFLOWT",
            Price = 860000,
            ComparePrice = 1290000
        },
        new()
        {
            Name = "Workflow Tìm Khách Hàng Tự Động Từ Facebook Chỉ Trong Vài Giây",
            Slug = "workflow-tim-khach-hang-tu-dong-tu-facebook",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow Tìm Khách Hàng Tự Động Từ Facebook Chỉ Trong Vài Giây chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-tim-khach-hang-tu-dong-tu-facebook-7504a299-400.webp",
            Sku = "MMO-095-WORKFLOWT",
            Price = 3450000,
            ComparePrice = 5175000
        },
        new()
        {
            Name = "Workflow Tìm Kiếm Khách Hàng Realtime Trong Nhóm Chat Zalo",
            Slug = "workflow-tim-kiem-khach-hang-realtime-trong-nhom-zalo",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow Tìm Kiếm Khách Hàng Realtime Trong Nhóm Chat Zalo chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-tim-kiem-khach-hang-realtime-trong-nhom-2-b3b6e090-400.webp",
            Sku = "MMO-096-WORKFLOWT",
            Price = 17200000,
            ComparePrice = 25800000
        },
        new()
        {
            Name = "Workflow Tự Động Tạo Và Đăng Video Ngắn Đăng Đa Nền Tảng Social n8n",
            Slug = "workflow-tu-dong-tao-va-dang-video-ngan",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow Tự Động Tạo Và Đăng Video Ngắn Đăng Đa Nền Tảng Social n8n chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-tu-dong-tao-va-dang-video-ngan-7808ee3d-400.webp",
            Sku = "MMO-097-WORKFLOWT",
            Price = 980000,
            ComparePrice = 1470000
        },
        new()
        {
            Name = "Workflow Tự Động Viết Bài Và Hẹn Lịch Đăng Lên Facebook N8N",
            Slug = "workflow-tu-dong-viet-bai-va-hen-lich-dang-len-facebook-n8n",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow Tự Động Viết Bài Và Hẹn Lịch Đăng Lên Facebook N8N chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-tu-dong-viet-bai-va-hen-lich-dang-len-facebook-n8n-a624f7d6-400.webp",
            Sku = "MMO-098-WORKFLOWT",
            Price = 860000,
            ComparePrice = 1290000
        },
        new()
        {
            Name = "Workflow Tự Động Viết Bài Và Đăng Lên WordPress N8N",
            Slug = "workflow-tu-dong-viet-bai-va-dang-len-wordpress-n8n",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow Tự Động Viết Bài Và Đăng Lên WordPress N8N chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-tu-dong-viet-bai-va-dang-len-wordpress-n8n-52de955f-400.webp",
            Sku = "MMO-099-WORKFLOWT",
            Price = 1800000,
            ComparePrice = 2700000
        },
        new()
        {
            Name = "Workflow Tự động Reply, Like Comment Khách Hàng Trên Page Facebook",
            Slug = "workflow-tu-dong-reply-like-comment-khach-hang-tren-page-facebook",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow Tự động Reply, Like Comment Khách Hàng Trên Page Facebook chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-tu-dong-reply-like-comment-khach-hang-tren-page-fac-620375e8-400.webp",
            Sku = "MMO-100-WORKFLOWT",
            Price = 1720000,
            ComparePrice = 2580000
        },
        new()
        {
            Name = "Workflow WhatsApp AI Agent N8N",
            Slug = "workflow-whatsapp-ai-agent-n8n",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow WhatsApp AI Agent N8N chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-whatsapp-ai-agent-n8n-1dd437f6-400.webp",
            Sku = "MMO-101-WORKFLOWW",
            Price = 780000,
            ComparePrice = 1170000
        },
        new()
        {
            Name = "Workflow n8n — Bản tin ngành tự động có trích nguồn",
            Slug = "workflow-ban-tin-nganh",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow n8n — Bản tin ngành tự động có trích nguồn chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-ban-tin-nganh-2bb6bfd4-400.webp",
            Sku = "MMO-102-WORKFLOWB",
            Price = 790000,
            ComparePrice = 1185000
        },
        new()
        {
            Name = "Workflow n8n — Chấm Công Nhân Viên Bằng Tin Nhắn",
            Slug = "workflow-n8n-cham-cong-nhan-vien-bang-tin-nhan",
            CategorySlug = "bot-script-tu-dong",
            Description = "Sản phẩm Workflow n8n — Chấm Công Nhân Viên Bằng Tin Nhắn chính hãng cung cấp tự động từ đối tác MMO uy tín. Bảo hành đầy đủ, hỗ trợ kỹ thuật 24/7, kích hoạt ngay sau khi thanh toán.",
            ImageUrl = "https://taphoammo.vn/media/gen/workflow-n8n-cham-cong-nhan-vien-bang-tin-nhan-c0b87de7-400.webp",
            Sku = "MMO-103-WORKFLOWN",
            Price = 750000,
            ComparePrice = 1125000
        },
    };

    public static async Task<int> SeedTapHoaMmoProductsAsync(
        SocialDbContext context,
        ICryptoService cryptoService,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Bắt đầu đồng bộ và nạp sản phẩm cào từ TapHoaMMO ({Count} sản phẩm)...", Items.Count);

            var partnerShop = await context.Shops.FirstOrDefaultAsync(s => s.Slug == "techzone-official", cancellationToken)
                ?? await context.Shops.FirstOrDefaultAsync(cancellationToken);

            if (partnerShop == null)
            {
                logger.LogWarning("Không tìm thấy Shop để gán sản phẩm TapHoaMMO.");
                return 0;
            }

            var allCategories = await context.Categories.ToListAsync(cancellationToken);
            int newProductsCount = 0;
            var random = new Random(42);

            foreach (var item in Items)
            {
                try
                {
                    var existingProduct = await context.Products
                        .Include(p => p.Variants)
                        .Include(p => p.Images)
                        .FirstOrDefaultAsync(p => p.Slug == item.Slug, cancellationToken);

                    var category = allCategories.FirstOrDefault(c => c.Slug == item.CategorySlug)
                        ?? allCategories.FirstOrDefault(c => c.Slug == "tai-khoan-dich-vu-so")
                        ?? allCategories.FirstOrDefault();

                    if (existingProduct == null)
                    {
                        existingProduct = new Product(
                            shopId: partnerShop.Id,
                            name: item.Name,
                            slug: item.Slug,
                            categoryId: category?.Id,
                            description: item.Description
                        );
                        existingProduct.Publish();
                        existingProduct.UpdatePriceCache(item.Price, item.Price);
                        
                        var rating = 4.7 + (random.NextDouble() * 0.3);
                        var reviews = random.Next(25, 180);
                        existingProduct.UpdateRating(Math.Round(rating, 1), reviews);

                        context.Products.Add(existingProduct);
                        await context.SaveChangesAsync(cancellationToken);

                        var variant = new ProductVariant(
                            productId: existingProduct.Id,
                            shopId: partnerShop.Id,
                            sku: item.Sku,
                            combinationKey: "default",
                            price: item.Price,
                            compareAtPrice: item.ComparePrice,
                            deliveryType: DeliveryType.CodePool,
                            trackInventory: true
                        );
                        context.ProductVariants.Add(variant);

                        var img = new ProductImage(existingProduct.Id, item.ImageUrl, 0, isCover: true);
                        context.ProductImages.Add(img);

                        await context.SaveChangesAsync(cancellationToken);
                        newProductsCount++;

                        // Bổ sung kho mã số tự động (15 mã) để sản phẩm lập tức mua được
                        for (int i = 1; i <= 15; i++)
                        {
                            var rawCode = $"{variant.Sku}-AUTO-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
                            var enc = cryptoService.Encrypt(rawCode);
                            var hash = cryptoService.ComputeHash(rawCode);
                            var masked = cryptoService.MaskValue(rawCode);
                            var code = new DigitalCode(variant.ShopId, variant.Id, enc, hash, masked);
                            context.DigitalCodes.Add(code);
                        }
                        await context.SaveChangesAsync(cancellationToken);
                    }
                    else
                    {
                        // Đảm bảo ảnh đại diện luôn hợp lệ và trỏ đúng nguồn ảnh thực tế
                        var coverImg = existingProduct.Images.FirstOrDefault(i => i.IsCover);
                        if (coverImg == null)
                        {
                            context.ProductImages.Add(new ProductImage(existingProduct.Id, item.ImageUrl, 0, isCover: true));
                            await context.SaveChangesAsync(cancellationToken);
                        }
                        else if (coverImg.Url != item.ImageUrl && !coverImg.Url.StartsWith("https://taphoammo.vn"))
                        {
                            context.ProductImages.Remove(coverImg);
                            context.ProductImages.Add(new ProductImage(existingProduct.Id, item.ImageUrl, 0, isCover: true));
                            await context.SaveChangesAsync(cancellationToken);
                        }

                        // Đảm bảo có biến thể và tồn kho
                        var variant = existingProduct.Variants.FirstOrDefault()
                            ?? await context.ProductVariants.FirstOrDefaultAsync(v => v.ProductId == existingProduct.Id, cancellationToken);

                        if (variant != null)
                        {
                            var availableCount = await context.DigitalCodes.CountAsync(
                                c => c.VariantId == variant.Id && c.Status == DigitalCodeStatus.Available,
                                cancellationToken);

                            if (availableCount < 10)
                            {
                                var addCount = 15 - availableCount;
                                for (int i = 1; i <= addCount; i++)
                                {
                                    var rawCode = $"{variant.Sku}-AUTO-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
                                    var enc = cryptoService.Encrypt(rawCode);
                                    var hash = cryptoService.ComputeHash(rawCode);
                                    var masked = cryptoService.MaskValue(rawCode);
                                    var code = new DigitalCode(variant.ShopId, variant.Id, enc, hash, masked);
                                    context.DigitalCodes.Add(code);
                                }
                                await context.SaveChangesAsync(cancellationToken);
                            }
                        }
                    }
                }
                catch (Exception itemEx)
                {
                    logger.LogWarning("Bỏ qua lỗi xử lý item {Slug}: {Message}", item.Slug, itemEx.Message);
                }
            }

            logger.LogInformation("Hoàn tất nạp dữ liệu TapHoaMMO: Đã tạo mới {Count} sản phẩm kèm mã kho!", newProductsCount);
            return newProductsCount;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi nạp dữ liệu TapHoaMMO: {Message}", ex.Message);
            return 0;
        }
    }
}
