import { apiClient } from './client';
import type {
  AdminShopItem,
  AdminShopDetail,
  AdminPayoutItem,
  AdminComplaintItem,
  AdminReportItem,
  ReconciliationSummary,
  AdminUserItem,
} from '@/types/admin';

export const adminApi = {
  // 1. Quản lý Gian hàng (Shops)
  getShops: async (params?: { status?: string | number; search?: string; page?: number; pageSize?: number }) => {
    const res = await apiClient.get<{
      total: number;
      page: number;
      pageSize: number;
      items: AdminShopItem[];
      stats?: import('@/types/admin').AdminShopListStats;
    }>('/admin/shops', { params });
    return res.data;
  },

  getShopDetail: async (id: string): Promise<AdminShopDetail> => {
    const res = await apiClient.get(`/admin/shops/${id}`);
    return res.data;
  },

  approveShop: async (id: string): Promise<AdminShopDetail> => {
    const res = await apiClient.post(`/admin/shops/${id}/approve`);
    return res.data;
  },

  rejectShop: async (id: string, data: { reasonCode: string; reasonNote: string; canResubmit: boolean }) => {
    const res = await apiClient.post(`/admin/shops/${id}/reject`, data);
    return res.data;
  },

  addSanction: async (id: string, data: { type: number | string; reason: string; endAt?: string }) => {
    const res = await apiClient.post(`/admin/shops/${id}/sanctions`, data);
    return res.data;
  },

  liftSanction: async (id: string, sanctionId: string) => {
    const res = await apiClient.delete(`/admin/shops/${id}/sanctions/${sanctionId}`);
    return res.data;
  },

  // 2. Hàng đợi Rút tiền (Payouts)
  getPayouts: async (params?: { status?: number | string; page?: number; pageSize?: number }) => {
    const res = await apiClient.get<{ items: AdminPayoutItem[]; totalCount: number }>('/admin/payouts', { params });
    return res.data;
  },

  getPayoutDetail: async (id: string): Promise<AdminPayoutItem> => {
    const res = await apiClient.get(`/admin/payouts/${id}`);
    return res.data;
  },

  approvePayout: async (id: string, note?: string) => {
    const res = await apiClient.post(`/admin/payouts/${id}/approve`, { note });
    return res.data;
  },

  rejectPayout: async (id: string, reason: string) => {
    const res = await apiClient.post(`/admin/payouts/${id}/reject`, { reason });
    return res.data;
  },

  markPaidPayout: async (id: string, bankReferenceCode: string, note?: string) => {
    const res = await apiClient.post(`/admin/payouts/${id}/mark-paid`, { bankReferenceCode, note });
    return res.data;
  },

  markUnknownPayout: async (id: string, incidentNote: string) => {
    const res = await apiClient.post(`/admin/payouts/${id}/mark-unknown`, { incidentNote });
    return res.data;
  },

  // 3. Phân xử Khiếu nại (Complaints)
  getComplaints: async (params?: { status?: number | string; page?: number; pageSize?: number }) => {
    const res = await apiClient.get<{ items: AdminComplaintItem[]; totalCount: number }>('/admin/complaints', {
      params,
    });
    return res.data;
  },

  resolveComplaint: async (
    id: string,
    data: {
      action: 'Refund' | 'Reject' | 'Replace';
      decisionNote: string;
      refundBearer?: 'Shop' | 'Platform' | 'Shared';
    }
  ) => {
    const res = await apiClient.post(`/admin/complaints/${id}/resolve`, data);
    return res.data;
  },

  // 4. Báo cáo vi phạm (Abuse & Reports)
  getReports: async (params?: { status?: number | string; page?: number; pageSize?: number }) => {
    const res = await apiClient.get<{ items: AdminReportItem[]; totalCount: number }>('/admin/reports', { params });
    return res.data;
  },

  resolveReport: async (
    id: string,
    data: { action: 'Takedown' | 'WarnShop' | 'Dismiss'; resolutionNote: string }
  ) => {
    const res = await apiClient.post(`/admin/reports/${id}/resolve`, data);
    return res.data;
  },

  // 5. Kiểm toán & Đối soát Sổ cái (Ledger Audit)
  getReconciliation: async (): Promise<ReconciliationSummary> => {
    const res = await apiClient.get('/admin/audit/reconciliation');
    return res.data;
  },

  fixShopBalance: async (shopId: string) => {
    const res = await apiClient.post(`/admin/audit/fix-balance/${shopId}`);
    return res.data;
  },

  // 6. Quản lý Người dùng (Users)
  getUsers: async (params?: { search?: string; role?: number | string; status?: number | string; page?: number; pageSize?: number }) => {
    const res = await apiClient.get<{ total: number; page: number; pageSize: number; items: AdminUserItem[] }>(
      '/admin/users',
      { params }
    );
    return res.data;
  },

  updateUserStatus: async (userId: string, status: 'Active' | 'Suspended' | number) => {
    const res = await apiClient.patch(`/admin/users/${userId}/status`, { status });
    return res.data;
  },

  updateUserRole: async (userId: string, role: 'Admin' | 'Member' | number) => {
    const res = await apiClient.patch(`/admin/users/${userId}/role`, { role });
    return res.data;
  },

  // 7. Cây danh mục & Thuộc tính (Categories & Attributes)
  getCategoriesTree: async () => {
    const res = await apiClient.get<import('@/types/admin').AdminCategoryNode[]>('/admin/categories/tree');
    return res.data;
  },

  createCategory: async (data: {
    name: string;
    slug?: string;
    parentId?: string | null;
    displayOrder?: number;
    productType?: number;
    moderationPolicy?: number;
    commissionRateBp?: number;
  }) => {
    const res = await apiClient.post<import('@/types/admin').AdminCategoryDetail>('/admin/categories', data);
    return res.data;
  },

  updateCategory: async (
    id: string,
    data: {
      name: string;
      slug: string;
      displayOrder: number;
      isActive: boolean;
      productType: number;
      moderationPolicy: number;
      commissionRateBp?: number | null;
    }
  ) => {
    const res = await apiClient.put<import('@/types/admin').AdminCategoryDetail>(`/admin/categories/${id}`, data);
    return res.data;
  },

  deleteCategory: async (id: string) => {
    const res = await apiClient.delete(`/admin/categories/${id}`);
    return res.data;
  },

  getAttributes: async () => {
    const res = await apiClient.get<import('@/types/admin').AdminAttributeDefinition[]>('/admin/attributes');
    return res.data;
  },

  createAttribute: async (data: {
    code: string;
    name: string;
    dataType: number;
    unit?: string;
    min?: number;
    max?: number;
    initialOptions?: string[];
  }) => {
    const res = await apiClient.post<import('@/types/admin').AdminAttributeDefinition>('/admin/attributes', data);
    return res.data;
  },

  updateAttribute: async (
    id: string,
    data: {
      name: string;
      dataType: number;
      unit?: string;
      min?: number;
      max?: number;
    }
  ) => {
    const res = await apiClient.put<import('@/types/admin').AdminAttributeDefinition>(`/admin/attributes/${id}`, data);
    return res.data;
  },

  addAttributeOption: async (id: string, data: { value: string; slug?: string; displayOrder?: number }) => {
    const res = await apiClient.post(`/admin/attributes/${id}/options`, data);
    return res.data;
  },

  deleteAttributeOption: async (optionId: string) => {
    const res = await apiClient.delete(`/admin/attributes/options/${optionId}`);
    return res.data;
  },

  assignCategoryAttribute: async (
    id: string,
    data: {
      attributeId: string;
      requirement: number;
      isFilterable: boolean;
      isVariantAxis: boolean;
    }
  ) => {
    const res = await apiClient.post(`/admin/categories/${id}/attributes`, data);
    return res.data;
  },

  removeCategoryAttribute: async (id: string, attributeId: string) => {
    const res = await apiClient.delete(`/admin/categories/${id}/attributes/${attributeId}`);
    return res.data;
  },

  getCategoryRequests: async (params?: { status?: number | string; page?: number; pageSize?: number }) => {
    const res = await apiClient.get<{ items: import('@/types/admin').AdminCategoryRequestItem[]; total: number; page: number; pageSize: number }>(
      '/admin/category-requests',
      { params }
    );
    return res.data;
  },

  reviewCategoryRequest: async (
    id: string,
    data: { isApproved: boolean; resolvedCategoryId?: string; adminNote?: string }
  ) => {
    const res = await apiClient.post(`/admin/category-requests/${id}/review`, data);
    return res.data;
  },

  // 8. Danh mục Sản phẩm & Takedown (Products & Direct Moderation)
  getAdminProducts: async (params?: {
    shopId?: string;
    status?: number | string;
    search?: string;
    page?: number;
    pageSize?: number;
  }) => {
    const res = await apiClient.get<{
      items: import('@/types/admin').AdminProductItem[];
      totalCount: number;
      page: number;
      pageSize: number;
      totalPages: number;
    }>('/admin/products', { params });
    return res.data;
  },

  takedownProduct: async (id: string, reason: string) => {
    const res = await apiClient.post(`/admin/products/${id}/takedown`, { reason });
    return res.data;
  },

  liftTakedown: async (id: string) => {
    const res = await apiClient.delete(`/admin/products/${id}/takedown`);
    return res.data;
  },
};

