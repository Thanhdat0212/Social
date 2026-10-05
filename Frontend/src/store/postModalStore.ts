import { create } from 'zustand';

interface PostModalState {
  activePostId: string | null;
  openPostModal: (postId: string) => void;
  closePostModal: () => void;
}

export const usePostModalStore = create<PostModalState>((set) => ({
  activePostId: null,
  openPostModal: (postId: string) => set({ activePostId: postId }),
  closePostModal: () => set({ activePostId: null }),
}));
