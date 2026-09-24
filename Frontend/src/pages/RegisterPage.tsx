import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { GoogleLogin } from '@react-oauth/google';
import { useAuthStore } from '@/store/authStore';
import { authApi } from '@/api/authApi';
import { getApiErrorMessage } from '@/api/client';

export const RegisterPage: React.FC = () => {
  const { setAuth } = useAuthStore();
  const navigate = useNavigate();
  const [displayName, setDisplayName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);

  // Cooldown timer cho nút gửi lại email
  const [cooldown, setCooldown] = useState(0);

  useEffect(() => {
    if (cooldown > 0) {
      const timer = setTimeout(() => setCooldown(cooldown - 1), 1000);
      return () => clearTimeout(timer);
    }
  }, [cooldown]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (password !== confirmPassword) {
      setError('Mật khẩu xác nhận không khớp.');
      return;
    }

    setLoading(true);

    try {
      await authApi.register({ displayName, email, password, confirmPassword });
      setIsSuccess(true);
      setCooldown(60); // Bắt đầu đếm ngược 60s
    } catch (err) {
      setError(getApiErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  const handleResend = async () => {
    if (cooldown > 0) return;
    try {
      await authApi.resendConfirmation({ email });
      setCooldown(60);
      alert('Đã gửi lại email xác minh. Vui lòng kiểm tra hộp thư.');
    } catch (err) {
      alert(getApiErrorMessage(err));
    }
  };

  if (isSuccess) {
    return (
      <div className="auth-container">
        <div className="card auth-card text-center">
          <div className="auth-success-icon">✉️</div>
          <h2>Đăng ký thành công!</h2>
          <p className="mt-2 text-muted">
            Chúng tôi đã gửi liên kết xác minh đến địa chỉ email:
          </p>
          <p className="font-bold text-accent">{email}</p>
          <p className="text-sm mt-3 text-muted">
            Vui lòng kiểm tra hộp thư đến (hoặc hòm thư rác/spam) và nhấp vào liên kết để kích hoạt tài khoản.
          </p>

          <div className="resend-box mt-4">
            <p className="text-sm">Chưa nhận được email?</p>
            <button
              onClick={handleResend}
              disabled={cooldown > 0}
              className="btn btn-secondary btn-sm mt-2"
              id="resend-confirmation-btn"
            >
              {cooldown > 0 ? `Gửi lại sau ${cooldown}s` : 'Gửi lại email xác minh'}
            </button>
          </div>

          <div className="mt-4">
            <Link to="/login" className="btn btn-primary btn-block">
              Quay lại đăng nhập
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="auth-container">
      <div className="card auth-card">
        <div className="auth-header">
          <h2>Tạo tài khoản</h2>
          <p>Tham gia cộng đồng Social ngay hôm nay</p>
        </div>

        {error && <div className="alert alert-error">{error}</div>}

        <form onSubmit={handleSubmit} className="auth-form">
          <div className="form-group">
            <label htmlFor="reg-name">Tên hiển thị</label>
            <input
              id="reg-name"
              type="text"
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              placeholder="Nguyễn Văn A"
              required
              className="form-input"
            />
          </div>

          <div className="form-group">
            <label htmlFor="reg-email">Email</label>
            <input
              id="reg-email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="name@example.com"
              required
              className="form-input"
            />
          </div>

          <div className="form-group">
            <label htmlFor="reg-password">Mật khẩu (tối thiểu 8 ký tự, gồm số và ký tự đặc biệt)</label>
            <input
              id="reg-password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              required
              className="form-input"
            />
          </div>

          <div className="form-group">
            <label htmlFor="reg-confirm-password">Xác nhận mật khẩu</label>
            <input
              id="reg-confirm-password"
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              placeholder="••••••••"
              required
              className="form-input"
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            className="btn btn-primary btn-block"
            id="register-submit-btn"
          >
            {loading ? 'Đang khởi tạo tài khoản...' : 'Đăng ký tài khoản'}
          </button>
        </form>

        <div className="auth-divider">
          <span>hoặc tiếp tục với</span>
        </div>

        <div className="google-auth-btn-wrapper">
          <GoogleLogin
            onSuccess={async (credentialResponse) => {
              if (credentialResponse.credential) {
                setLoading(true);
                setError(null);
                try {
                  const data = await authApi.googleLogin({ idToken: credentialResponse.credential });
                  setAuth(data.accessToken, data.user);
                  navigate('/profile');
                } catch (err) {
                  setError(getApiErrorMessage(err));
                } finally {
                  setLoading(false);
                }
              }
            }}
            onError={() => {
              setError('Đăng nhập bằng Google thất bại. Vui lòng thử lại.');
            }}
            text="signup_with"
            shape="pill"
            size="large"
            theme="filled_black"
          />
        </div>

        <div className="auth-footer">
          Đã có tài khoản?{' '}
          <Link to="/login" className="form-link" id="to-login-link">
            Đăng nhập
          </Link>
        </div>
      </div>
    </div>
  );
};
