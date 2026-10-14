export interface AdminShopItem {
  id: string;
  name: string;
  slug: string;
  contactEmail: string;
  logoUrl?: string;
  description?: string;
  approvalStatus: 'Pending' | 'Approved' | 'Rejected' | number;
  operatingStatus: 'Open' | 'Closed' | number;
  effectiveStatus?: string;
  ownerUserId: string;
  createdAtUtc: string;
  activeSanctionCount?: number;
}

export interface AdminShopDetail {
  id: string;
  name: string;
  slug: string;
  contactEmail: string;
  logoUrl?: string;
  description?: string;
  approvalStatus: string;
  operatingStatus: string;
  effectiveStatus: string;
  ownerUserId: string;
  createdAtUtc: string;
  reviews?: Array<{
    id: string;
    submissionNo: number;
    decision: string;
    reasonCode?: string;
    reasonNote?: string;
    reviewedAt?: string;
    snapshotJson?: string;
  }>;
  sanctions?: Array<{
    id: string;
    type: string;
    reason: string;
    startAt: string;
    endAt?: string;
    liftedAt?: string;
  }>;
}

export interface AdminPayoutItem {
  id: string;
  shopId: string;
  shopName?: string;
  amount: number;
  fee: number;
  status: 'Requested' | 'Approved' | 'Processing' | 'Succeeded' | 'Failed' | 'Rejected' | 'Cancelled' | 'Unknown' | number;
  bankCode?: string;
  bankAccountHolderName?: string;
  bankAccountNumberMasked?: string;
  requestedAt: string;
  rejectReason?: string;
}

export interface AdminComplaintItem {
  id: string;
  shopOrderId: string;
  orderItemId: string;
  buyerUserId: string;
  buyerDisplayName?: string;
  shopId: string;
  shopName?: string;
  productName: string;
  reasonCode: string;
  description: string;
  status: 'Open' | 'UnderReview' | 'EscalatedToAdmin' | 'ResolvedRefund' | 'ResolvedReplacement' | 'Rejected' | number;
  shopDeadlineAt: string;
  createdAtUtc: string;
  evidenceUrls?: string[];
}

export interface AdminReportItem {
  id: string;
  reporterUserId: string;
  reporterName?: string;
  productId: string;
  productName?: string;
  shopId?: string;
  shopName?: string;
  reason: number;
  reasonText?: string;
  description: string;
  evidenceUrl?: string;
  status: 'Pending' | 'ResolvedActionTaken' | 'Dismissed' | number;
  createdAtUtc: string;
}

export interface ReconciliationSummary {
  runAtUtc: string;
  isBalanced: boolean;
  totalDebit: number;
  totalCredit: number;
  discrepancy: number;
  activePayoutsTotal: number;
  payoutPendingBalance: number;
  payoutsMatch: boolean;
  totalShopsAudited: number;
  discrepantShopCount: number;
  discrepantShops: Array<{
    shopId: string;
    shopName: string;
    cachedAvailable: number;
    computedAvailable: number;
    difference: number;
  }>;
}

export interface AdminUserItem {
  id: string;
  email: string;
  displayName: string;
  avatarUrl?: string;
  emailConfirmed: boolean;
  role: 'Admin' | 'Member';
  status: 'Active' | 'Suspended';
  createdAtUtc: string;
}

export interface AdminCategoryNode {
  id: string;
  parentId?: string | null;
  name: string;
  slug: string;
  path: string;
  depth: number;
  displayOrder: number;
  isActive: boolean;
  productType: string;
  moderationPolicy: string;
  commissionRateBp?: number | null;
  children: AdminCategoryNode[];
}

export interface AdminCategoryDetail {
  id: string;
  parentId?: string | null;
  parentName?: string | null;
  name: string;
  slug: string;
  path: string;
  depth: number;
  displayOrder: number;
  isActive: boolean;
  productType: string;
  moderationPolicy: string;
  commissionRateBp?: number | null;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  attributes: AdminCategoryAttribute[];
}

export interface AdminCategoryAttribute {
  attributeId: string;
  code: string;
  name: string;
  dataType: string;
  unit?: string | null;
  requirement: string;
  isFilterable: boolean;
  isVariantAxis: boolean;
  inheritedFromCategoryId?: string | null;
  options: AdminAttributeOption[];
}

export interface AdminAttributeOption {
  id: string;
  attributeId: string;
  value: string;
  slug: string;
  displayOrder: number;
  isActive: boolean;
}

export interface AdminAttributeDefinition {
  id: string;
  code: string;
  name: string;
  dataType: string;
  unit?: string | null;
  min?: number | null;
  max?: number | null;
  options: AdminAttributeOption[];
}

export interface AdminCategoryRequestItem {
  id: string;
  shopId: string;
  shopName: string;
  name: string;
  suggestedParentId?: string | null;
  suggestedParentName?: string | null;
  reasonNote?: string | null;
  status: string;
  adminNote?: string | null;
  resolvedCategoryId?: string | null;
  resolvedCategoryName?: string | null;
  createdAtUtc: string;
  reviewedAtUtc?: string | null;
}

export interface AdminProductItem {
  id: string;
  name: string;
  slug: string;
  categoryId?: string | null;
  categoryName?: string | null;
  coverImageUrl?: string | null;
  status: string;
  minPrice: number;
  maxPrice: number;
  variantCount: number;
  createdAtUtc: string;
  hasActiveTakedown?: boolean;
  shopName?: string | null;
}

