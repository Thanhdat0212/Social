using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Marketplace;

/// <summary>Đăng ký toàn bộ repository, service và worker của module Marketplace.</summary>
public static class MarketplaceModule
{
    public static IServiceCollection AddMarketplaceModule(this IServiceCollection services)
    {
        services.AddScoped<Application.Interfaces.Repositories.Marketplace.Shops.IShopRepository, Infrastructure.Marketplace.Shops.ShopRepository>();
        services.AddScoped<Application.Interfaces.Repositories.Marketplace.Shops.IShopReviewRepository, Infrastructure.Marketplace.Shops.ShopReviewRepository>();
        services.AddScoped<Application.Interfaces.Repositories.Marketplace.Shops.IShopSanctionRepository, Infrastructure.Marketplace.Shops.ShopSanctionRepository>();
        services.AddScoped<Application.Interfaces.Repositories.Marketplace.Categories.ICategoryRepository, Infrastructure.Marketplace.Categories.CategoryRepository>();
        services.AddScoped<Application.Interfaces.Repositories.Marketplace.Categories.IAttributeRepository, Infrastructure.Marketplace.Categories.AttributeRepository>();
        services.AddScoped<Application.Interfaces.Repositories.Marketplace.Categories.ICategoryRequestRepository, Infrastructure.Marketplace.Categories.CategoryRequestRepository>();

        services.AddScoped<Application.Interfaces.Marketplace.Shops.IShopService, Infrastructure.Marketplace.Shops.ShopService>();
        services.AddScoped<Application.Interfaces.Marketplace.Shops.IAdminShopService, Infrastructure.Marketplace.Shops.AdminShopService>();
        services.AddScoped<Application.Interfaces.Marketplace.Categories.ICategoryService, Infrastructure.Marketplace.Categories.CategoryService>();
        services.AddScoped<Application.Interfaces.Marketplace.Categories.IAdminCategoryService, Infrastructure.Marketplace.Categories.AdminCategoryService>();
        services.AddScoped<Application.Interfaces.Marketplace.Categories.ISellerCategoryService, Infrastructure.Marketplace.Categories.SellerCategoryService>();
        services.AddScoped<Application.Interfaces.Marketplace.Products.IProductService, Infrastructure.Marketplace.Products.ProductService>();
        services.AddScoped<Application.Interfaces.Marketplace.Products.ICatalogService, Infrastructure.Marketplace.Products.CatalogService>();
        services.AddScoped<Application.Interfaces.Marketplace.Products.IAdminProductService, Infrastructure.Marketplace.Products.AdminProductService>();
        services.AddScoped<Application.Interfaces.Marketplace.Products.IProductReviewService, Infrastructure.Marketplace.Products.ProductReviewService>();
        services.AddScoped<Application.Interfaces.Marketplace.Inventory.IInventoryService, Infrastructure.Marketplace.Inventory.InventoryService>();
        services.AddScoped<Application.Interfaces.Marketplace.Orders.ICartService, Infrastructure.Marketplace.Orders.CartService>();
        services.AddScoped<Application.Interfaces.Marketplace.Orders.ICheckoutService, Infrastructure.Marketplace.Orders.CheckoutService>();
        services.AddScoped<Application.Interfaces.Marketplace.Orders.IOrderService, Infrastructure.Marketplace.Orders.OrderService>();
        services.AddHostedService<Infrastructure.Marketplace.Orders.CheckoutExpirationWorker>();
        services.AddHostedService<Infrastructure.Marketplace.Orders.FundReleaseWorker>();
        services.AddScoped<Application.Interfaces.Repositories.Marketplace.Payments.IPaymentRepository, Infrastructure.Marketplace.Payments.PaymentRepository>();
        services.AddScoped<Application.Interfaces.Repositories.Marketplace.Ledger.ILedgerRepository, Infrastructure.Marketplace.Ledger.LedgerRepository>();
        services.AddScoped<Application.Interfaces.Marketplace.Payments.IPaymentGateway, Infrastructure.Marketplace.Payments.MockPaymentGateway>();
        services.AddHttpClient<Infrastructure.Marketplace.Payments.PayOsPaymentGateway>();
        services.AddScoped<Application.Interfaces.Marketplace.Payments.IPaymentGateway>(sp => sp.GetRequiredService<Infrastructure.Marketplace.Payments.PayOsPaymentGateway>());
        services.AddScoped<Application.Interfaces.Marketplace.Payments.IPaymentService, Infrastructure.Marketplace.Payments.PaymentService>();
        services.AddScoped<Application.Interfaces.Marketplace.Ledger.ILedgerService, Infrastructure.Marketplace.Ledger.LedgerService>();
        services.AddScoped<Application.Interfaces.Repositories.Marketplace.Complaints.IComplaintRepository, Infrastructure.Marketplace.Complaints.ComplaintRepository>();
        services.AddScoped<Application.Interfaces.Repositories.Marketplace.Complaints.IRefundRepository, Infrastructure.Marketplace.Complaints.RefundRepository>();
        services.AddScoped<Application.Interfaces.Marketplace.Complaints.IComplaintService, Infrastructure.Marketplace.Complaints.ComplaintService>();
        services.AddHostedService<Infrastructure.Marketplace.Complaints.ComplaintEscalationWorker>();
        services.AddScoped<Application.Interfaces.Repositories.Marketplace.Payouts.IShopBankAccountRepository, Infrastructure.Marketplace.Payouts.ShopBankAccountRepository>();
        services.AddScoped<Application.Interfaces.Repositories.Marketplace.Payouts.IPayoutRepository, Infrastructure.Marketplace.Payouts.PayoutRepository>();
        services.AddScoped<Application.Interfaces.Marketplace.Payouts.IPayoutService, Infrastructure.Marketplace.Payouts.PayoutService>();
        services.AddScoped<Application.Interfaces.Marketplace.Audit.IReconciliationService, Infrastructure.Marketplace.Audit.ReconciliationService>();
        services.AddHostedService<Infrastructure.Marketplace.Audit.DailyReconciliationWorker>();
        services.AddScoped<Application.Interfaces.Marketplace.Reports.IAbuseProtectionService, Infrastructure.Marketplace.Reports.AbuseProtectionService>();

        return services;
    }
}
