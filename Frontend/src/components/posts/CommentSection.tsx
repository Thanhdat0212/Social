import React, { useEffect, useState } from 'react';
import type { CommentDto, CommentAddedEvent } from '@/types';
import { postApi } from '@/api/postApi';
import { useAuth } from '@/hooks/useAuth';
import { useSignalR } from '@/hooks/useSignalR';
import { useNotificationStore } from '@/store';
import { signalrService } from '@/services/signalrService';
import { getApiErrorMessage } from '@/utils/error';
import { formatRelativeTime } from '@/utils/date';
import {
  Avatar,
  Button,
  CloseIcon,
  SendIcon,
  TrashIcon,
  ConfirmModal,
} from '@/components/common';

interface CommentSectionProps {
  postId: string;
  onCommentCountChange?: (newCount: number) => void;
}

export const CommentSection: React.FC<CommentSectionProps> = ({
  postId,
  onCommentCountChange,
}) => {
  const { isAuthenticated, user } = useAuth();
  const { showToast } = useNotificationStore();

  const [comments, setComments] = useState<CommentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [content, setContent] = useState('');
  const [replyingTo, setReplyingTo] = useState<CommentDto | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Modal xóa comment
  const [deleteTarget, setDeleteTarget] = useState<{
    id: string;
    isReply: boolean;
    parentId?: string;
  } | null>(null);
  const [deleting, setDeleting] = useState(false);

  // Tham gia nhóm realtime của bài viết này khi mở bình luận
  useEffect(() => {
    signalrService.joinPostGroup(postId);
    return () => {
      signalrService.leavePostGroup(postId);
    };
  }, [postId]);

  // Lắng nghe bình luận mới từ SignalR
  useSignalR<CommentAddedEvent>('CommentAdded', (event) => {
    if (event?.postId !== postId || !event?.comment) return;
    const incoming = event.comment;

    setComments((prev) => {
      const exists = prev.some(
        (c) => c.id === incoming.id || c.replies?.some((r) => r.id === incoming.id)
      );
      if (exists) return prev;

      if (incoming.parentCommentId) {
        return prev.map((c) => {
          if (c.id === incoming.parentCommentId) {
            const replyExists = c.replies?.some((r) => r.id === incoming.id);
            if (replyExists) return c;
            return {
              ...c,
              replies: [...(c.replies || []), incoming],
            };
          }
          return c;
        });
      }

      return [incoming, ...prev];
    });

    onCommentCountChange?.(event.totalCommentCount);
  });

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
        setComments((prev) => [newComment, ...prev]);
      }

      setContent('');
      setReplyingTo(null);

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

  const handleConfirmDelete = async () => {
    if (!deleteTarget) return;

    setDeleting(true);
    try {
      await postApi.deleteComment(deleteTarget.id);

      if (deleteTarget.isReply && deleteTarget.parentId) {
        setComments((prev) =>
          prev.map((c) => {
            if (c.id === deleteTarget.parentId) {
              return {
                ...c,
                replies: c.replies.filter((r) => r.id !== deleteTarget.id),
              };
            }
            return c;
          })
        );
      } else {
        setComments((prev) => prev.filter((c) => c.id !== deleteTarget.id));
      }

      showToast('Đã xóa bình luận thành công.', 'info');
      setDeleteTarget(null);
    } catch {
      showToast('Không thể xóa bình luận.', 'error');
    } finally {
      setDeleting(false);
    }
  };

  return (
    <div className="comments-thread-wrap">
      {/* Comment Composer */}
      {isAuthenticated ? (
        <form className="comment-composer-form" onSubmit={handleSubmit}>
          {replyingTo && (
            <div className="replying-banner">
              <span>
                Đang trả lời <strong>{replyingTo.author.displayName}</strong>
              </span>
              <button
                type="button"
                className="btn-cancel-reply"
                onClick={() => setReplyingTo(null)}
                aria-label="Hủy trả lời"
              >
                <CloseIcon size={12} />
                <span>Hủy</span>
              </button>
            </div>
          )}

          <div className="comment-input-row">
            <Avatar
              src={user?.avatarUrl}
              name={user?.displayName}
              size="sm"
              className="comment-user-avatar"
            />
            <div className="comment-field-group">
              <input
                type="text"
                className="comment-field"
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
              <Button
                type="submit"
                variant="primary"
                size="sm"
                disabled={!content.trim() || submitting}
                loading={submitting}
                iconRight={<SendIcon size={12} />}
              >
                Gửi
              </Button>
            </div>
          </div>
        </form>
      ) : (
        <div className="comment-login-hint">
          <span>Vui lòng đăng nhập để tham gia thảo luận.</span>
        </div>
      )}

      {error && <div className="comment-error-banner">{error}</div>}

      {/* Comments List */}
      <div className="comments-tree">
        {loading ? (
          <div className="comments-loading-state">
            <span className="spinner-inline" />
            <span>Đang tải bình luận...</span>
          </div>
        ) : comments.length === 0 ? (
          <div className="comments-empty-state">
            <p>Chưa có bình luận nào. Hãy là người đầu tiên chia sẻ cảm nghĩ!</p>
          </div>
        ) : (
          comments.map((comment) => (
            <div key={comment.id} className="comment-block">
              {/* Parent Comment */}
              <div className="comment-bubble">
                <Avatar
                  src={comment.author.avatarUrl}
                  name={comment.author.displayName}
                  size="sm"
                  className="comment-author-avatar"
                />

                <div className="comment-body-col">
                  <div className="comment-card-box">
                    <div className="comment-meta-row">
                      <span className="comment-author-name">{comment.author.displayName}</span>
                      <time className="comment-post-time">
                        {formatRelativeTime(comment.createdAtUtc)}
                      </time>
                    </div>
                    <p className="comment-text-content">{comment.content}</p>
                  </div>

                  {/* Actions (Reply, Delete) */}
                  <div className="comment-action-links">
                    {isAuthenticated && (
                      <button
                        type="button"
                        className="comment-link-action"
                        onClick={() => {
                          setReplyingTo(comment);
                          setContent('');
                        }}
                      >
                        Trả lời
                      </button>
                    )}

                    {user?.id === comment.author.id && (
                      <button
                        type="button"
                        className="comment-link-action action-delete"
                        onClick={() =>
                          setDeleteTarget({
                            id: comment.id,
                            isReply: false,
                          })
                        }
                        title="Xóa bình luận này"
                      >
                        <TrashIcon size={12} />
                        <span>Xóa</span>
                      </button>
                    )}
                  </div>
                </div>
              </div>

              {/* Nested Replies */}
              {comment.replies && comment.replies.length > 0 && (
                <div className="comment-replies-thread">
                  {comment.replies.map((reply) => (
                    <div key={reply.id} className="comment-bubble reply-bubble">
                      <Avatar
                        src={reply.author.avatarUrl}
                        name={reply.author.displayName}
                        size="xs"
                        className="comment-author-avatar"
                      />

                      <div className="comment-body-col">
                        <div className="comment-card-box reply-card-box">
                          <div className="comment-meta-row">
                            <span className="comment-author-name">
                              {reply.author.displayName}
                            </span>
                            <time className="comment-post-time">
                              {formatRelativeTime(reply.createdAtUtc)}
                            </time>
                          </div>
                          <p className="comment-text-content">{reply.content}</p>
                        </div>

                        {user?.id === reply.author.id && (
                          <div className="comment-action-links">
                            <button
                              type="button"
                              className="comment-link-action action-delete"
                              onClick={() =>
                                setDeleteTarget({
                                  id: reply.id,
                                  isReply: true,
                                  parentId: comment.id,
                                })
                              }
                              title="Xóa phản hồi này"
                            >
                              <TrashIcon size={12} />
                              <span>Xóa</span>
                            </button>
                          </div>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          ))
        )}
      </div>

      {/* Modal xác nhận xóa bình luận */}
      <ConfirmModal
        isOpen={Boolean(deleteTarget)}
        title="Xóa bình luận"
        message="Bạn có chắc chắn muốn xóa bình luận này không?"
        confirmText="Xóa"
        cancelText="Hủy"
        confirmVariant="danger"
        isLoading={deleting}
        onConfirm={handleConfirmDelete}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  );
};
