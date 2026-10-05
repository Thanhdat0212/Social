import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useTitle } from '@/hooks/useTitle';
import { authApi } from '@/api/authApi';
import { getApiErrorMessage } from '@/utils/error';
import { ROUTES } from '@/constants/routes';
import { Button, Alert, LogoIcon, CheckIcon } from '@/components/common';

export const RegisterPage: React.FC = () => {
  useTitle('Đăng ký tài khoản');
  const [displayName, setDisplayName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);

  const [cooldown, setCooldown] = useState(0);

  useEffect(() => {
    if (cooldown > 0) {
      const timer = setTimeout(() => setCooldown((prev) => prev - 1), 1000);
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
      setCooldown(60);
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
      <div className="auth-view-container">
        <div className="auth-box-card text-center">
          <div className="auth-success-badge">
            <CheckIcon size={28} />
          </div>
          <h1 className="auth-box-title">Đăng ký thành công!</h1>
          <p className="auth-box-subtitle mt-2">
            Chúng tôi đã gửi liên kết xác minh kích hoạt đến địa chỉ email:
          </p>
          <p className="font-bold text-accent mt-1">{email}</p>
          <p className="text-sm mt-3 text-muted">
            Vui lòng kiểm tra hộp thư đến (hoặc hòm thư rác/spam) và nhấp vào liên kết để kích hoạt tài khoản.
          </p>

          <div className="resend-confirmation-box mt-4">
            <p>Chưa nhận được email xác nhận?</p>
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={handleResend}
              disabled={cooldown > 0}
              id="resend-confirmation-btn"
            >
              {cooldown > 0 ? `Gửi lại sau ${cooldown}s` : 'Gửi lại email xác minh'}
            </Button>
          </div>

          <div className="mt-6">
            <Link to={ROUTES.AUTH.LOGIN}>
              <Button variant="primary" size="md" block>
                Quay lại đăng nhập
              </Button>
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="auth-view-container">
      <div className="auth-box-card">
        <div className="auth-box-header">
          <div className="auth-logo-badge">
            <LogoIcon size={32} />
          </div>
          <h1 className="auth-box-title">Tạo tài khoản mới</h1>
          <p className="auth-box-subtitle">Tham gia cộng đồng Social ngay hôm nay</p>
        </div>

        {error && <Alert type="error" message={error} className="mb-4" />}

        <form onSubmit={handleSubmit} className="auth-box-form">
          <div className="form-group">
            <label htmlFor="reg-name">Tên hiển thị</label>
            <input
              id="reg-name"
              type="text"
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              placeholder="Ví dụ: Nguyễn Văn A"
              required
              className="form-control"
              autoComplete="name"
            />
          </div>

          <div className="form-group">
            <label htmlFor="reg-email">Địa chỉ Email</label>
            <input
              id="reg-email"
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
            <label htmlFor="reg-password">Mật khẩu (tối thiểu 8 ký tự)</label>
            <input
              id="reg-password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              required
              className="form-control"
              autoComplete="new-password"
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
              className="form-control"
              autoComplete="new-password"
            />
          </div>

          <Button
            type="submit"
            variant="primary"
            size="md"
            block
            loading={loading}
            disabled={loading}
            id="register-submit-btn"
          >
            Đăng ký tài khoản
          </Button>
        </form>

        <div className="auth-box-footer">
          <span>Đã có tài khoản? </span>
          <Link to={ROUTES.AUTH.LOGIN} className="link-accent">
            Đăng nhập ngay
          </Link>
        </div>
      </div>
    </div>
  );
};
