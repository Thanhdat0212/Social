import { create } from 'zustand';
import type { PostDto } from '@/types';

export type FeedTab = 'for-you' | 'following' | 'recent';

interface FeedTabState {
  posts: PostDto[];
  page: number;
  hasMore: boolean;
  scrollY: number;
}

const initialTabState = (): FeedTabState => ({
  posts: [],
  page: 1,
  hasMore: true,
  scrollY: 0,
});

interface FeedStoreState {
  activeTab: FeedTab;
  tabStates: Record<FeedTab, FeedTabState>;

  setActiveTab: (tab: FeedTab) => void;
  setTabPosts: (tab: FeedTab, posts: PostDto[], page: number, hasMore: boolean) => void;
  appendTabPosts: (tab: FeedTab, newPosts: PostDto[], page: number, hasMore: boolean) => void;
  prependTabPost: (post: PostDto) => void;
  removeTabPost: (postId: string) => void;
  updatePostInFeed: (postId: string, updater: (p: PostDto) => PostDto) => void;
  setTabScrollY: (tab: FeedTab, scrollY: number) => void;
  clearTab: (tab: FeedTab) => void;
}

export const useFeedStore = create<FeedStoreState>((set) => ({
  activeTab: 'for-you',
  tabStates: {
    'for-you': initialTabState(),
    following: initialTabState(),
    recent: initialTabState(),
  },

  setActiveTab: (tab) => set({ activeTab: tab }),

  setTabPosts: (tab, posts, page, hasMore) =>
    set((state) => ({
      tabStates: {
        ...state.tabStates,
        [tab]: {
          ...state.tabStates[tab],
          posts,
          page,
          hasMore,
        },
      },
    })),

  appendTabPosts: (tab, incoming, page, hasMore) =>
    set((state) => {
      const current = state.tabStates[tab].posts;
      if (incoming.length === 0) {
        return {
          tabStates: {
            ...state.tabStates,
            [tab]: {
              ...state.tabStates[tab],
              page,
              hasMore,
            },
          },
        };
      }

      // Tránh lặp ngay lập tức các bài viết vừa hiển thị ở cuối feed
      const recentWindow = current.slice(-Math.max(incoming.length, 5));
      const recentIds = new Set(recentWindow.map((p) => p.id));
      let toAppend = incoming.filter((p) => !recentIds.has(p.id));

      // Khi đã lướt hết toàn bộ bài viết mới và hệ thống tuần hoàn lại (hoặc số bài ít),
      // chấp nhận nạp tiếp incoming để feed tiếp tục cuộn vô hạn không ngắt quãng
      if (toAppend.length === 0) {
        toAppend = incoming;
      }

      return {
        tabStates: {
          ...state.tabStates,
          [tab]: {
            ...state.tabStates[tab],
            posts: [...current, ...toAppend],
            page,
            hasMore,
          },
        },
      };
    }),

  prependTabPost: (newPost) =>
    set((state) => {
      const updated = { ...state.tabStates };
      for (const tab of ['for-you', 'following', 'recent'] as FeedTab[]) {
        const current = updated[tab].posts;
        if (!current.some((p) => p.id === newPost.id)) {
          updated[tab] = {
            ...updated[tab],
            posts: [newPost, ...current],
          };
        }
      }
      return { tabStates: updated };
    }),

  removeTabPost: (postId) =>
    set((state) => {
      const updated = { ...state.tabStates };
      for (const tab of ['for-you', 'following', 'recent'] as FeedTab[]) {
        updated[tab] = {
          ...updated[tab],
          posts: updated[tab].posts.filter((p) => p.id !== postId),
        };
      }
      return { tabStates: updated };
    }),

  updatePostInFeed: (postId, updater) =>
    set((state) => {
      const targetId = postId.toLowerCase();
      const updated = { ...state.tabStates };
      for (const tab of ['for-you', 'following', 'recent'] as FeedTab[]) {
        updated[tab] = {
          ...updated[tab],
          posts: updated[tab].posts.map((p) =>
            p.id.toLowerCase() === targetId ? updater(p) : p
          ),
        };
      }
      return { tabStates: updated };
    }),

  setTabScrollY: (tab, scrollY) =>
    set((state) => ({
      tabStates: {
        ...state.tabStates,
        [tab]: {
          ...state.tabStates[tab],
          scrollY,
        },
      },
    })),

  clearTab: (tab) =>
    set((state) => ({
      tabStates: {
        ...state.tabStates,
        [tab]: initialTabState(),
      },
    })),
}));
