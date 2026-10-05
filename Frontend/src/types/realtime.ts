import type { CommentDto } from './comment';
import type { PostDto } from './post';

export interface PostLikeEvent {
  postId: string;
  likeCount: number;
  userId: string;
  isLiked: boolean;
}

export interface CommentAddedEvent {
  postId: string;
  totalCommentCount: number;
  comment: CommentDto;
}

export interface UserNotificationEvent {
  id: string;
  type: 'like' | 'comment' | 'new_post' | 'follow' | string;
  message: string;
  triggeredByUserId?: string;
  triggeredByUserName?: string;
  triggeredByUserAvatar?: string;
  targetPostId?: string;
  createdAtUtc: string;
}

export interface RealtimeEvents {
  ReceiveNewPost: (post: PostDto) => void;
  PostLiked: (event: PostLikeEvent) => void;
  CommentAdded: (event: CommentAddedEvent) => void;
  ReceiveNotification: (notification: UserNotificationEvent) => void;
}
