using Domain.Entities.Marketplace.Categories;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

public static class CategorySeeder
{
    public static async Task SeedAsync(SocialDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        try
        {
            if (await context.Categories.AnyAsync(cancellationToken))
            {
                logger.LogInformation("Database đã có danh mục hàng hóa, bỏ qua seed Categories.");
                return;
            }

            logger.LogInformation("Bắt đầu nạp danh mục và thuộc tính mẫu cho gian hàng số...");

            // 1. SEED THUỘC TÍNH SẢN PHẨM (Attribute Definitions & Options)
            var attrDuration = new AttributeDefinition("thoi-han", "Thời hạn sử dụng", AttributeDataType.Enum);
            attrDuration.Options.Add(new AttributeOption(attrDuration.Id, "1 Tháng", "1-thang", 0));
            attrDuration.Options.Add(new AttributeOption(attrDuration.Id, "3 Tháng", "3-thang", 1));
            attrDuration.Options.Add(new AttributeOption(attrDuration.Id, "6 Tháng", "6-thang", 2));
            attrDuration.Options.Add(new AttributeOption(attrDuration.Id, "1 Năm", "1-nam", 3));
            attrDuration.Options.Add(new AttributeOption(attrDuration.Id, "Vĩnh viễn", "vinh-vien", 4));

            var attrOs = new AttributeDefinition("he-dieu-hanh", "Hệ điều hành hỗ trợ", AttributeDataType.Enum);
            attrOs.Options.Add(new AttributeOption(attrOs.Id, "Windows", "windows", 0));
            attrOs.Options.Add(new AttributeOption(attrOs.Id, "macOS", "macos", 1));
            attrOs.Options.Add(new AttributeOption(attrOs.Id, "Linux", "linux", 2));
            attrOs.Options.Add(new AttributeOption(attrOs.Id, "Android", "android", 3));
            attrOs.Options.Add(new AttributeOption(attrOs.Id, "iOS", "ios", 4));

            var attrLanguage = new AttributeDefinition("ngon-ngu", "Ngôn ngữ lập trình", AttributeDataType.Enum);
            attrLanguage.Options.Add(new AttributeOption(attrLanguage.Id, "C# / .NET", "csharp-dotnet", 0));
            attrLanguage.Options.Add(new AttributeOption(attrLanguage.Id, "JavaScript / TypeScript", "js-ts", 1));
            attrLanguage.Options.Add(new AttributeOption(attrLanguage.Id, "Python", "python", 2));
            attrLanguage.Options.Add(new AttributeOption(attrLanguage.Id, "Java", "java", 3));
            attrLanguage.Options.Add(new AttributeOption(attrLanguage.Id, "Go", "golang", 4));

            var attrStorage = new AttributeDefinition("dung-luong", "Dung lượng lưu trữ", AttributeDataType.Number, unit: "GB");
            var attrDevices = new AttributeDefinition("so-thiet-bi", "Số thiết bị tối đa", AttributeDataType.Number, unit: "Thiết bị");

            context.AttributeDefinitions.AddRange(attrDuration, attrOs, attrLanguage, attrStorage, attrDevices);
            await context.SaveChangesAsync(cancellationToken);

            // 2. SEED CÂY DANH MỤC (Tối đa 3 cấp)
            // ROOT 1: Tài khoản & Dịch vụ số
            var catAccounts = new Category(
                name: "Tài khoản & Dịch vụ số",
                slug: "tai-khoan-dich-vu-so",
                displayOrder: 1,
                productType: ProductType.Digital,
                moderationPolicy: CategoryModerationPolicy.DirectListing);
            context.Categories.Add(catAccounts);
            await context.SaveChangesAsync(cancellationToken);

            var catEntertainment = new Category("Giải trí & Xem phim", "giai-tri-xem-phim", catAccounts.Id, catAccounts.Path, catAccounts.Depth, 1);
            var catWorkStudy = new Category("Học tập & Làm việc", "hoc-tap-lam-viec", catAccounts.Id, catAccounts.Path, catAccounts.Depth, 2);
            var catVpn = new Category("VPN & Tiện ích mạng", "vpn-tien-ich-mang", catAccounts.Id, catAccounts.Path, catAccounts.Depth, 3);
            context.Categories.AddRange(catEntertainment, catWorkStudy, catVpn);
            await context.SaveChangesAsync(cancellationToken);

            // ROOT 2: Key & Bản quyền phần mềm
            var catKeys = new Category(
                name: "Key & Bản quyền phần mềm",
                slug: "key-ban-quyen-phan-mem",
                displayOrder: 2,
                productType: ProductType.Digital,
                moderationPolicy: CategoryModerationPolicy.DirectListing);
            context.Categories.Add(catKeys);
            await context.SaveChangesAsync(cancellationToken);

            var catOs = new Category("Hệ điều hành", "he-dieu-hanh", catKeys.Id, catKeys.Path, catKeys.Depth, 1);
            var catOffice = new Category("Ứng dụng văn phòng", "ung-dung-van-phong", catKeys.Id, catKeys.Path, catKeys.Depth, 2);
            var catSecurity = new Category("Bảo mật & Antivirus", "bao-mat-antivirus", catKeys.Id, catKeys.Path, catKeys.Depth, 3);
            context.Categories.AddRange(catOs, catOffice, catSecurity);
            await context.SaveChangesAsync(cancellationToken);

            // ROOT 3: Khóa học số & Tài liệu
            var catCourses = new Category(
                name: "Khóa học & Tài liệu số",
                slug: "khoa-hoc-tai-lieu-so",
                displayOrder: 3,
                productType: ProductType.Digital,
                moderationPolicy: CategoryModerationPolicy.DirectListing);
            context.Categories.Add(catCourses);
            await context.SaveChangesAsync(cancellationToken);

            var catProgramming = new Category("Lập trình & CNTT", "lap-trinh-cntt", catCourses.Id, catCourses.Path, catCourses.Depth, 1);
            var catLanguages = new Category("Ngoại ngữ", "ngoai-ngu", catCourses.Id, catCourses.Path, catCourses.Depth, 2);
            context.Categories.AddRange(catProgramming, catLanguages);
            await context.SaveChangesAsync(cancellationToken);

            // ROOT 4: Source Code & Template
            var catSourceCode = new Category(
                name: "Source Code & Template",
                slug: "source-code-template",
                displayOrder: 4,
                productType: ProductType.Digital,
                moderationPolicy: CategoryModerationPolicy.DirectListing);
            context.Categories.Add(catSourceCode);
            await context.SaveChangesAsync(cancellationToken);

            var catWeb = new Category("Website & Web App", "website-web-app", catSourceCode.Id, catSourceCode.Path, catSourceCode.Depth, 1);
            var catMobile = new Category("Mobile App", "mobile-app", catSourceCode.Id, catSourceCode.Path, catSourceCode.Depth, 2);
            var catBots = new Category("Bot & Script tự động", "bot-script-tu-dong", catSourceCode.Id, catSourceCode.Path, catSourceCode.Depth, 3);
            context.Categories.AddRange(catWeb, catMobile, catBots);
            await context.SaveChangesAsync(cancellationToken);

            // 3. GÁN THUỘC TÍNH VÀO DANH MỤC (CategoryAttributes)
            // Gán Thời hạn & Số thiết bị cho Tài khoản
            context.CategoryAttributes.Add(new CategoryAttribute(catAccounts.Id, attrDuration.Id, AttributeRequirement.Required, isFilterable: true, isVariantAxis: true));
            context.CategoryAttributes.Add(new CategoryAttribute(catAccounts.Id, attrDevices.Id, AttributeRequirement.Optional, isFilterable: true, isVariantAxis: false));

            // Gán Thời hạn & Hệ điều hành cho Key bản quyền
            context.CategoryAttributes.Add(new CategoryAttribute(catKeys.Id, attrDuration.Id, AttributeRequirement.Required, isFilterable: true, isVariantAxis: true));
            context.CategoryAttributes.Add(new CategoryAttribute(catKeys.Id, attrOs.Id, AttributeRequirement.Required, isFilterable: true, isVariantAxis: false));

            // Gán Ngôn ngữ lập trình cho Source Code
            context.CategoryAttributes.Add(new CategoryAttribute(catSourceCode.Id, attrLanguage.Id, AttributeRequirement.Required, isFilterable: true, isVariantAxis: false));

            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Đã hoàn tất nạp cây danh mục và thuộc tính sản phẩm số.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi xảy ra trong quá trình seed danh mục và thuộc tính: {Message}", ex.Message);
        }
    }
}
