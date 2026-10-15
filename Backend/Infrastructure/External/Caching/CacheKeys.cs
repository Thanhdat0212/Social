namespace Infrastructure.External.Caching;

/// <summary>
/// Định nghĩa quy chuẩn tên Cache Key thống nhất toàn hệ thống
/// </summary>
public static class CacheKeys
{
    // Social Cache Keys
    public const string InterestsAllActive = "interests:all_active";
    public static string UserProfile(string username) => $"profile:{username.ToLowerInvariant()}";

    // Marketplace Catalog & Shop Cache Keys (Sẵn sàng cho M1 - M3)
    public const string CategoriesTree = "categories:tree";
    public static string CategoryAttributes(Guid categoryId) => $"categories:{categoryId}:attributes";
    public static string ShopPublic(string slug) => $"shop:{slug.ToLowerInvariant()}:public";
    public static string ProductDetail(string slug) => $"product:{slug.ToLowerInvariant()}:detail";

    // Marketplace Finance Cache Keys (M6)
    public static string ShopBalance(Guid shopId) => $"shop:{shopId}:balance";
}
