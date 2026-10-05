import React, { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '@/hooks/useAuth';
import { useTitle } from '@/hooks/useTitle';
import { useSignalR } from '@/hooks/useSignalR';
import { useInfiniteScroll } from '@/hooks/useInfiniteScroll';
import { postApi } from '@/api/postApi';
import type { PostDto, PostLikeEvent, CommentAddedEvent } from '@/types';
import { CreatePostBox, PostList, NewPostAlertPill } from '@/components/posts';
import {
  ScrollToTopBtn,
  SparklesIcon,
  UsersIcon,
  ClockIcon,
  UserIcon,
  Avatar,
  Button,
} from '@/components/common';
import { getApiErrorMessage } from '@/utils/error';
import { seenPostsService } from '@/utils/seenPosts';
import { useFeedStore, type FeedTab } from '@/store';
import { ROUTES } from '@/constants/routes';
import { ENV } from '@/config';

const PAGE_SIZE = 20;

export const HomePage: React.FC = () => {
  useTitle('Trang chủ — Bảng tin gợi ý AI');
  const { isAuthenticated, user } = useAuth();

  const {
    activeTab,
    tabStates,
    setActiveTab,
    setTabPosts,
    appendTabPosts,
    prependTabPost,
    removeTabPost,
    updatePostInFeed,
    setTabScrollY,
  } = useFeedStore();

  const currentTabState = tabStates[activeTab];
  const posts = currentTabState.posts;
  const page = currentTabState.page;
  const hasMore = currentTabState.hasMore;

  const [loading, setLoading] = useState(posts.length === 0);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Bài viết mới nhận được qua SignalR
  const [pendingPosts, setPendingPosts] = useState<PostDto[]>([]);
  const [bottomPendingPosts, setBottomPendingPosts] = useState<PostDto[]>([]);

  // Tải danh sách bài viết trang đầu tiên
  const fetchPosts = useCallback(
    async (forceRefresh = false) => {
      // Nếu tab này đã có bài và không phải lệnh refresh chủ động, giữ nguyên bài đang đọc
      if (!forceRefresh && tabStates[activeTab].posts.length > 0) {
        setLoading(false);
        return;
      }

      setLoading(true);
      setError(null);
      setPendingPosts([]);
      setBottomPendingPosts([]);

      try {
        const seenIds = seenPostsService.getRecentSeenIds(60);
        let data: PostDto[];
        if (activeTab === 'following') {
          data = await postApi.getFollowingFeed(1, PAGE_SIZE, seenIds);
        } else if (activeTab === 'recent') {
          data = await postApi.getRecentPosts(1, PAGE_SIZE);
        } else {
          data = await postApi.getForYouFeed(1, PAGE_SIZE, seenIds);
        }

        setTabPosts(activeTab, data, 1, data.length >= PAGE_SIZE);
      } catch (err) {
        setError(getApiErrorMessage(err));
      } finally {
        setLoading(false);
      }
    },
    [activeTab, tabStates, setTabPosts]
  );

  // Nạp thêm đợt bài tiếp theo khi lướt xuống dưới (Infinite Scroll)
  const handleLoadMore = useCallback(async () => {
    if (loading || loadingMore) return;

    if (!hasMore && bottomPendingPosts.length > 0) {
      appendTabPosts(activeTab, bottomPendingPosts, page, false);
      setBottomPendingPosts([]);
      return;
    }

    if (!hasMore) return;

    setLoadingMore(true);
    const nextPage = page + 1;

    try {
      const currentSeenIds = [
        ...seenPostsService.getRecentSeenIds(60),
        ...posts.map((p) => p.id),
      ];

      let nextBatch: PostDto[];
      if (activeTab === 'following') {
        nextBatch = await postApi.getFollowingFeed(nextPage, PAGE_SIZE, currentSeenIds);
      } else if (activeTab === 'recent') {
        nextBatch = await postApi.getRecentPosts(nextPage, PAGE_SIZE);
      } else {
        nextBatch = await postApi.getForYouFeed(nextPage, PAGE_SIZE, currentSeenIds);
      }

      const incoming = [...bottomPendingPosts, ...nextBatch];
      setBottomPendingPosts([]);

      appendTabPosts(activeTab, incoming, nextPage, nextBatch.length >= PAGE_SIZE);
    } catch (err) {
      console.warn('Lỗi khi nạp thêm bài viết:', err);
    } finally {
      setLoadingMore(false);
    }
  }, [loading, loadingMore, hasMore, bottomPendingPosts, page, posts, activeTab, appendTabPosts]);

  const { sentinelRef } = useInfiniteScroll({
    onLoadMore: handleLoadMore,
    hasMore: hasMore || bottomPendingPosts.length > 0,
    isLoading: loading || loadingMore,
    rootMargin: '400px',
  });

  const handleManualRefresh = () => {
    window.scrollTo({ top: 0, behavior: 'smooth' });
    setTabScrollY(activeTab, 0);
    fetchPosts(true);
  };

  const handleTabSelect = (tab: FeedTab) => {
    if (tab === activeTab) {
      handleManualRefresh();
      return;
    }
    setTabScrollY(activeTab, window.scrollY);
    setActiveTab(tab);
  };

  // Khôi phục vị trí cuộn khi mount hoặc đổi tab
  useEffect(() => {
    if (tabStates[activeTab].posts.length > 0) {
      setLoading(false);
      const savedY = tabStates[activeTab].scrollY;
      if (savedY > 0) {
        requestAnimationFrame(() => {
          window.scrollTo({ top: savedY, behavior: 'instant' });
        });
      }
    } else {
      fetchPosts();
    }
  }, [activeTab]);

  // Lưu vị trí cuộn trước khi unmount trang
  useEffect(() => {
    return () => {
      setTabScrollY(activeTab, window.scrollY);
    };
  }, [activeTab, setTabScrollY]);

  // Lắng nghe sự kiện click Logo / Trang chủ từ Navbar để làm mới feed
  useEffect(() => {
    const onCustomRefresh = () => {
      handleManualRefresh();
    };
    window.addEventListener('social_refresh_feed', onCustomRefresh);
    return () => window.removeEventListener('social_refresh_feed', onCustomRefresh);
  }, [fetchPosts, activeTab]);

  // Realtime SignalR Listeners
  useSignalR<PostDto>('ReceiveNewPost', (newPost) => {
    if (!newPost || !newPost.id) return;
    if (user?.id && newPost.author?.id === user.id) return;

    // Nếu người dùng đang ở đầu trang, tự động đẩy bài viết mới vào đầu feed luôn
    const isAtTop = window.scrollY < 120;
    if (isAtTop) {
      prependTabPost(newPost);
    } else {
      // Nếu đang cuộn sâu đọc bài, đưa vào thông báo để tránh giật giao diện
      setPendingPosts((prev) => {
        if (prev.some((p) => p.id === newPost.id)) return prev;
        return [newPost, ...prev];
      });
    }
  });

  useSignalR<PostLikeEvent>('PostLiked', (event) => {
    if (!event?.postId) return;
    updatePostInFeed(event.postId, (p) => ({ ...p, likeCount: event.likeCount }));
  });

  useSignalR<CommentAddedEvent>('CommentAdded', (event) => {
    if (!event?.postId) return;
    updatePostInFeed(event.postId, (p) => ({
      ...p,
      commentCount: event.totalCommentCount,
    }));
  });

  const handlePostCreated = (newPost: PostDto) => {
    prependTabPost(newPost);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const handlePostDeleted = (postId: string) => {
    removeTabPost(postId);
    setPendingPosts((prev) => prev.filter((p) => p.id !== postId));
    setBottomPendingPosts((prev) => prev.filter((p) => p.id !== postId));
  };

  useSignalR<string>('PostDeleted', (deletedPostId) => {
    if (!deletedPostId) return;
    handlePostDeleted(deletedPostId);
  });

  const handleApplyPendingPosts = () => {
    if (pendingPosts.length === 0) return;
    pendingPosts.forEach((p) => prependTabPost(p));
    setPendingPosts([]);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  return (
    <div className="container home-page-container">
      {/* Refined Welcome Banner for Guests (Compact, not pushing feed away) */}
      {!isAuthenticated && (
        <section className="guest-welcome-card" aria-label="Giới thiệu nền tảng">
          <div className="guest-welcome-content">
            <span className="welcome-tag">
              <SparklesIcon size={14} />
              <span>Gợi ý cá nhân hóa bởi Gemini AI</span>
            </span>
            <h1 className="welcome-headline">
              Chào mừng bạn đến với <span className="accent-text">{ENV.APP_NAME}</span>
            </h1>
            <p className="welcome-subtext">
              Khám phá bài viết, chia sẻ công nghệ và kết nối cùng cộng đồng với bảng tin tự động phân loại theo sở thích thực tế của bạn.
            </p>
          </div>
          <div className="guest-welcome-actions">
            <Link to={ROUTES.AUTH.REGISTER}>
              <Button variant="primary" size="md">
                Tạo tài khoản miễn phí
              </Button>
            </Link>
            <Link to={ROUTES.AUTH.LOGIN}>
              <Button variant="ghost" size="md">
                Đăng nhập
              </Button>
            </Link>
          </div>
        </section>
      )}

      {/* Main 2-Column Social Feed Layout */}
      <div className="social-feed-layout">
        {/* Main Feed Column */}
        <main className="feed-stream-col">
          {isAuthenticated && (
            <CreatePostBox onPostCreated={handlePostCreated} />
          )}

          {/* Feed Navigation Segmented Tabs */}
          <div className="feed-tabs-container">
            <div className="feed-tabs-bar" role="tablist">
              <button
                type="button"
                role="tab"
                aria-selected={activeTab === 'for-you'}
                className={`feed-tab-item ${activeTab === 'for-you' ? 'active' : ''}`}
                onClick={() => handleTabSelect('for-you')}
                id="feed-tab-for-you"
              >
                <SparklesIcon size={16} />
                <span>Dành cho bạn</span>
              </button>

              {isAuthenticated && (
                <button
                  type="button"
                  role="tab"
                  aria-selected={activeTab === 'following'}
                  className={`feed-tab-item ${activeTab === 'following' ? 'active' : ''}`}
                  onClick={() => handleTabSelect('following')}
                  id="feed-tab-following"
                >
                  <UsersIcon size={16} />
                  <span>Đang theo dõi</span>
                </button>
              )}

              <button
                type="button"
                role="tab"
                aria-selected={activeTab === 'recent'}
                className={`feed-tab-item ${activeTab === 'recent' ? 'active' : ''}`}
                onClick={() => handleTabSelect('recent')}
                id="feed-tab-recent"
              >
                <ClockIcon size={16} />
                <span>Mới nhất</span>
              </button>
            </div>
          </div>

          {/* Floating Realtime Pill Alert */}
          <NewPostAlertPill count={pendingPosts.length} onClick={handleApplyPendingPosts} />

          <PostList
            posts={posts}
            loading={loading}
            loadingMore={loadingMore}
            hasMore={hasMore || bottomPendingPosts.length > 0}
            sentinelRef={sentinelRef}
            error={error}
            onRefresh={handleManualRefresh}
            onPostDeleted={handlePostDeleted}
          />
        </main>

        {/* Sidebar Column */}
        <aside className="feed-aside-col">
          {isAuthenticated && user ? (
            <div className="aside-widget profile-widget">
              <div className="widget-user-header">
                <Avatar
                  src={user.avatarUrl}
                  name={user.displayName}
                  size="lg"
                  className="widget-avatar"
                />
                <div className="widget-user-info">
                  <h3 className="widget-user-name">{user.displayName}</h3>
                  <p className="widget-user-email">{user.email}</p>
                </div>
              </div>

              {user.bio && <p className="widget-user-bio">{user.bio}</p>}

              <div className="widget-actions">
                <Link to={ROUTES.PROFILE} className="widget-action-link" id="sidebar-profile-link">
                  <UserIcon size={16} />
                  <span>Trang cá nhân</span>
                  <span className="arrow-hint">&rarr;</span>
                </Link>
              </div>
            </div>
          ) : (
            <div className="aside-widget join-widget">
              <h3 className="join-title">Tham gia cộng đồng</h3>
              <p className="join-desc">
                Đăng nhập để theo dõi tác giả, lưu bài viết và tương tác với các chủ đề bạn yêu thích.
              </p>
              <div className="join-buttons">
                <Link to={ROUTES.AUTH.REGISTER} className="w-full">
                  <Button variant="primary" size="sm" block id="sidebar-reg-btn">
                    Đăng ký ngay
                  </Button>
                </Link>
                <Link to={ROUTES.AUTH.LOGIN} className="w-full">
                  <Button variant="secondary" size="sm" block id="sidebar-login-btn">
                    Đăng nhập
                  </Button>
                </Link>
              </div>
            </div>
          )}
        </aside>
      </div>

      {/* Threads-style Scroll to top button */}
      <ScrollToTopBtn
        hasNewPosts={pendingPosts.length > 0}
        onClick={handleApplyPendingPosts}
      />
    </div>
  );
};
