import React, { useEffect, useState } from 'react';
import type { CommentDto } from '@/types';
import { postApi } from '@/api/postApi';
import { useAuth } from '@/hooks/useAuth';
import { getApiErrorMessage } from '@/utils/error';

interface CommentSectionProps {
  postId: string;
  onCommentCountChange?: (newCount: number) => void;
}

const formatCommentTime = (utcDateString: string): string => {
  const date = new Date(utcDateString);
  const now = new Date();
  const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);

  if (diffInSeconds < 60) return 'Vừa xong';
  if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)} phút`;
  if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)} giờ`;
  return `${Math.floor(diffInSeconds / 86400)} ngày`;
};

export const CommentSection: React.FC<CommentSectionProps> = ({
  postId,
  onCommentCountChange,
}) => {
  const { isAuthenticated, user } = useAuth();
  const [comments, setComments] = useState<CommentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [content, setContent] = useState('');
  const [replyingTo, setReplyingTo] = useState<CommentDto | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;
    const loadComments = async () => {
      setLoading(true);
      setError(null);
      try {
        const data = await postApi.getComments(postId);
        if (isMounted) {
          setComments(data);
        }
      } catch (err) {
        if (isMounted) {
          setError(getApiErrorMessage(err));
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };

    loadComments();
    return () => {
      isMounted = false;
    };
  }, [postId]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!content.trim() || submitting) return;

    setSubmitting(true);
    setError(null);

    try {
      const newComment = await postApi.createComment(postId, {
        content: content.trim(),
        parentCommentId: replyingTo?.id || null,
      });

      if (replyingTo) {
        // Gắn vào danh sách replies của comment cha
        setComments((prev) =>
          prev.map((c) => {
            if (c.id === replyingTo.id) {
              return {
                ...c,
                replies: [...(c.replies || []), newComment],
              };
            }
            return c;
          })
        );
      } else {
        // Comment cấp 1
        setComments((prev) => [newComment, ...prev]);
      }

      setContent('');
      setReplyingTo(null);

      // Cập nhật đếm tổng comment
      const totalCount = comments.reduce(
        (acc, curr) => acc + 1 + (curr.replies?.length || 0),
        1
      );
      onCommentCountChange?.(totalCount);
    } catch (err) {
      setError(getApiErrorMessage(err));
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (commentId: string, isReply: boolean, parentId?: string) => {
    if (!window.confirm('Bạn có chắc muốn xóa bình luận này?')) return;

    try {
      await postApi.deleteComment(commentId);

      if (isReply && parentId) {
        setComments((prev) =>
          prev.map((c) => {
            if (c.id === parentId) {
              return {
                ...c,
                replies: c.replies.filter((r) => r.id !== commentId),
              };
            }
            return c;
          })
        );
      } else {
        setComments((prev) => prev.filter((c) => c.id !== commentId));
      }
    } catch {
      alert('Không thể xóa bình luận.');
    }
  };

  return (
    <div className="post-comment-section">
      {/* Comment Input Box */}
      {isAuthenticated ? (
        <form className="comment-form" onSubmit={handleSubmit}>
          {replyingTo && (
            <div className="replying-notice">
              <span>Đang trả lời <strong>{replyingTo.author.displayName}</strong></span>
              <button
                type="button"
                className="btn-cancel-reply"
                onClick={() => setReplyingTo(null)}
              >
                ✕ Hủy
              </button>
            </div>
          )}
          <div className="comment-input-wrap">
            <input
              type="text"
              className="form-control comment-input"
              placeholder={
                replyingTo
                  ? `Trả lời ${replyingTo.author.displayName}...`
                  : 'Viết bình luận của bạn...'
              }
              value={content}
              onChange={(e) => setContent(e.target.value)}
              disabled={submitting}
              maxLength={2000}
            />
            <button
              type="submit"
              className="btn btn-primary btn-sm comment-submit-btn"
              disabled={!content.trim() || submitting}
            >
              {submitting ? '...' : 'Gửi'}
            </button>
          </div>
        </form>
      ) : (
        <div className="comment-login-prompt">
          <span>Đăng nhập để tham gia bình luận và thảo luận.</span>
        </div>
      )}

      {error && <div className="comment-error-alert">{error}</div>}

      {/* Comment List */}
      <div className="comments-list">
        {loading ? (
          <div className="comments-loading">Đang tải bình luận...</div>
        ) : comments.length === 0 ? (
          <div className="comments-empty">Chưa có bình luận nào. Hãy là người đầu tiên chia sẻ ý kiến!</div>
        ) : (
          comments.map((comment) => (
            <div key={comment.id} className="comment-item">
              <div className="comment-header-row">
                <div className="comment-author-group">
                  <div className="avatar comment-avatar">
                    {comment.author.avatarUrl ? (
                      <img src={comment.author.avatarUrl} alt={comment.author.displayName} />
                    ) : (
                      <span>{comment.author.displayName.charAt(0)}</span>
                    )}
                  </div>
                  <div>
                    <span className="comment-author-name">{comment.author.displayName}</span>
                    <span className="comment-time">{formatCommentTime(comment.createdAtUtc)}</span>
                  </div>
                </div>

                {user?.id === comment.author.id && (
                  <button
                    type="button"
                    className="btn-delete-comment"
                    onClick={() => handleDelete(comment.id, false)}
                    title="Xóa bình luận"
                  >
                    ✕
                  </button>
                )}
              </div>

              <div className="comment-body-text">{comment.content}</div>

              {isAuthenticated && (
                <div className="comment-actions-bar">
                  <button
                    type="button"
                    className="btn-reply-link"
                    onClick={() => {
                      setReplyingTo(comment);
                      setContent('');
                    }}
                  >
                    💬 Trả lời
                  </button>
                </div>
              )}

              {/* Nested Replies */}
              {comment.replies && comment.replies.length > 0 && (
                <div className="comment-replies-list">
                  {comment.replies.map((reply) => (
                    <div key={reply.id} className="reply-item">
                      <div className="comment-header-row">
                        <div className="comment-author-group">
                          <div className="avatar reply-avatar">
                            {reply.author.avatarUrl ? (
                              <img src={reply.author.avatarUrl} alt={reply.author.displayName} />
                            ) : (
                              <span>{reply.author.displayName.charAt(0)}</span>
                            )}
                          </div>
                          <div>
                            <span className="comment-author-name">{reply.author.displayName}</span>
                            <span className="comment-time">{formatCommentTime(reply.createdAtUtc)}</span>
                          </div>
                        </div>

                        {user?.id === reply.author.id && (
                          <button
                            type="button"
                            className="btn-delete-comment"
                            onClick={() => handleDelete(reply.id, true, comment.id)}
                            title="Xóa phản hồi"
                          >
                            ✕
                          </button>
                        )}
                      </div>

                      <div className="comment-body-text">{reply.content}</div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          ))
        )}
      </div>
    </div>
  );
};
