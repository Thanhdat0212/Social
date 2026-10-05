import { apiClient } from './client';
import type {
  CreatePostRequestDto,
  PostDto,
  PostMediaUploadResponseDto,
  CommentDto,
  CreateCommentRequestDto,
} from '@/types';

export interface LikeToggleResponse {
  postId: string;
  isLiked: boolean;
  likeCount: number;
}

export const postApi = {
  /**
   * Lấy bảng tin gợi ý thông minh (For You Feed - 70/20/10)
   */
  getForYouFeed: async (page = 1, pageSize = 20, seenIds?: string[]) => {
    const params: Record<string, any> = { page, pageSize };
    if (seenIds && seenIds.length > 0) {
      params.seenIds = seenIds.join(',');
    }
    const response = await apiClient.get<PostDto[]>('/posts/feed', { params });
    return response.data;
  },

  /**
   * Lấy bảng tin từ các tác giả đang theo dõi (Following Feed)
   */
  getFollowingFeed: async (page = 1, pageSize = 20, seenIds?: string[]) => {
    const params: Record<string, any> = { page, pageSize };
    if (seenIds && seenIds.length > 0) {
      params.seenIds = seenIds.join(',');
    }
    const response = await apiClient.get<PostDto[]>('/posts/following', { params });
    return response.data;
  },

  /**
   * Lấy danh sách bài viết mới nhất
   */
  getRecentPosts: async (page = 1, pageSize = 20, seenIds?: string[]) => {
    const params: Record<string, any> = { page, pageSize };
    if (seenIds && seenIds.length > 0) {
      params.seenIds = seenIds.join(',');
    }
    const response = await apiClient.get<PostDto[]>('/posts', { params });
    return response.data;
  },

  /**
   * Lấy danh sách bài viết của chính người dùng hiện tại đang đăng nhập
   */
  getMyPosts: async (page = 1, pageSize = 20) => {
    const response = await apiClient.get<PostDto[]>('/posts/mine', {
      params: { page, pageSize },
    });
    return response.data;
  },

  /**
   * Lấy danh sách bài viết theo mã tác giả (User Profile)
   */
  getUserPosts: async (userId: string, page = 1, pageSize = 20) => {
    const response = await apiClient.get<PostDto[]>(`/posts/user/${userId}`, {
      params: { page, pageSize },
    });
    return response.data;
  },

  /**
   * Lấy tổng số lượng bài viết của một người dùng
   */
  getUserPostCount: async (userId: string) => {
    const response = await apiClient.get<{ count: number }>(`/posts/user/${userId}/count`);
    return response.data.count;
  },

  /**
   * Lấy chi tiết một bài viết kèm thông tin tác giả và chủ đề AI
   */
  getPostById: async (id: string) => {
    const response = await apiClient.get<PostDto>(`/posts/${id}`);
    return response.data;
  },

  /**
   * Đăng bài viết mới (kích hoạt Gemini AI phân loại ngầm)
   */
  createPost: async (data: CreatePostRequestDto) => {
    const response = await apiClient.post<PostDto>('/posts', data);
    return response.data;
  },

  /**
   * Xóa bài viết
   */
  deletePost: async (id: string) => {
    await apiClient.delete(`/posts/${id}`);
  },

  /**
   * Tải ảnh bài viết lên CDN Cloudinary
   */
  uploadMedia: async (file: File) => {
    const formData = new FormData();
    formData.append('file', file);

    const response = await apiClient.post<PostMediaUploadResponseDto>('/posts/media', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  /**
   * Thích / Bỏ thích bài viết
   */
  toggleLike: async (postId: string) => {
    const response = await apiClient.post<LikeToggleResponse>(`/posts/${postId}/like`);
    return response.data;
  },

  /**
   * Lấy danh sách bình luận của bài viết
   */
  getComments: async (postId: string, page = 1, pageSize = 50) => {
    const response = await apiClient.get<CommentDto[]>(`/posts/${postId}/comments`, {
      params: { page, pageSize },
    });
    return response.data;
  },

  /**
   * Gửi bình luận vào bài viết
   */
  createComment: async (postId: string, data: CreateCommentRequestDto) => {
    const response = await apiClient.post<CommentDto>(`/posts/${postId}/comments`, data);
    return response.data;
  },

  /**
   * Xóa bình luận của chính mình
   */
  deleteComment: async (commentId: string) => {
    await apiClient.delete(`/comments/${commentId}`);
  },
};
