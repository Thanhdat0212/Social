import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { GoogleLogin } from '@react-oauth/google';
import { useAuthStore } from '@/store/authStore';
import { authApi } from '@/api/authApi';
import { getApiErrorMessage } from '@/api/client';

export const LoginPage: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [needsConfirmation, setNeedsConfirmation] = useState(false);

  const { setAuth } = useAuthStore();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setNeedsConfirmation(false);
    setLoading(true);

    try {
      const data = await authApi.login({ email, password });
      setAuth(data.accessToken, data.user);
      navigate('/profile');
    } catch (err: any) {
      const msg = getApiErrorMessage(err);
      setError(msg);
      if (err.response?.status === 403 || msg.includes('chưa được xác minh') || msg.includes('EMAIL_NOT_CONFIRMED')) {
        setNeedsConfirmation(true);
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-container">
      <div className="card auth-card">
        <div className="auth-header">
          <h2>Đăng nhập</h2>
          <p>Chào mừng bạn trở lại với mạng xã hội Social</p>
        </div>

        {error && <div className="alert alert-error">{error}</div>}

        <form onSubmit={handleSubmit} className="auth-form">
          <div className="form-group">
            <label htmlFor="login-email">Email</label>
            <input
              id="login-email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="name@example.com"
              required
              className="form-input"
            />
          </div>

          <div className="form-group">
            <div className="form-label-row">
              <label htmlFor="login-password">Mật khẩu</label>
              <Link to="/forgot-password" className="form-link">
                Quên mật khẩu?
              </Link>
            </div>
            <input
              id="login-password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              required
              className="form-input"
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            className="btn btn-primary btn-block"
            id="login-submit-btn"
          >
            {loading ? 'Đang đăng nhập...' : 'Đăng nhập'}
          </button>
        </form>

        <div className="auth-divider">
          <span>hoặc đăng nhập bằng</span>
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
            text="signin_with"
            shape="pill"
            size="large"
            theme="filled_black"
          />
        </div>

        {needsConfirmation && (
          <div className="alert alert-warning mt-4">
            <p>Tài khoản chưa xác minh email?</p>
            <button
              onClick={async () => {
                try {
                  await authApi.resendConfirmation({ email });
                  alert('Liên kết xác minh mới đã được gửi vào email của bạn!');
                } catch (resendErr) {
                  alert(getApiErrorMessage(resendErr));
                }
              }}
              className="btn btn-secondary btn-sm mt-2"
              id="resend-conf-btn"
            >
              Gửi lại email xác minh
            </button>
          </div>
        )}

        <div className="auth-footer">
          Chưa có tài khoản?{' '}
          <Link to="/register" className="form-link" id="to-register-link">
            Đăng ký ngay
          </Link>
        </div>
      </div>
    </div>
  );
};
