const STORAGE_KEY = 'social_seen_post_ids';
const MAX_SEEN_IDS = 300;

export const seenPostsService = {
  /**
   * Lấy toàn bộ danh sách ID các bài viết đã xem từ localStorage
   */
  getSeenIds(): string[] {
    try {
      const data = localStorage.getItem(STORAGE_KEY);
      if (!data) return [];
      const parsed = JSON.parse(data);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  },

  /**
   * Lấy danh sách ID đã xem gần nhất (giới hạn số lượng) để gửi lên API
   */
  getRecentSeenIds(limit: number = 60): string[] {
    const ids = this.getSeenIds();
    return ids.slice(0, limit);
  },

  /**
   * Đánh dấu một bài viết là đã xem
   */
  markAsSeen(id: string): void {
    if (!id) return;
    try {
      const current = this.getSeenIds();
      // Đưa ID mới xem lên đầu danh sách để giữ thứ tự thời gian xem
      const filtered = current.filter((item) => item !== id);
      filtered.unshift(id);
      if (filtered.length > MAX_SEEN_IDS) {
        filtered.length = MAX_SEEN_IDS;
      }
      localStorage.setItem(STORAGE_KEY, JSON.stringify(filtered));
    } catch (e) {
      console.warn('[SeenPostsService] Failed to save seen post id', e);
    }
  },

  /**
   * Đánh dấu hàng loạt bài viết là đã xem
   */
  markMultipleAsSeen(ids: string[]): void {
    if (!ids || ids.length === 0) return;
    try {
      const current = this.getSeenIds();
      const set = new Set(ids);
      const remaining = current.filter((item) => !set.has(item));
      const combined = [...ids, ...remaining];
      if (combined.length > MAX_SEEN_IDS) {
        combined.length = MAX_SEEN_IDS;
      }
      localStorage.setItem(STORAGE_KEY, JSON.stringify(combined));
    } catch (e) {
      console.warn('[SeenPostsService] Failed to save multiple seen post ids', e);
    }
  },

  /**
   * Kiểm tra bài viết đã xem hay chưa
   */
  hasSeen(id: string): boolean {
    return this.getSeenIds().includes(id);
  },

  /**
   * Xóa lịch sử bài đã xem
   */
  clear(): void {
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch (e) {
      console.warn('[SeenPostsService] Failed to clear seen posts', e);
    }
  },
};
