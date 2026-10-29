import React, { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '@/hooks/useAuth';
import { useTitle } from '@/hooks/useTitle';
import { useSignalR } from '@/hooks/useSignalR';
import { useInfiniteScroll } from '@/hooks/useInfiniteScroll';
import { postApi } from '@/api/postApi';
import { marketplaceApi } from '@/api/marketplaceApi';
import { interestApi } from '@/api/interestApi';
import type { PostDto, PostLikeEvent, CommentAddedEvent } from '@/types';
import type { Category, ProductListItem } from '@/types/marketplace';
import type { InterestDto } from '@/types/interest';
import { CreatePostBox, PostList, NewPostAlertPill } from '@/components/posts';
import {
  ScrollToTopBtn,
  SparklesIcon,
  UsersIcon,
  ClockIcon,
  Button,
  PageContainer,
  MarketplaceIcon,
  StoreIcon,
  ShieldIcon,
  CheckCircleIcon,
  ZapIcon,
  CloseIcon,
  TagIcon,
  ChevronRightIcon,
  SidebarProductsSkeleton,
} from '@/components/common';
import { getApiErrorMessage } from '@/utils/error';
import { seenPostsService } from '@/utils/seenPosts';
import { useFeedStore, type FeedTab } from '@/store';
import { ROUTES } from '@/constants/routes';
import { ENV } from '@/config';

const INITIAL_PAGE_SIZE = 2; // Micro-batching: Chỉ tải 2 bài viết đầu tiên để tốc độ phản hồi siêu tốc (< 200ms)
const PAGE_SIZE = 5; // Khi người dùng cuộn (Infinite Scroll) nạp tiếp từng đợt 5 bài
const SIDEBAR_PRODUCT_FALLBACK_THUMB =
  "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='42' height='42' viewBox='0 0 42 42'%3E%3Crect width='42' height='42' fill='%23e2e8f0'/%3E%3Cpath d='M14 26l6-7 4 5 6-8 8 10H8z' fill='%2394a3b8'/%3E%3C/svg%3E";

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
  const [refreshing, setRefreshing] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Bài viết mới nhận được qua SignalR
  const [pendingPosts, setPendingPosts] = useState<PostDto[]>([]);
  const [bottomPendingPosts, setBottomPendingPosts] = useState<PostDto[]>([]);

  // Dữ liệu sidebar thực tế (tải độc lập theo BigPipe pattern)
  const [featuredProducts, setFeaturedProducts] = useState<ProductListItem[]>([]);
  const [isProductsLoading, setIsProductsLoading] = useState(true);
  const [categories, setCategories] = useState<Category[]>([]);
  const [topInterests, setTopInterests] = useState<InterestDto[]>([]);
  const [selectedTopic, setSelectedTopic] = useState<string | null>(null);

  useEffect(() => {
    const onFilterTopic = (e: Event) => {
      const topic = (e as CustomEvent<string>).detail?.trim();
      if (!topic) return;
      setSelectedTopic((prev) => (prev?.toLowerCase() === topic.toLowerCase() ? null : topic));
      window.scrollTo({ top: 0, behavior: 'smooth' });
    };
    window.addEventListener('social_filter_topic', onFilterTopic);
    return () => window.removeEventListener('social_filter_topic', onFilterTopic);
  }, []);

  useEffect(() => {
    marketplaceApi
      .getProducts({ page: 1, pageSize: 5 })
      .then((res) => setFeaturedProducts(res.items || []))
      .catch(() => {})
      .finally(() => setIsProductsLoading(false));

    marketplaceApi
      .getCategoriesTree()
      .then((res) => setCategories((res || []).slice(0, 6)))
      .catch(() => {});

    interestApi
      .getAllInterests()
      .then((res) => setTopInterests(res.slice(0, 8)))
      .catch(() => {});
  }, []);

  // Tải danh sách bài viết trang đầu tiên
  const fetchPosts = useCallback(
    async (forceRefresh = false) => {
      const hasExistingPosts = tabStates[activeTab].posts.length > 0;

      // Nếu tab này đã có bài và không phải lệnh refresh chủ động, giữ nguyên bài đang đọc
      if (!forceRefresh && hasExistingPosts) {
        setLoading(false);
        return;
      }

      if (forceRefresh) {
        setRefreshing(true);
        if (!hasExistingPosts) {
          setLoading(true);
        }
      } else {
        setLoading(true);
      }

      setError(null);
      setPendingPosts([]);
      setBottomPendingPosts([]);

      const minSpinDelay = forceRefresh
        ? new Promise((resolve) => setTimeout(resolve, 550))
        : Promise.resolve();

      try {
        const seenIds = seenPostsService.getRecentSeenIds(60);
        let fetchPromise: Promise<PostDto[]>;
        if (activeTab === 'following') {
          fetchPromise = postApi.getFollowingFeed(1, INITIAL_PAGE_SIZE, seenIds);
        } else if (activeTab === 'recent') {
          fetchPromise = postApi.getRecentPosts(1, INITIAL_PAGE_SIZE);
        } else {
          fetchPromise = postApi.getForYouFeed(1, INITIAL_PAGE_SIZE, seenIds);
        }

        const [data] = await Promise.all([fetchPromise, minSpinDelay]);

        setTabPosts(activeTab, data, 1, data.length >= INITIAL_PAGE_SIZE);
      } catch (err) {
        await minSpinDelay;
        setError(getApiErrorMessage(err));
      } finally {
        setLoading(false);
        setRefreshing(false);
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
    <PageContainer variant="feed" className="home-page-container">
      {/* Facebook-style Floating Circular Reload Spinner */}
      <div
        className={`fb-refresh-indicator ${refreshing ? 'is-active' : ''}`}
        role="status"
        aria-live="polite"
        aria-hidden={!refreshing}
      >
        <div className="fb-refresh-disc" title="Đang làm mới bảng tin...">
          <svg className="fb-refresh-svg" viewBox="0 0 50 50">
            <circle
              className="fb-refresh-track"
              cx="25"
              cy="25"
              r="20"
              fill="none"
              strokeWidth="4.5"
            />
            <circle
              className="fb-refresh-arc"
              cx="25"
              cy="25"
              r="20"
              fill="none"
              strokeWidth="4.5"
            />
          </svg>
        </div>
      </div>

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

      {/* Main 3-Column Social Feed Layout */}
      <div className="social-feed-layout">
        {/* Left Discovery & Category Filter Column (No duplicate Avatar or Navbar links) */}
        <aside className="feed-left-sidebar" aria-label="Khám phá chủ đề và danh mục">
          {/* Widget 1: Chủ đề khám phá trên Bảng tin */}
          {topInterests.length > 0 && (
            <div className="aside-widget" style={{ marginBottom: 0 }}>
              <div className="aside-widget-header">
                <div className="aside-widget-title-wrap">
                  <span className="aside-widget-icon aside-widget-icon--purple">
                    <SparklesIcon size={16} />
                  </span>
                  <h3 className="aside-widget-title">Chủ đề khám phá</h3>
                </div>
                {isAuthenticated && (
                  <Link
                    to={ROUTES.ONBOARDING}
                    className="aside-widget-link"
                    title="Cập nhật sở thích gợi ý AI của bạn"
                  >
                    Sở thích AI
                  </Link>
                )}
              </div>
              <div className="sidebar-topics-cloud">
                {topInterests.map((interest) => {
                  const isTopicActive =
                    selectedTopic?.toLowerCase() === interest.name.toLowerCase();
                  return (
                    <button
                      key={interest.id}
                      type="button"
                      className={`sidebar-topic-pill ${isTopicActive ? 'active' : ''}`}
                      aria-pressed={isTopicActive}
                      onClick={() => {
                        setSelectedTopic((prev) =>
                          prev?.toLowerCase() === interest.name.toLowerCase()
                            ? null
                            : interest.name
                        );
                        window.scrollTo({ top: 0, behavior: 'smooth' });
                      }}
                      title={`Lọc bài viết về #${interest.name}`}
                    >
                      <span className="topic-pill-hash">#</span>
                      <span className="topic-pill-name">{interest.name}</span>
                    </button>
                  );
                })}
              </div>
            </div>
          )}

          {/* Widget 2: Danh mục Chợ sản phẩm số (Real API) */}
          {categories.length > 0 && (
            <div className="aside-widget" style={{ marginBottom: 0, padding: '1rem 0.85rem' }}>
              <div className="aside-widget-header" style={{ padding: '0 0.3rem', marginBottom: '0.6rem' }}>
                <div className="aside-widget-title-wrap">
                  <span className="aside-widget-icon aside-widget-icon--blue">
                    <TagIcon size={15} />
                  </span>
                  <h3 className="aside-widget-title">Danh mục Chợ số</h3>
                </div>
              </div>
              <nav className="left-category-list" aria-label="Danh mục sản phẩm số">
                {categories.map((cat) => (
                  <Link
                    key={cat.id}
                    to={`${ROUTES.MARKETPLACE.INDEX}?categoryId=${encodeURIComponent(cat.id)}`}
                    className="left-nav-item"
                    style={{ justifyContent: 'space-between', padding: '0.55rem 0.7rem' }}
                  >
                    <span style={{ whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                      {cat.name}
                    </span>
                    <ChevronRightIcon size={14} style={{ color: 'var(--text-dim)', flexShrink: 0 }} />
                  </Link>
                ))}
              </nav>
            </div>
          )}

          {/* Tip Card */}
          <div className="left-tip-card">
            <strong style={{ color: 'var(--accent-primary)', display: 'block', marginBottom: '0.25rem' }}>
              ⚡ Mẹo gắn sản phẩm
            </strong>
            Bạn có thể đính kèm tối đa 3 sản phẩm số vào mỗi bài viết để người đọc mua ngay trên bảng tin.
          </div>
        </aside>

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

          {/* Active Topic / Hashtag Filter Banner */}
          {selectedTopic && (
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                gap: '0.75rem',
                padding: '0.65rem 1rem',
                borderRadius: 'var(--radius-md)',
                background: 'var(--accent-subtle)',
                border: '1px solid var(--accent-border)',
                color: 'var(--text-main)',
                fontSize: '0.86rem',
              }}
            >
              <span>
                Đang lọc bài viết theo chủ đề:{' '}
                <strong style={{ color: 'var(--accent-primary)' }}>#{selectedTopic}</strong>
              </span>
              <button
                type="button"
                onClick={() => setSelectedTopic(null)}
                style={{
                  display: 'inline-flex',
                  alignItems: 'center',
                  gap: '4px',
                  padding: '0.25rem 0.6rem',
                  borderRadius: 'var(--radius-full)',
                  background: 'var(--bg-surface)',
                  border: '1px solid var(--border-color)',
                  color: 'var(--text-secondary)',
                  fontSize: '0.78rem',
                  fontWeight: 600,
                  cursor: 'pointer',
                }}
              >
                <CloseIcon size={12} />
                <span>Bỏ lọc</span>
              </button>
            </div>
          )}

          <PostList
            posts={
              selectedTopic
                ? posts.filter((p) =>
                    p.content?.toLowerCase().includes(selectedTopic.toLowerCase())
                  )
                : posts
            }
            loading={loading}
            loadingMore={loadingMore}
            hasMore={!selectedTopic && (hasMore || bottomPendingPosts.length > 0)}
            sentinelRef={sentinelRef}
            error={error}
            onRefresh={handleManualRefresh}
            onPostDeleted={handlePostDeleted}
          />
        </main>

        {/* Sidebar Column */}
        <aside className="feed-aside-col">
          {!isAuthenticated && (
            <div className="aside-widget join-widget" style={{ marginBottom: '1.25rem' }}>
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

          {/* Widget: Sản phẩm số nổi bật (Real API + BigPipe Skeleton) */}
          {isProductsLoading ? (
            <SidebarProductsSkeleton />
          ) : featuredProducts.length > 0 ? (
            <div className="aside-widget" style={{ marginBottom: 0 }}>
              <div className="aside-widget-header">
                <div className="aside-widget-title-wrap">
                  <span className="aside-widget-icon aside-widget-icon--blue">
                    <MarketplaceIcon size={16} />
                  </span>
                  <h3 className="aside-widget-title">Chợ số nổi bật</h3>
                </div>
                <Link to={ROUTES.MARKETPLACE.INDEX} className="aside-widget-link">
                  Xem tất cả
                </Link>
              </div>

              <div className="sidebar-product-list">
                {featuredProducts.map((p) => (
                  <Link
                    key={p.id}
                    to={`/marketplace/products/${p.slug}`}
                    className="sidebar-product-item"
                  >
                    <img
                      src={p.coverImageUrl || SIDEBAR_PRODUCT_FALLBACK_THUMB}
                      alt={p.name}
                      className="sidebar-product-thumb"
                      loading="lazy"
                      onError={(e) => {
                        const target = e.currentTarget;
                        if (!target.dataset.hasFallback) {
                          target.dataset.hasFallback = 'true';
                          target.src = SIDEBAR_PRODUCT_FALLBACK_THUMB;
                        }
                      }}
                    />
                    <div style={{ flex: 1, minWidth: 0 }}>
                      <div style={{ fontSize: '0.82rem', fontWeight: 600, color: 'var(--text-main)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                        {p.name}
                      </div>
                      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '0.4rem', marginTop: '0.15rem' }}>
                        <span style={{ fontSize: '0.76rem', fontWeight: 700, color: 'var(--accent-primary)' }}>
                          {p.minPrice.toLocaleString('vi-VN')} đ
                        </span>
                        {p.shopName && (
                          <span style={{ fontSize: '0.7rem', color: 'var(--text-dim)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis', maxWidth: '110px' }}>
                            <StoreIcon size={11} style={{ display: 'inline', verticalAlign: '-1px', marginRight: '3px' }} />
                            {p.shopName}
                          </span>
                        )}
                      </div>
                    </div>
                  </Link>
                ))}
              </div>
            </div>
          ) : null}

          {/* Widget: Tiêu chuẩn giao dịch Chợ số */}
          <div className="aside-widget" style={{ marginBottom: 0 }}>
            <div className="aside-widget-header">
              <div className="aside-widget-title-wrap">
                <span className="aside-widget-icon aside-widget-icon--green">
                  <ZapIcon size={16} />
                </span>
                <h3 className="aside-widget-title">Tiêu chuẩn Chợ số</h3>
              </div>
              <span className="aside-widget-badge-green">24/7 Tự động</span>
            </div>

            <div className="live-activity-list">
              <div className="live-activity-item">
                <div className="live-activity-icon">
                  <CheckCircleIcon size={14} />
                </div>
                <div className="live-activity-content">
                  <div className="live-activity-text">Xuất kho mã số tức thì</div>
                  <div className="live-activity-meta">Nhận key & tài khoản ngay sau khi thanh toán</div>
                </div>
              </div>

              <div className="live-activity-item">
                <div className="live-activity-icon">
                  <CheckCircleIcon size={14} />
                </div>
                <div className="live-activity-content">
                  <div className="live-activity-text">Thanh toán VietQR chuẩn Napas</div>
                  <div className="live-activity-meta">Tự động khớp lệnh chính xác theo từng đơn</div>
                </div>
              </div>
            </div>
          </div>

          {/* Buyer Protection Guarantee Card */}
          <div className="buyer-guarantee-card" style={{ marginTop: 0 }}>
            <div className="guarantee-icon">
              <ShieldIcon size={18} />
            </div>
            <div>
              <div className="guarantee-title">Bảo vệ Người mua 100%</div>
              <p className="guarantee-desc">
                Cơ chế ký quỹ giữ tiền an toàn 3 ngày. Bạn chỉ thanh toán khi nhận đúng mã số và hài lòng với chất lượng.
              </p>
            </div>
          </div>
        </aside>
      </div>

      {/* Threads-style Scroll to top button */}
      <ScrollToTopBtn
        hasNewPosts={pendingPosts.length > 0}
        onClick={handleApplyPendingPosts}
      />
    </PageContainer>
  );
};
