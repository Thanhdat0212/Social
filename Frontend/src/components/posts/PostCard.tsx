import React, { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import type { PostDto } from '@/types';
import { useAuth } from '@/hooks/useAuth';
import { useNotificationStore, usePostModalStore } from '@/store';
import { postApi } from '@/api/postApi';
import { followApi } from '@/api/followApi';
import { interactionApi } from '@/api/interactionApi';
import { InteractionType } from '@/types/interaction';
import { seenPostsService } from '@/utils/seenPosts';
import { formatRelativeTime } from '@/utils/date';
import {
  Avatar,
  HeartIcon,
  CommentIcon,
  ShareIcon,
  TrashIcon,
  MoreVerticalIcon,
  CheckIcon,
  CloseIcon,
  ConfirmModal,
  DropdownMenu,
  TagIcon,
  StoreIcon,
  CopyIcon,
  ChevronDownIcon,
} from '@/components/common';
import { CommentSection } from './CommentSection';

const DEFAULT_PRODUCT_THUMB =
  "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='80' height='80' viewBox='0 0 80 80'%3E%3Crect width='80' height='80' fill='%23e2e8f0'/%3E%3Cpath d='M30 46l6-8 5 6 7-10 12 16H20z' fill='%2394a3b8'/%3E%3Ccircle cx='32' cy='28' r='4' fill='%2394a3b8'/%3E%3C/svg%3E";

const PostCodeCard: React.FC<{ lang?: string; code: string }> = ({ lang, code }) => {
  const [copied, setCopied] = useState(false);

  const handleCopy = () => {
    if (navigator.clipboard) {
      navigator.clipboard.writeText(code);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  return (
    <div className="post-code-card">
      <div className="post-code-header">
        <div className="post-code-dots">
          <span className="post-code-dot red" />
          <span className="post-code-dot yellow" />
          <span className="post-code-dot green" />
        </div>
        <span className="post-code-lang">{lang || 'CODE'}</span>
        <button
          type="button"
          className={`post-code-copy-btn ${copied ? 'copied' : ''}`}
          onClick={handleCopy}
          aria-label="Sao chép mã"
        >
          {copied ? (
            <>
              <CheckIcon size={12} />
              <span>Đã chép!</span>
            </>
          ) : (
            <>
              <CopyIcon size={12} />
              <span>Sao chép</span>
            </>
          )}
        </button>
      </div>
      <pre className="post-code-body">
        <code>{code}</code>
      </pre>
    </div>
  );
};

const renderParagraphWithHashtags = (text: string) => {
  const parts = text.split(/(#[a-zA-Z0-9_À-ỹ]+)/g);
  return parts.map((part, i) => {
    if (part.startsWith('#')) {
      const topicName = part.slice(1);
      return (
        <span
          key={i}
          className="post-hashtag"
          role="button"
          tabIndex={0}
          onClick={(e) => {
            e.stopPropagation();
            window.dispatchEvent(new CustomEvent('social_filter_topic', { detail: topicName }));
          }}
          onKeyDown={(e) => {
            if (e.key === 'Enter' || e.key === ' ') {
              e.preventDefault();
              e.stopPropagation();
              window.dispatchEvent(new CustomEvent('social_filter_topic', { detail: topicName }));
            }
          }}
        >
          {part}
        </span>
      );
    }
    return part;
  });
};

const isSectionHeader = (line: string): boolean => {
  const trimmed = line.trim();
  if (!trimmed) return false;
  return (
    /^(?:🔍|🎯|❓|💻|📌|■|👉|✦|⭐|💡|🚀|⚠️?|✅|❌|🔥|✨)/u.test(trimmed) ||
    /^(phân tích|mã nguồn|kiến thức|bí quyết|lưu ý|tóm lại|kết luận|bước \d|step \d)/i.test(trimmed)
  );
};

const renderFormattedContent = (content: string) => {
  if (!content) return null;

  // Chuẩn hóa ngắt dòng hệ điều hành (\r\n -> \n)
  const normalized = content.replace(/\r\n/g, '\n');
  const codeBlockRegex = /```(\w*)\n([\s\S]*?)```/g;
  const elements: React.ReactNode[] = [];
  let lastIndex = 0;
  let match: RegExpExecArray | null;
  let keyIndex = 0;

  const processTextBlock = (text: string) => {
    // Tách các dòng văn bản
    const lines = text.split('\n');
    lines.forEach((line) => {
      const trimmed = line.trim();
      // Bỏ qua các dòng trống rỗng để không chèn thẻ div nhân tạo gây phình khoảng trắng thừa
      if (!trimmed) return;

      if (isSectionHeader(trimmed)) {
        elements.push(
          <h5 key={`sec-${keyIndex++}`} className="post-section-header">
            {renderParagraphWithHashtags(trimmed)}
          </h5>
        );
      } else {
        elements.push(
          <p key={`p-${keyIndex++}`} className="post-text-p">
            {renderParagraphWithHashtags(line)}
          </p>
        );
      }
    });
  };

  while ((match = codeBlockRegex.exec(normalized)) !== null) {
    const textBefore = normalized.substring(lastIndex, match.index);
    if (textBefore) {
      processTextBlock(textBefore);
    }

    const lang = match[1] || '';
    const code = match[2]?.trimEnd() || '';
    elements.push(<PostCodeCard key={`code-${keyIndex++}`} lang={lang} code={code} />);

    lastIndex = match.index + match[0].length;
  }

  const remainingText = normalized.substring(lastIndex);
  if (remainingText) {
    processTextBlock(remainingText);
  }

  return elements;
};

interface PostCardProps {
  post: PostDto;
  onPostDeleted?: (postId: string) => void;
  initialShowComments?: boolean;
  priority?: boolean;
}

export const PostCard: React.FC<PostCardProps> = ({
  post,
  onPostDeleted,
  initialShowComments = false,
  priority = false,
}) => {
  const { user, isAuthenticated } = useAuth();
  const { showToast } = useNotificationStore();
  const { openPostModal } = usePostModalStore();
  const navigate = useNavigate();

  const [isDeleting, setIsDeleting] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [previewImage, setPreviewImage] = useState<string | null>(null);

  // Like state
  const [liked, setLiked] = useState(post.isLikedByCurrentUser || false);
  const [likeCount, setLikeCount] = useState(post.likeCount || 0);
  const [likeLoading, setLikeLoading] = useState(false);

  // Comment state
  const [showComments, setShowComments] = useState(initialShowComments);
  const [commentCount, setCommentCount] = useState(post.commentCount || 0);

  // Expandable Read More state for long posts
  const [isExpanded, setIsExpanded] = useState(false);
  const isLongPost = Boolean(
    post.content &&
      (post.content.length > 380 ||
        post.content.split('\n').filter((l) => l.trim().length > 0).length > 7 ||
        post.content.includes('```'))
  );

  // Đồng bộ số lượng like, trạng thái like và comment khi có cập nhật từ cha hoặc realtime
  useEffect(() => {
    setLiked(post.isLikedByCurrentUser || false);
  }, [post.isLikedByCurrentUser]);

  useEffect(() => {
    setLikeCount(post.likeCount || 0);
  }, [post.likeCount]);

  useEffect(() => {
    setCommentCount(post.commentCount || 0);
  }, [post.commentCount]);

  // Follow state
  const [isFollowing, setIsFollowing] = useState(false);
  const [followLoading, setFollowLoading] = useState(false);

  // View tracking ref
  const cardRef = useRef<HTMLElement>(null);
  const hasTrackedView = useRef(false);

  const isAuthor = user?.id === post.author.id;

  // Tự động ghi nhận hành vi xem bài (View tracking) bằng IntersectionObserver
  useEffect(() => {
    if (hasTrackedView.current || !cardRef.current) return;

    let timer: ReturnType<typeof setTimeout> | null = null;

    const observer = new IntersectionObserver(
      (entries) => {
        const [entry] = entries;
        if (entry.isIntersecting) {
          timer = setTimeout(() => {
            if (!hasTrackedView.current) {
              hasTrackedView.current = true;
              // Đánh dấu đã xem trên frontend
              seenPostsService.markAsSeen(post.id);

              if (isAuthenticated) {
                interactionApi
                  .track({
                    postId: post.id,
                    interactionType: InteractionType.View,
                    value: 1.0,
                  })
                  .catch(() => {});
              }
            }
          }, 250);
        } else {
          if (timer) {
            clearTimeout(timer);
            timer = null;
          }
        }
      },
      { threshold: 0.15 }
    );

    observer.observe(cardRef.current);

    return () => {
      observer.disconnect();
      if (timer) clearTimeout(timer);
    };
  }, [isAuthenticated, post.id]);

  // Handle Like/Unlike
  const handleToggleLike = async () => {
    if (!isAuthenticated) {
      showToast('Vui lòng đăng nhập để thích bài viết.', 'warning');
      return;
    }

    if (likeLoading) return;

    const previousLiked = liked;
    const previousCount = likeCount;

    setLiked(!previousLiked);
    setLikeCount(previousLiked ? Math.max(0, previousCount - 1) : previousCount + 1);
    setLikeLoading(true);

    try {
      const res = await postApi.toggleLike(post.id);
      setLiked(res.isLiked);
      setLikeCount(res.likeCount);
    } catch {
      setLiked(previousLiked);
      setLikeCount(previousCount);
    } finally {
      setLikeLoading(false);
    }
  };

  // Handle Follow/Unfollow Author
  const handleToggleFollow = async () => {
    if (!isAuthenticated) {
      showToast('Vui lòng đăng nhập để theo dõi tác giả.', 'warning');
      return;
    }

    if (followLoading || isAuthor) return;

    setFollowLoading(true);
    try {
      const res = await followApi.toggleFollow(post.author.id);
      setIsFollowing(res.isFollowing);
      showToast(
        res.isFollowing
          ? `Đã theo dõi ${post.author.displayName}`
          : `Đã hủy theo dõi ${post.author.displayName}`,
        'info'
      );
    } catch {
      showToast('Không thể cập nhật theo dõi.', 'error');
    } finally {
      setFollowLoading(false);
    }
  };

  // Handle Delete Post
  const handleConfirmDelete = async () => {
    setIsDeleting(true);
    try {
      await postApi.deletePost(post.id);
      setShowDeleteConfirm(false);
      showToast('Đã xóa bài viết thành công.', 'info');
      onPostDeleted?.(post.id);
    } catch {
      showToast('Không thể xóa bài viết. Vui lòng thử lại.', 'error');
    } finally {
      setIsDeleting(false);
    }
  };

  const handleCopyLink = () => {
    if (navigator.clipboard) {
      const postUrl = `${window.location.origin}/posts/${post.id}`;
      navigator.clipboard.writeText(postUrl);
      showToast('Đã sao chép liên kết bài viết vào bộ nhớ tạm!', 'info');
    }
  };

  return (
    <>
      <article ref={cardRef} className="post-entry" id={`post-${post.id}`}>
        {/* Post Header */}
        <header className="post-entry-header">
          <div className="post-entry-author">
            <Avatar
              src={post.author.avatarUrl}
              name={post.author.displayName}
              size="md"
            />
            <div className="post-author-meta">
              <div className="author-headline">
                <span className="author-display-name">{post.author.displayName}</span>
              </div>
              <button
                type="button"
                className="post-entry-time-link"
                onClick={() => openPostModal(post.id)}
                title="Xem chi tiết bài viết"
              >
                <time className="post-entry-time" dateTime={post.createdAtUtc}>
                  {formatRelativeTime(post.createdAtUtc)}
                </time>
              </button>
            </div>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            {isAuthenticated && !isAuthor && (
              <button
                type="button"
                className={`btn-follow-chip ${isFollowing ? 'following' : ''}`}
                onClick={handleToggleFollow}
                disabled={followLoading}
                aria-label={isFollowing ? `Hủy theo dõi ${post.author.displayName}` : `Theo dõi ${post.author.displayName}`}
              >
                {isFollowing ? (
                  <>
                    <CheckIcon size={12} />
                    <span>Đang theo dõi</span>
                  </>
                ) : (
                  <span>+ Theo dõi</span>
                )}
              </button>
            )}

            {/* Context Menu 3-dots */}
            <DropdownMenu
              align="right"
              trigger={(isOpen) => (
                <button
                  type="button"
                  className={`btn-icon-ghost ${isOpen ? 'active' : ''}`}
                  aria-label="Tùy chọn bài viết"
                  title="Tùy chọn"
                >
                  <MoreVerticalIcon size={18} />
                </button>
              )}
            >
              <button
                type="button"
                className="dropdown-item-link"
                onClick={() => openPostModal(post.id)}
              >
                <ShareIcon size={16} />
                <span>Xem chi tiết</span>
              </button>
              <button type="button" className="dropdown-item-link" onClick={handleCopyLink}>
                <ShareIcon size={16} />
                <span>Sao chép liên kết</span>
              </button>
              {isAuthor && (
                <button
                  type="button"
                  className="dropdown-item-btn is-danger"
                  onClick={() => setShowDeleteConfirm(true)}
                >
                  <TrashIcon size={16} />
                  <span>Xóa bài viết</span>
                </button>
              )}
            </DropdownMenu>
          </div>
        </header>

        {/* Post Content */}
        {post.content && (
          <div className="post-content-clamp-wrapper">
            <div className={isLongPost && !isExpanded ? 'post-content-clamp' : ''}>
              <div className="post-entry-body">
                {renderFormattedContent(post.content)}
              </div>
            </div>
            {isLongPost && (
              <button
                type="button"
                className="post-expand-toggle-btn"
                onClick={() => setIsExpanded(!isExpanded)}
                aria-expanded={isExpanded}
              >
                <span>{isExpanded ? 'Thu gọn bài viết' : 'Xem thêm bài viết...'}</span>
                <ChevronDownIcon size={14} className={`chevron-icon ${isExpanded ? 'is-rotated' : ''}`} />
              </button>
            )}
          </div>
        )}

        {/* Post Media Gallery */}
        {post.mediaUrls && post.mediaUrls.length > 0 && (
          <div className={`post-gallery gallery-count-${Math.min(post.mediaUrls.length, 5)}`}>
            {post.mediaUrls.map((url, index) => {
              const isLcpCandidate = priority && index === 0;
              return (
                <div
                  key={index}
                  className="gallery-cell"
                  onClick={() => setPreviewImage(url)}
                  title="Bấm để xem ảnh kích thước đầy đủ"
                >
                  <img
                    src={url}
                    alt={`post-media-${index}`}
                    loading={isLcpCandidate ? 'eager' : 'lazy'}
                    fetchPriority={isLcpCandidate ? 'high' : 'auto'}
                    decoding={isLcpCandidate ? 'sync' : 'async'}
                  />
                </div>
              );
            })}
          </div>
        )}

        {/* Tagged Products Widget (Social Commerce) */}
        {post.taggedProducts && post.taggedProducts.length > 0 && (
          <div className="post-tagged-products-container">
            <div className="post-tagged-products-header">
              <TagIcon size={15} className="tagged-badge-icon" />
              <span className="tagged-badge-title">Sản phẩm số đính kèm ({post.taggedProducts.length})</span>
            </div>
            <div className="post-tagged-products-list">
              {post.taggedProducts.map((product) => (
                <div
                  key={product.id}
                  className="post-tagged-product-card"
                  onClick={() => navigate(`/marketplace/products/${product.slug}`)}
                  title={`Xem chi tiết sản phẩm "${product.name}"`}
                >
                  <img
                    src={product.coverImageUrl || DEFAULT_PRODUCT_THUMB}
                    alt={product.name}
                    className="tagged-product-thumb"
                    onError={(e) => {
                      const target = e.currentTarget;
                      if (!target.dataset.hasFallback) {
                        target.dataset.hasFallback = 'true';
                        target.src = DEFAULT_PRODUCT_THUMB;
                      }
                    }}
                  />
                  <div className="tagged-product-info">
                    <span className="tagged-product-shop">
                      <StoreIcon size={13} style={{ display: 'inline-block', verticalAlign: '-1px', marginRight: '4px' }} />
                      {product.shopName}
                    </span>
                    <h4 className="tagged-product-title">{product.name}</h4>
                    <div className="tagged-product-pricing">
                      <span className="tagged-product-price">
                        {new Intl.NumberFormat('vi-VN').format(product.minPrice)} ₫
                      </span>
                    </div>
                  </div>
                  <div className="tagged-product-actions">
                    <button
                      type="button"
                      className="btn-view-product"
                      onClick={(e) => {
                        e.stopPropagation();
                        navigate(`/marketplace/products/${product.slug}`);
                      }}
                    >
                      Xem chi tiết →
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Interaction Toolbar */}
        <footer className="post-entry-toolbar">
          <button
            type="button"
            className={`post-toolbar-btn ${liked ? 'is-liked' : ''}`}
            onClick={handleToggleLike}
            disabled={likeLoading}
            aria-label={liked ? 'Bỏ thích' : 'Thích bài viết'}
          >
            <HeartIcon size={19} filled={liked} className="heart-icon-anim" />
            <span className="toolbar-counter">{likeCount > 0 ? likeCount : 'Thích'}</span>
          </button>

          <button
            type="button"
            className={`post-toolbar-btn ${showComments ? 'is-active' : ''}`}
            onClick={() => setShowComments((prev) => !prev)}
            aria-label="Xem bình luận"
          >
            <CommentIcon size={19} />
            <span className="toolbar-counter">
              {commentCount > 0 ? commentCount : 'Bình luận'}
            </span>
          </button>

          <button
            type="button"
            className="post-toolbar-btn"
            onClick={handleCopyLink}
            aria-label="Chia sẻ bài viết"
          >
            <ShareIcon size={19} />
            <span className="toolbar-counter">Chia sẻ</span>
          </button>
        </footer>

        {/* Expandable Comment Section */}
        {showComments && (
          <CommentSection
            postId={post.id}
            onCommentCountChange={(count) => {
              setCommentCount(count);
            }}
          />
        )}
      </article>

      {/* Full-Screen Image Lightbox Modal */}
      {previewImage && (
        <div
          className="image-lightbox-overlay"
          onClick={() => setPreviewImage(null)}
          role="dialog"
          aria-modal="true"
        >
          <button
            type="button"
            className="lightbox-close-btn"
            onClick={() => setPreviewImage(null)}
            aria-label="Đóng ảnh"
          >
            <CloseIcon size={20} />
          </button>
          <div className="image-lightbox-container" onClick={(e) => e.stopPropagation()}>
            <img src={previewImage} alt="Full resolution view" />
          </div>
        </div>
      )}

      {/* In-app Delete Confirmation Modal */}
      <ConfirmModal
        isOpen={showDeleteConfirm}
        title="Xóa bài viết"
        message="Bạn có chắc chắn muốn xóa bài viết này không? Hành động này không thể hoàn tác."
        confirmText="Xóa vĩnh viễn"
        cancelText="Giữ lại"
        confirmVariant="danger"
        isLoading={isDeleting}
        onConfirm={handleConfirmDelete}
        onCancel={() => setShowDeleteConfirm(false)}
      />
    </>
  );
};
