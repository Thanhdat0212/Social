import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { GoogleLogin } from '@react-oauth/google';
import { useAuth } from '@/hooks/useAuth';
import { useTitle } from '@/hooks/useTitle';
import { authApi } from '@/api/authApi';
import { interestApi } from '@/api/interestApi';
import { getApiErrorMessage } from '@/utils/error';
import { ROUTES } from '@/constants/routes';
import { Button, Alert, LogoIcon } from '@/components/common';

export const LoginPage: React.FC = () => {
  useTitle('Đăng nhập');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [needsConfirmation, setNeedsConfirmation] = useState(false);

  const { setAuth } = useAuth();
  const navigate = useNavigate();

  const handlePostLoginRedirect = async () => {
    try {
      const status = await interestApi.getOnboardingStatus();
      if (!status.isOnboarded) {
        navigate(ROUTES.ONBOARDING);
        return;
      }
    } catch {
      // Fallback
    }
    navigate(ROUTES.HOME);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setNeedsConfirmation(false);
    setLoading(true);

    try {
      const data = await authApi.login({ email, password });
      setAuth(data.accessToken, data.user);
      await handlePostLoginRedirect();
    } catch (err: unknown) {
      const msg = getApiErrorMessage(err);
      setError(msg);
      const axiosErr = err as { response?: { status?: number } };
      if (
        axiosErr.response?.status === 403 ||
        msg.includes('chưa được xác minh') ||
        msg.includes('EMAIL_NOT_CONFIRMED')
      ) {
        setNeedsConfirmation(true);
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-view-container">
      <div className="auth-box-card">
        <div className="auth-box-header">
          <div className="auth-logo-badge">
            <LogoIcon size={32} />
          </div>
          <h1 className="auth-box-title">Đăng nhập tài khoản</h1>
          <p className="auth-box-subtitle">Chào mừng bạn trở lại với Social</p>
        </div>

        {error && <Alert type="error" message={error} className="mb-4" />}

        <form onSubmit={handleSubmit} className="auth-box-form">
          <div className="form-group">
            <label htmlFor="login-email">Địa chỉ Email</label>
            <input
              id="login-email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="name@example.com"
              required
              className="form-control"
              autoComplete="email"
            />
          </div>

          <div className="form-group">
            <div className="form-label-row">
              <label htmlFor="login-password">Mật khẩu</label>
              <Link to={ROUTES.AUTH.FORGOT_PASSWORD} className="link-subtle">
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
              className="form-control"
              autoComplete="current-password"
            />
          </div>

          <Button
            type="submit"
            variant="primary"
            size="md"
            block
            loading={loading}
            disabled={loading}
            id="login-submit-btn"
          >
            Đăng nhập
          </Button>
        </form>

        <div className="auth-separator">
          <span>hoặc tiếp tục với</span>
        </div>

        <div className="google-auth-wrapper">
          <GoogleLogin
            onSuccess={async (credentialResponse) => {
              if (credentialResponse.credential) {
                setLoading(true);
                setError(null);
                try {
                  const data = await authApi.googleLogin({ idToken: credentialResponse.credential });
                  setAuth(data.accessToken, data.user);
                  await handlePostLoginRedirect();
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
          <div className="resend-confirmation-box">
            <p>Tài khoản chưa được kích hoạt?</p>
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={async () => {
                try {
                  await authApi.resendConfirmation({ email });
                  alert('Liên kết xác minh mới đã được gửi vào email của bạn!');
                } catch (resendErr) {
                  alert(getApiErrorMessage(resendErr));
                }
              }}
              id="resend-conf-btn"
            >
              Gửi lại email xác minh
            </Button>
          </div>
        )}

        <div className="auth-box-footer">
          <span>Chưa có tài khoản? </span>
          <Link to={ROUTES.AUTH.REGISTER} className="link-accent">
            Đăng ký ngay
          </Link>
        </div>
      </div>
    </div>
  );
};
