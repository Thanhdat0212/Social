import { create } from 'zustand';
import { marketplaceApi } from '@/api/marketplaceApi';
import type { CartItemDto } from '@/types/marketplace';

interface CartState {
  items: CartItemDto[];
  totalAmount: number;
  totalQuantity: number;
  isLoading: boolean;
  error: string | null;

  fetchCart: () => Promise<void>;
  addItem: (variantId: string, quantity?: number) => Promise<boolean>;
  updateItemQuantity: (itemId: string, quantity: number) => Promise<void>;
  removeItem: (itemId: string) => Promise<void>;
  clearCart: () => void;
}

export const useCartStore = create<CartState>((set, get) => ({
  items: [],
  totalAmount: 0,
  totalQuantity: 0,
  isLoading: false,
  error: null,

  fetchCart: async () => {
    set({ isLoading: true, error: null });
    try {
      const data = await marketplaceApi.getCart();
      set({
        items: data.items || [],
        totalAmount: data.estimatedTotal ?? data.totalAmount ?? 0,
        totalQuantity: data.totalItems ?? data.totalQuantity ?? 0,
        isLoading: false,
      });
    } catch {
      // Người dùng chưa đăng nhập hoặc giỏ rỗng
      set({ isLoading: false });
    }
  },

  addItem: async (variantId: string, quantity = 1): Promise<boolean> => {
    set({ isLoading: true, error: null });
    try {
      const data = await marketplaceApi.addToCart(variantId, quantity);
      set({
        items: data.items || [],
        totalAmount: data.estimatedTotal ?? data.totalAmount ?? 0,
        totalQuantity: data.totalItems ?? data.totalQuantity ?? 0,
        isLoading: false,
      });
      return true;
    } catch (err: any) {
      const msg = err.response?.data?.detail || err.response?.data?.message || 'Không thể thêm sản phẩm vào giỏ hàng.';
      set({ error: msg, isLoading: false });
      return false;
    }
  },

  updateItemQuantity: async (itemId: string, quantity: number) => {
    if (quantity <= 0) {
      await get().removeItem(itemId);
      return;
    }
    set({ isLoading: true, error: null });
    try {
      const data = await marketplaceApi.updateCartItem(itemId, quantity);
      set({
        items: data.items || [],
        totalAmount: data.estimatedTotal ?? data.totalAmount ?? 0,
        totalQuantity: data.totalItems ?? data.totalQuantity ?? 0,
        isLoading: false,
      });
    } catch (err: any) {
      set({
        error: err.response?.data?.detail || err.response?.data?.message || 'Không thể cập nhật số lượng.',
        isLoading: false,
      });
    }
  },

  removeItem: async (itemId: string) => {
    set({ isLoading: true, error: null });
    try {
      const data = await marketplaceApi.removeCartItem(itemId);
      set({
        items: data.items || [],
        totalAmount: data.estimatedTotal ?? data.totalAmount ?? 0,
        totalQuantity: data.totalItems ?? data.totalQuantity ?? 0,
        isLoading: false,
      });
    } catch (err: any) {
      set({
        error: err.response?.data?.detail || err.response?.data?.message || 'Không thể xóa sản phẩm khỏi giỏ.',
        isLoading: false,
      });
    }
  },

  clearCart: () => {
    set({ items: [], totalAmount: 0, totalQuantity: 0, error: null });
  },
}));
