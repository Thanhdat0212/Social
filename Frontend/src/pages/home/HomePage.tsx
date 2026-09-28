import React, { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '@/hooks/useAuth';
import { useTitle } from '@/hooks/useTitle';
import { postApi } from '@/api/postApi';
import type { PostDto } from '@/types/post';
import { CreatePostBox, PostList } from '@/components/posts';
import { getApiErrorMessage } from '@/utils/error';
import { ROUTES } from '@/constants/routes';
import { ENV } from '@/config';

type FeedTab = 'for-you' | 'following' | 'recent';

export const HomePage: React.FC = () => {
  useTitle('Trang chủ — Bảng tin gợi ý AI');
  const { isAuthenticated, user } = useAuth();

  const [activeTab, setActiveTab] = useState<FeedTab>('for-you');
  const [posts, setPosts] = useState<PostDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchPosts = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      let data: PostDto[];
      if (activeTab === 'following') {
        data = await postApi.getFollowingFeed(1, 20);
      } else if (activeTab === 'recent') {
        data = await postApi.getRecentPosts(1, 20);
      } else {
        // 'for-you' (Default smart AI recommendation)
        data = await postApi.getForYouFeed(1, 20);
      }
      setPosts(data);
    } catch (err) {
      setError(getApiErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }, [activeTab]);

  useEffect(() => {
    fetchPosts();
  }, [fetchPosts]);

  const handlePostCreated = (newPost: PostDto) => {
    setPosts((prev) => [newPost, ...prev]);
  };

  const handlePostDeleted = (postId: string) => {
    setPosts((prev) => prev.filter((p) => p.id !== postId));
  };

  return (
    <div className="container home-page">
      {/* Hero Banner for Guests */}
      {!isAuthenticated && (
        <div className="hero-section">
          <div className="badge hero-badge">🚀 Mạng xã hội thông minh — Tích hợp Google Gemini AI</div>
          <h1 className="hero-title">
            Khám phá kiến thức cùng <span className="gradient-text">{ENV.APP_NAME}</span>
          </h1>
          <p className="hero-subtitle">
            Hệ thống tự động phân loại chủ đề bài viết bằng AI và cá nhân hóa bảng tin theo sở thích thực tế của bạn.
          </p>

          <div className="hero-cta">
            <div className="cta-group">
              <Link to={ROUTES.AUTH.REGISTER} className="btn btn-primary btn-lg" id="get-started-btn">
                Bắt đầu ngay — Đăng ký
              </Link>
              <Link to={ROUTES.AUTH.LOGIN} className="btn btn-secondary btn-lg" id="login-hero-btn">
                Đăng nhập
              </Link>
            </div>
          </div>
        </div>
      )}

      {/* Main 2-Column Layout */}
      <div className="home-feed-layout">
        {/* Main Feed Column */}
        <main className="feed-main-col">
          {isAuthenticated && (
            <CreatePostBox onPostCreated={handlePostCreated} />
          )}

          {/* Feed Navigation Tabs */}
          <div className="feed-tabs-nav">
            <button
              type="button"
              className={`feed-tab-btn ${activeTab === 'for-you' ? 'active' : ''}`}
              onClick={() => setActiveTab('for-you')}
              id="feed-tab-for-you"
            >
              <span className="tab-icon">✨</span>
              <span>Dành cho bạn</span>
            </button>

            {isAuthenticated && (
              <button
                type="button"
                className={`feed-tab-btn ${activeTab === 'following' ? 'active' : ''}`}
                onClick={() => setActiveTab('following')}
                id="feed-tab-following"
              >
                <span className="tab-icon">👥</span>
                <span>Đang theo dõi</span>
              </button>
            )}

            <button
              type="button"
              className={`feed-tab-btn ${activeTab === 'recent' ? 'active' : ''}`}
              onClick={() => setActiveTab('recent')}
              id="feed-tab-recent"
            >
              <span className="tab-icon">🕒</span>
              <span>Mới nhất</span>
            </button>
          </div>

          <PostList
            posts={posts}
            loading={loading}
            error={error}
            onRefresh={fetchPosts}
            onPostDeleted={handlePostDeleted}
          />
        </main>

        {/* Sidebar Column */}
        <aside className="feed-sidebar-col">
          {isAuthenticated && user ? (
            <div className="card sidebar-info-card">
              <h3 className="sidebar-title">
                <span>👋 Chào mừng, {user.displayName}</span>
              </h3>
              <p className="sidebar-content">
                Chúc bạn có những trải nghiệm kết nối và chia sẻ tuyệt vời hôm nay!
              </p>
              <div className="sidebar-links">
                <Link to={ROUTES.PROFILE} className="sidebar-link-btn" id="sidebar-profile-link">
                  <span>👤 Trang cá nhân</span>
                  <span>→</span>
                </Link>
              </div>
            </div>
          ) : (
            <div className="card sidebar-info-card">
              <h3 className="sidebar-title">
                <span>🌟 Chào mừng bạn</span>
              </h3>
              <p className="sidebar-content">
                Tham gia cộng đồng Social để chia sẻ câu chuyện và kết nối cùng mọi người.
              </p>
              <div className="sidebar-links">
                <Link to={ROUTES.AUTH.REGISTER} className="sidebar-link-btn" id="sidebar-reg-link">
                  <span>✨ Đăng ký tài khoản</span>
                  <span>→</span>
                </Link>
                <Link to={ROUTES.AUTH.LOGIN} className="sidebar-link-btn" id="sidebar-login-link">
                  <span>🔑 Đăng nhập</span>
                  <span>→</span>
                </Link>
              </div>
            </div>
          )}
        </aside>
      </div>
    </div>
  );
};
