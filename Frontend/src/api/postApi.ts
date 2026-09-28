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
  getForYouFeed: async (page = 1, pageSize = 20) => {
    const response = await apiClient.get<PostDto[]>('/posts/feed', {
      params: { page, pageSize },
    });
    return response.data;
  },

  /**
   * Lấy bảng tin từ các tác giả đang theo dõi (Following Feed)
   */
  getFollowingFeed: async (page = 1, pageSize = 20) => {
    const response = await apiClient.get<PostDto[]>('/posts/following', {
      params: { page, pageSize },
    });
    return response.data;
  },

  /**
   * Lấy danh sách bài viết mới nhất
   */
  getRecentPosts: async (page = 1, pageSize = 20) => {
    const response = await apiClient.get<PostDto[]>('/posts', {
      params: { page, pageSize },
    });
    return response.data;
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
