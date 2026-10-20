export interface Category {
  id: string;
  name: string;
  slug: string;
  parentId?: string | null;
  displayOrder: number;
  children?: Category[];
}

export interface ProductVariant {
  id: string;
  sku: string;
  name?: string;
  combinationKey?: string;
  price: number;
  compareAtPrice?: number | null;
  originalPrice?: number | null;
  deliveryType: string;
  availableStock?: number;
  trackInventory?: boolean;
  optionValues?: string[];
  options?: { [key: string]: string };
}

export interface ProductImage {
  id: string;
  url: string;
  isCover: boolean;
  displayOrder: number;
}

export interface ProductListItem {
  id: string;
  shopId: string;
  shopName: string;
  name: string;
  slug: string;
  categoryName?: string;
  coverImageUrl?: string;
  minPrice: number;
  maxPrice: number;
  variantCount: number;
  ratingAverage?: number;
  salesCount?: number;
  variants?: SellerProductVariantDto[];
}

export interface ProductDetail {
  id: string;
  shopId: string;
  shopName: string;
  shopSlug?: string;
  name: string;
  slug: string;
  description: string;
  categoryId: string;
  categoryName?: string;
  images: ProductImage[];
  variants: ProductVariant[];
  attributes?: { name: string; value: string }[];
}

// Giỏ hàng
export interface CartItemDto {
  id: string;
  shopId: string;
  shopName: string;
  shopSlug?: string;
  productId: string;
  productName: string;
  productSlug?: string;
  variantId: string;
  variantName?: string;
  variantLabel?: string;
  price?: number;
  unitPrice?: number;
  quantity: number;
  imageUrl?: string;
  coverImageUrl?: string;
  availableStock?: number;
  isAvailable?: boolean;
  unavailableReason?: string;
  lineTotal?: number;
}

export interface CartGroupedShop {
  shopId: string;
  shopName: string;
  items: CartItemDto[];
  subtotal: number;
}

export interface CartDto {
  items: CartItemDto[];
  totalAmount?: number;
  estimatedTotal?: number;
  totalQuantity?: number;
  totalItems?: number;
}

// Checkout & Thanh toán
export interface CheckoutItemRequest {
  variantId: string;
  quantity: number;
}

export interface CreateCheckoutRequest {
  selectedVariantIds?: string[];
  items?: CheckoutItemRequest[];
  buyNowItem?: { variantId: string; quantity: number };
  notes?: string;
  idempotencyKey?: string;
}

export interface CheckoutResponse {
  id?: string;
  checkoutId?: string;
  checkoutNo?: string;
  status?: string;
  itemsTotal?: number;
  discountTotal?: number;
  grandTotal?: number;
  totalAmount?: number;
  expiresAtUtc?: string;
  paymentUrl?: string;
  qrCodeUrl?: string;
  accountNumber?: string;
  accountName?: string;
  paymentCode?: string;
}

export interface PaymentStatusResponse {
  paymentId: string;
  status: string; // 'Initiated' | 'Succeeded' | 'Failed' | 'Expired'
  amount: number;
  provider: string;
}

// Đơn hàng & Giao hàng số
export interface DeliveredCredentialDto {
  orderItemId: string;
  productName: string;
  variantName: string;
  codePlain: string; // Mã số bản quyền / license đã giải mã AES
  fileDownloadUrl?: string;
  signedDownloadUrl?: string;
  downloadCount?: number;
  maxDownloads?: number | null;
  instructions?: string;
}

export interface OrderItemDto {
  id: string;
  variantId?: string;
  productId?: string;
  productName: string;
  variantName?: string;
  variantLabel?: string;
  sku?: string;
  price?: number;
  unitPrice?: number;
  quantity: number;
  total?: number;
  lineTotal?: number;
  deliveryType?: string;
  deliveredCodes?: string[];
  downloadFileName?: string;
  downloadStoragePath?: string;
  signedDownloadUrl?: string;
  downloadCount?: number;
  maxDownloads?: number | null;
  credentials?: DeliveredCredentialDto[];
}

export interface SignedDownloadUrlDto {
  orderItemId: string;
  fileName: string;
  downloadUrl: string;
  expiresAtUtc: string;
  downloadCount: number;
  maxDownloads?: number | null;
}

export interface OrderDto {
  id: string;
  orderNo: string;
  shopId: string;
  shopName: string;
  shopSlug?: string;
  status: string; // 'Pending' | 'Paid' | 'Delivered' | 'Completed' | 'Cancelled'
  total?: number;
  totalAmount?: number;
  deliveredAtUtc?: string;
  complaintWindowEndsAtUtc?: string;
  complaintDeadlineUtc?: string;
  createdAtUtc: string;
  items: OrderItemDto[];
  canComplaint?: boolean;
}

// Ví tài chính & Rút tiền Shop
export interface ShopBalanceDto {
  pendingBalance: number;
  availableBalance: number;
  payoutPendingBalance: number;
  lifetimeGrossRevenue: number;
  lifetimeCommissionPaid: number;
}

export interface ShopBankAccountDto {
  id: string;
  bankCode: string;
  bankName: string;
  accountNumberMasked: string;
  accountHolderName: string;
  isLocked: boolean;
  lockedUntilUtc?: string;
}

export interface PayoutDto {
  id: string;
  amount: number;
  fee: number;
  netAmount: number;
  status: string; // 'Requested' | 'Approved' | 'Processing' | 'Succeeded' | 'Failed' | 'Unknown' | 'Rejected' | 'Cancelled'
  bankCode: string;
  bankName: string;
  accountNumberMasked: string;
  accountHolderName: string;
  bankTransactionRef?: string;
  failureReason?: string;
  adminNote?: string;
  createdAtUtc: string;
  completedAtUtc?: string;
}

export interface ShopPerformanceDto {
  totalOrders: number;
  completedOrders: number;
  totalComplaints: number;
  validComplaints: number;
  defectRate: number;
  isHighRisk: boolean;
  performanceTier: string;
}

// Khiếu nại
export interface CreateComplaintRequest {
  reason: number; // enum ComplaintReason
  description: string;
  faultyQuantity: number;
  evidenceUrls: string[];
}

export interface ComplaintDto {
  id: string;
  shopOrderId: string;
  orderItemId: string;
  productName: string;
  variantName: string;
  status: string;
  reason: string;
  description: string;
  shopResponseNote?: string;
  adminResolutionNote?: string;
  createdAtUtc: string;
}

// Bổ sung: Tạo sản phẩm mới
export interface CreateProductInput {
  name: string;
  categoryId?: string;
  description?: string;
  defaultVariant?: {
    price: number;
    compareAtPrice?: number;
    sku?: string;
    deliveryType?: number | string;
    trackInventory?: boolean;
  };
}

// Bổ sung: Đơn hàng phía người bán (Shop Order)
export interface SellerOrderItemDto {
  id: string;
  variantId: string;
  productId: string;
  productName: string;
  variantLabel: string;
  sku: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
  shopNet: number;
  deliveryType: string;
}

export interface SellerOrderDto {
  id: string;
  orderNo: string;
  buyerUserId: string;
  status: string;
  source?: 'Marketplace' | 'ManualEntry' | 'ExcelImport' | string;
  externalCustomerInfo?: string;
  note?: string;
  total: number;
  shopGross: number;
  commissionTotal: number;
  shopNet: number;
  deliveredAtUtc?: string;
  createdAtUtc: string;
  items: SellerOrderItemDto[];
}

// Bổ sung: Nhập đơn hàng bằng Excel & Tạo đơn thủ công
export interface ExcelOrderPreviewRowDto {
  rowIndex: number;
  rawOrderCode?: string;
  rawSkuOrProduct?: string;
  quantity: number;
  unitPrice: number;
  customerEmail?: string;
  customerPhone?: string;
  customerName?: string;
  note?: string;
  isValid: boolean;
  errorMessage?: string;
  matchedVariantId?: string;
  matchedProductId?: string;
  matchedProductName?: string;
  matchedVariantLabel?: string;
  matchedSku?: string;
  availableStock: number;
  matchConfidence: number;
}

export interface ExcelOrderGroupPreviewDto {
  orderGroupKey: string;
  orderNo?: string;
  customerEmail?: string;
  customerPhone?: string;
  customerName?: string;
  note?: string;
  totalAmount: number;
  canBeCreated: boolean;
  blockingErrors: string[];
  items: ExcelOrderPreviewRowDto[];
}

export interface ExcelOrderImportPreviewResultDto {
  totalRows: number;
  validRows: number;
  invalidRows: number;
  totalOrderCount: number;
  readyToCreateOrderCount: number;
  orders: ExcelOrderGroupPreviewDto[];
  allRows: ExcelOrderPreviewRowDto[];
}

export interface ExecuteExcelImportRequestDto {
  skipInvalidOrders: boolean;
  orders: ExcelOrderGroupPreviewDto[];
}

export interface ExecuteExcelImportResultDto {
  successCount: number;
  failedCount: number;
  createdOrderNos: string[];
  errors: string[];
}

export interface CreateManualOrderRequest {
  variantId: string;
  quantity: number;
  customUnitPrice?: number;
  customerEmail: string;
  customerName?: string;
  customerPhone?: string;
  note?: string;
}

// Bổ sung: Quản lý mã kho số chi tiết
export interface DigitalCodeItemDto {
  id: string;
  maskedValue: string;
  status: 'Available' | 'Reserved' | 'Delivered' | 'Invalid';
  createdAtUtc: string;
  reservedAtUtc?: string;
  deliveredAtUtc?: string;
}

// Bổ sung: Sao kê Sổ cái tài chính kép (Double-Entry Ledger Statement)
export interface LedgerStatementItemDto {
  entryId: string;
  transactionId: string;
  transactionType: string;
  accountCode: string;
  accountName: string;
  debit: number;
  credit: number;
  memo: string;
  refType: string;
  refId: string;
  createdAt: string;
}

// Bổ sung: Chỉnh sửa & Quản lý Sản phẩm Người bán
export interface SellerProductImageDto {
  id: string;
  url: string;
  isCover: boolean;
  position: number;
}

export interface SellerProductVariantDto {
  id: string;
  sku: string;
  name?: string;
  combinationKey?: string;
  price: number;
  compareAtPrice?: number | null;
  deliveryType: string;
  trackInventory: boolean;
  availableStock?: number;
  optionValues?: string[];
}

export interface SellerProductOptionDto {
  id: string;
  name: string;
  position: number;
  values: { id: string; value: string; position: number }[];
}

export interface SellerProductDetailDto {
  id: string;
  shopId: string;
  shopName: string;
  shopSlug: string;
  categoryId?: string | null;
  categoryName?: string;
  name: string;
  slug: string;
  description?: string;
  status: string;
  minPrice: number;
  maxPrice: number;
  hasActiveTakedown: boolean;
  activeTakedownReason?: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  images: SellerProductImageDto[];
  variants: SellerProductVariantDto[];
  options: SellerProductOptionDto[];
}

export interface UpdateProductDetailsRequest {
  name: string;
  categoryId?: string | null;
  description?: string;
  price?: number;
  compareAtPrice?: number | null;
}

export interface UpdateVariantPriceRequest {
  price: number;
  compareAtPrice?: number | null;
}

export interface UpdateProductVariantsRequest {
  options: { name: string; position: number; values: string[] }[];
  variants: {
    id?: string;
    sku?: string;
    price: number;
    compareAtPrice?: number | null;
    deliveryType?: number | string;
    trackInventory?: boolean;
    optionValues?: string[];
  }[];
}

export interface ProductReviewDto {
  id: string;
  productId: string;
  productName: string;
  variantLabel: string;
  shopId: string;
  buyerUserId: string;
  buyerName: string;
  buyerAvatar?: string | null;
  rating: number;
  comment: string;
  isVerifiedPurchase: boolean;
  sellerReply?: string | null;
  sellerRepliedAtUtc?: string | null;
  createdAtUtc: string;
}

export interface ProductReviewSummaryDto {
  averageRating: number;
  totalReviews: number;
  starDistribution: { [star: number]: number };
}

export interface CreateProductReviewRequest {
  orderItemId: string;
  rating: number;
  comment: string;
}

export interface SellerReplyReviewRequest {
  reply: string;
}

// Bổ sung: Nhập sản phẩm bằng Excel (Excel Product Import)
export interface ExcelProductPreviewRowDto {
  rowIndex: number;
  rawProductName?: string;
  rawCategoryName?: string;
  rawVariantName?: string;
  rawSku?: string;
  rawPrice?: string;
  rawCompareAtPrice?: string;
  rawDescription?: string;
  rawImageUrl?: string;
  rawInitialCodes?: string;
  rawPublish?: string;
  productName?: string;
  categoryName?: string;
  matchedCategoryId?: string | null;
  matchedCategoryName?: string | null;
  variantName?: string;
  sku?: string | null;
  price: number;
  compareAtPrice?: number | null;
  description?: string | null;
  imageUrl?: string | null;
  initialCodes: string[];
  publishImmediate: boolean;
  isValid: boolean;
  errorMessage?: string | null;
}

export interface ExcelProductGroupPreviewDto {
  groupKey: string;
  productName: string;
  matchedCategoryId?: string | null;
  matchedCategoryName?: string | null;
  description?: string | null;
  imageUrl?: string | null;
  publishImmediate: boolean;
  canBeCreated: boolean;
  blockingErrors: string[];
  variants: ExcelProductPreviewRowDto[];
}

export interface ExcelProductImportPreviewResultDto {
  totalRows: number;
  validRows: number;
  invalidRows: number;
  totalProductCount: number;
  readyToCreateProductCount: number;
  products: ExcelProductGroupPreviewDto[];
  allRows: ExcelProductPreviewRowDto[];
}

export interface ExecuteProductImportRequestDto {
  skipInvalidProducts: boolean;
  products: ExcelProductGroupPreviewDto[];
}

export interface ExecuteProductImportResultDto {
  successCount: number;
  failedCount: number;
  createdProductNames: string[];
  errors: string[];
}


