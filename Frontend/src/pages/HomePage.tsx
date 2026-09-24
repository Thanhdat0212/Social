import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';
import { apiClient, getApiErrorMessage } from '@/api/client';

export const HomePage: React.FC = () => {
  const { isAuthenticated, user } = useAuthStore();
  const [proxyTestResult, setProxyTestResult] = useState<string | null>(null);
  const [testingProxy, setTestingProxy] = useState(false);

  const handleTestProxy = async () => {
    setTestingProxy(true);
    setProxyTestResult(null);
    try {
      // Gọi thử endpoint auth resend-confirmation để kiểm tra kết nối proxy tới backend .NET
      const res = await apiClient.post('/auth/resend-confirmation', {
        email: 'test-connection@social.local',
      });
      setProxyTestResult(`✅ Thành công (HTTP 200): ${res.data.message || 'Kết nối API Proxy hoạt động hoàn hảo!'}`);
    } catch (err) {
      setProxyTestResult(`❌ Lỗi: ${getApiErrorMessage(err)}`);
    } finally {
      setTestingProxy(false);
    }
  };

  return (
    <div className="container home-page">
      <div className="hero-section">
        <div className="badge hero-badge">🚀 Phase 1: Authentication & Hồ sơ cá nhân</div>
        <h1 className="hero-title">
          Mạng xã hội <span className="gradient-text">Social</span>
        </h1>
        <p className="hero-subtitle">
          Nền tảng mạng xã hội hiệu năng cao xây dựng trên ASP.NET Core 10 &amp; React Vite.
          Bảo mật tối đa với cơ chế JWT Access Token trong RAM và Refresh Token trong cookie httpOnly.
        </p>

        <div className="hero-cta">
          {isAuthenticated ? (
            <Link to="/profile" className="btn btn-primary btn-lg" id="view-profile-btn">
              Xem hồ sơ cá nhân →
            </Link>
          ) : (
            <div className="cta-group">
              <Link to="/register" className="btn btn-primary btn-lg" id="get-started-btn">
                Bắt đầu ngay — Đăng ký
              </Link>
              <Link to="/login" className="btn btn-secondary btn-lg" id="login-hero-btn">
                Đăng nhập
              </Link>
            </div>
          )}
        </div>
      </div>

      <div className="features-grid">
        <div className="card feature-card">
          <div className="feature-icon">🛡️</div>
          <h3>Bảo mật Auth cấp cao</h3>
          <p>
            Access token 15 phút chỉ lưu trong bộ nhớ RAM (Zustand), ngăn chặn hoàn toàn tấn công XSS.
            Refresh token 7 ngày xoay vòng qua cookie <code>httpOnly</code>.
          </p>
        </div>

        <div className="card feature-card">
          <div className="feature-icon">🌐</div>
          <h3>Vite &amp; Vercel Proxy</h3>
          <p>
            Môi trường dev chuyển tiếp qua <code>server.proxy</code> của Vite, Production dùng Vercel rewrite
            giúp Frontend và Backend chạy cùng origin, không vướng rào cản CORS.
          </p>
        </div>

        <div className="card feature-card">
          <div className="feature-icon">👤</div>
          <h3>Quản lý Hồ sơ &amp; Avatar</h3>
          <p>
            Tùy biến Display Name, tiểu sử cá nhân và tải lên ảnh đại diện trực tiếp lên Cloudinary với
            preview tức thì.
          </p>
        </div>
      </div>

      {/* Box kiểm thử kết nối Proxy */}
      <div className="card proxy-test-card">
        <div className="card-header">
          <h3>⚡ Kiểm tra kết nối Vite Proxy ➔ Backend (.NET)</h3>
          <p>Bấm nút bên dưới để gửi request từ trình duyệt tới <code>/api/auth/resend-confirmation</code> qua Vite proxy.</p>
        </div>
        <div className="card-body">
          <button
            onClick={handleTestProxy}
            disabled={testingProxy}
            className="btn btn-accent"
            id="test-proxy-btn"
          >
            {testingProxy ? 'Đang gửi request...' : 'Kiểm tra kết nối Proxy'}
          </button>

          {proxyTestResult && (
            <div className={`result-box ${proxyTestResult.startsWith('✅') ? 'success' : 'error'}`}>
              {proxyTestResult}
            </div>
          )}
        </div>
      </div>

      {/* Trạng thái xác thực hiện tại */}
      {isAuthenticated && user && (
        <div className="card user-status-card">
          <h3>Trạng thái đăng nhập hiện tại:</h3>
          <div className="user-details">
            <p><strong>Email:</strong> {user.email}</p>
            <p><strong>Tên hiển thị:</strong> {user.displayName}</p>
            <p><strong>Đã xác minh email:</strong> {user.emailConfirmed ? '✅ Có' : '❌ Chưa'}</p>
          </div>
        </div>
      )}
    </div>
  );
};
