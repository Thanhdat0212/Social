import { apiClient } from './client';
import type {
  Category,
  ProductListItem,
  ProductDetail,
  CartDto,
  CreateCheckoutRequest,
  CheckoutResponse,
  PaymentStatusResponse,
  OrderDto,
  CreateComplaintRequest,
  ComplaintDto,
  ShopBalanceDto,
  ShopBankAccountDto,
  PayoutDto,
  ShopPerformanceDto,
  SellerProductDetailDto,
  UpdateProductDetailsRequest,
  UpdateProductVariantsRequest,
  ProductReviewDto,
  ProductReviewSummaryDto,
  CreateProductReviewRequest,
  SellerReplyReviewRequest,
  SignedDownloadUrlDto,
} from '@/types/marketplace';

export const marketplaceApi = {
  // 1. Khám phá & Danh mục
  getCategoriesTree: async (): Promise<Category[]> => {
    const res = await apiClient.get('/categories/tree');
    return res.data;
  },

  getProducts: async (params?: {
    categoryId?: string;
    search?: string;
    minPrice?: number;
    maxPrice?: number;
    sort?: string;
    page?: number;
    pageSize?: number;
  }): Promise<{ items: ProductListItem[]; totalCount: number }> => {
    const res = await apiClient.get('/products', { params });
    return res.data;
  },

  getProductBySlug: async (slug: string): Promise<ProductDetail> => {
    const res = await apiClient.get(`/products/${slug}`);
    return res.data;
  },

  getPublicShop: async (slug: string) => {
    const res = await apiClient.get(`/shops/${slug}`);
    return res.data;
  },

  getShopPublicProducts: async (
    slug: string,
    params?: { page?: number; pageSize?: number; search?: string }
  ): Promise<{ items: ProductListItem[]; totalCount: number }> => {
    const res = await apiClient.get(`/shops/${slug}/products`, { params });
    return res.data;
  },

  reportProduct: async (
    productId: string,
    data: { reason: number; description: string; evidenceUrl?: string }
  ) => {
    const res = await apiClient.post(`/products/${productId}/report`, data);
    return res.data;
  },

  // 2. Giỏ hàng
  getCart: async (): Promise<CartDto> => {
    const res = await apiClient.get('/cart');
    return res.data;
  },

  addToCart: async (variantId: string, quantity: number): Promise<CartDto> => {
    const res = await apiClient.post('/cart/items', { variantId, quantity });
    return res.data;
  },

  updateCartItem: async (itemId: string, quantity: number): Promise<CartDto> => {
    const res = await apiClient.patch(`/cart/items/${itemId}`, { quantity });
    return res.data;
  },

  removeCartItem: async (itemId: string): Promise<CartDto> => {
    const res = await apiClient.delete(`/cart/items/${itemId}`);
    return res.data;
  },

  // 3. Checkout & Thanh toán VietQR
  createCheckout: async (request: CreateCheckoutRequest): Promise<CheckoutResponse> => {
    const res = await apiClient.post('/checkouts', request);
    return res.data;
  },

  createPayOsPayment: async (checkoutId: string) => {
    try {
      const res = await apiClient.post(`/payments/checkout/${checkoutId}`);
      return res.data;
    } catch {
      const res = await apiClient.post('/payments', { checkoutId, provider: 2 });
      return res.data;
    }
  },

  verifyPayment: async (checkoutId: string): Promise<PaymentStatusResponse> => {
    try {
      const res = await apiClient.get(`/payments/checkout/${checkoutId}/verify`);
      return res.data;
    } catch {
      const res = await apiClient.get(`/payments/checkout/${checkoutId}`);
      return res.data;
    }
  },

  // 4. Đơn hàng & Mã đã nhận
  getMyOrders: async (
    page = 1,
    pageSize = 15,
    status?: string
  ): Promise<{ items: OrderDto[]; totalCount: number }> => {
    const res = await apiClient.get('/orders', { params: { page, pageSize, status } });
    return res.data;
  },

  getOrderDetail: async (orderId: string): Promise<OrderDto> => {
    const res = await apiClient.get(`/orders/${orderId}`);
    return res.data;
  },

  createComplaint: async (
    orderId: string,
    orderItemId: string,
    request: CreateComplaintRequest
  ): Promise<ComplaintDto> => {
    const res = await apiClient.post(`/orders/${orderId}/items/${orderItemId}/complaints`, request);
    return res.data;
  },

  // 5. Kênh Người bán (Seller Portal)
  getMyShop: async () => {
    const res = await apiClient.get('/shop');
    return res.data;
  },

  createShop: async (data: { name: string; slug: string; contactEmail: string; description?: string }) => {
    const res = await apiClient.post('/shops', data);
    return res.data;
  },

  getShopPerformance: async (): Promise<ShopPerformanceDto> => {
    const res = await apiClient.get('/shop/performance');
    return res.data;
  },

  getShopBalance: async (): Promise<ShopBalanceDto> => {
    const res = await apiClient.get('/shop/finance/balance');
    return res.data;
  },

  getShopBankAccount: async (): Promise<ShopBankAccountDto> => {
    const res = await apiClient.get('/shop/bank-account');
    return res.data;
  },

  setShopBankAccount: async (data: {
    bankCode: string;
    bankName: string;
    accountNumber: string;
    accountHolderName: string;
  }): Promise<ShopBankAccountDto> => {
    const res = await apiClient.post('/shop/bank-account', data);
    return res.data;
  },

  requestPayout: async (amount: number): Promise<PayoutDto> => {
    const res = await apiClient.post('/shop/payouts', { amount });
    return res.data;
  },

  getShopPayouts: async (page = 1, pageSize = 15): Promise<{ items: PayoutDto[]; totalCount: number }> => {
    const res = await apiClient.get('/shop/payouts', { params: { page, pageSize } });
    return res.data;
  },

  cancelPayout: async (payoutId: string, reason: string): Promise<PayoutDto> => {
    const res = await apiClient.post(`/shop/payouts/${payoutId}/cancel`, { reason });
    return res.data;
  },

  getShopProducts: async (page = 1, pageSize = 20) => {
    const res = await apiClient.get('/shop/products', { params: { page, pageSize } });
    return res.data;
  },

  createShopProduct: async (data: any) => {
    const res = await apiClient.post('/shop/products', data);
    return res.data;
  },

  publishShopProduct: async (productId: string) => {
    const res = await apiClient.post(`/shop/products/${productId}/publish`);
    return res.data;
  },

  hideShopProduct: async (productId: string) => {
    const res = await apiClient.post(`/shop/products/${productId}/hide`);
    return res.data;
  },

  unhideShopProduct: async (productId: string) => {
    const res = await apiClient.post(`/shop/products/${productId}/unhide`);
    return res.data;
  },

  archiveShopProduct: async (productId: string) => {
    const res = await apiClient.post(`/shop/products/${productId}/archive`);
    return res.data;
  },

  addShopProductImage: async (productId: string, data: { url: string; position?: number; isCover?: boolean }) => {
    const res = await apiClient.post(`/shop/products/${productId}/images`, data);
    return res.data;
  },

  /**
   * Tải tệp hình ảnh sản phẩm từ máy tính lên Cloudinary
   */
  uploadShopProductImageFile: async (file: File) => {
    const formData = new FormData();
    formData.append('file', file);
    const res = await apiClient.post<{ url: string; publicId: string }>('/shop/products/upload-image', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return res.data;
  },

  /**
   * Tải tệp ảnh trực tiếp từ máy tính lên Cloudinary và gắn vào sản phẩm đã có
   */
  uploadAndAddShopProductImage: async (productId: string, file: File, isCover = false, position = 0) => {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('isCover', String(isCover));
    formData.append('position', String(position));
    const res = await apiClient.post(`/shop/products/${productId}/images/upload`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return res.data;
  },

  addDigitalCodesBulk: async (variantId: string, codes: string[]) => {
    const res = await apiClient.post(`/shop/variants/${variantId}/codes/bulk`, { codes });
    return res.data;
  },

  getVariantCodes: async (variantId: string, status?: string, page = 1, pageSize = 20) => {
    const res = await apiClient.get(`/shop/variants/${variantId}/codes`, { params: { status, page, pageSize } });
    return res.data;
  },

  invalidateCode: async (variantId: string, codeId: string) => {
    const res = await apiClient.post(`/shop/variants/${variantId}/codes/${codeId}/invalidate`);
    return res.data;
  },

  getShopOrders: async (status?: string, page = 1, pageSize = 15) => {
    const res = await apiClient.get('/shop/orders', { params: { status, page, pageSize } });
    return res.data;
  },

  getShopStatement: async (page = 1, pageSize = 20, fromDate?: string, toDate?: string) => {
    const res = await apiClient.get('/shop/finance/statement', {
      params: {
        page,
        pageSize,
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
      },
    });
    return res.data;
  },

  closeMyShop: async () => {
    const res = await apiClient.post('/shop/close');
    return res.data;
  },

  openMyShop: async () => {
    const res = await apiClient.post('/shop/open');
    return res.data;
  },

  updateMyShop: async (data: { name?: string; contactEmail?: string; description?: string; logoUrl?: string }) => {
    const res = await apiClient.put('/shop', data);
    return res.data;
  },

  resubmitMyShop: async (data: { name?: string; contactEmail?: string; description?: string; notes?: string }) => {
    const res = await apiClient.post('/shop/resubmit', data);
    return res.data;
  },

  devMockPay: async (checkoutId: string, result: 'success' | 'fail' = 'success') => {
    const res = await apiClient.post(`/dev/checkouts/${checkoutId}/mock-pay`, { result });
    return res.data;
  },

  getShopComplaints: async (page = 1, pageSize = 15): Promise<{ items: ComplaintDto[]; totalCount: number }> => {
    const res = await apiClient.get('/shop/complaints', { params: { page, pageSize } });
    return res.data;
  },

  respondComplaint: async (complaintId: string, accept: boolean, note: string) => {
    const res = await apiClient.post(`/shop/complaints/${complaintId}/respond`, { accept, note });
    return res.data;
  },

  // 6. Chỉnh sửa & Quản lý chi tiết sản phẩm cho Seller
  getShopProductById: async (productId: string): Promise<SellerProductDetailDto> => {
    const res = await apiClient.get(`/shop/products/${productId}`);
    return res.data;
  },

  updateShopProductDetails: async (
    productId: string,
    data: UpdateProductDetailsRequest
  ): Promise<SellerProductDetailDto> => {
    const res = await apiClient.put(`/shop/products/${productId}`, data);
    return res.data;
  },

  updateShopProductVariants: async (
    productId: string,
    data: UpdateProductVariantsRequest
  ): Promise<SellerProductDetailDto> => {
    const res = await apiClient.put(`/shop/products/${productId}/variants`, data);
    return res.data;
  },

  deleteShopProductImage: async (productId: string, imageId: string): Promise<void> => {
    await apiClient.delete(`/shop/products/${productId}/images/${imageId}`);
  },

  reorderShopProductImages: async (
    productId: string,
    images: { imageId: string; position: number; isCover: boolean }[]
  ): Promise<void> => {
    await apiClient.put(`/shop/products/${productId}/images/reorder`, { images });
  },

  // 7. Đánh giá & Nhận xét sản phẩm (Product Reviews & Ratings)
  getProductReviews: async (
    productId: string,
    page = 1,
    pageSize = 10
  ): Promise<{ items: ProductReviewDto[]; totalCount: number; page: number; pageSize: number; totalPages: number }> => {
    const res = await apiClient.get(`/products/${productId}/reviews`, { params: { page, pageSize } });
    return res.data;
  },

  getProductReviewSummary: async (productId: string): Promise<ProductReviewSummaryDto> => {
    const res = await apiClient.get(`/products/${productId}/reviews/summary`);
    return res.data;
  },

  createProductReview: async (
    orderItemId: string,
    data: CreateProductReviewRequest
  ): Promise<ProductReviewDto> => {
    const res = await apiClient.post(`/orders/items/${orderItemId}/reviews`, data);
    return res.data;
  },

  checkHasReviewedOrderItem: async (orderItemId: string): Promise<boolean> => {
    const res = await apiClient.get(`/orders/items/${orderItemId}/reviewed`);
    return res.data.hasReviewed;
  },

  replyProductReview: async (reviewId: string, data: SellerReplyReviewRequest): Promise<ProductReviewDto> => {
    const res = await apiClient.post(`/shop/reviews/${reviewId}/reply`, data);
    return res.data;
  },

  // 8. Tệp tin số có link chữ ký bảo mật (DigitalFile Expiring Signed URL)
  getSignedDownloadUrl: async (orderItemId: string): Promise<SignedDownloadUrlDto> => {
    const res = await apiClient.get(`/orders/items/${orderItemId}/download-url`);
    return res.data;
  },
};
