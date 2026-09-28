export interface CommentAuthorDto {
  id: string;
  displayName: string;
  avatarUrl?: string | null;
}

export interface CommentDto {
  id: string;
  postId: string;
  parentCommentId?: string | null;
  content: string;
  createdAtUtc: string;
  author: CommentAuthorDto;
  replies: CommentDto[];
}

export interface CreateCommentRequestDto {
  content: string;
  parentCommentId?: string | null;
}
