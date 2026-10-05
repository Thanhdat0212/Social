import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { authApi } from '@/api/authApi';
import { getApiErrorMessage } from '@/utils/error';
import { useTitle } from '@/hooks/useTitle';
import { ROUTES } from '@/constants/routes';
import { Button, Alert, LogoIcon, CheckIcon } from '@/components/common';

export const ForgotPasswordPage: React.FC = () => {
  useTitle('Quên mật khẩu');
  const [email, setEmail] = useState('');
  const [loading, setLoading] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      await authApi.forgotPassword({ email });
      setSubmitted(true);
    } catch (err) {
      setError(getApiErrorMessage(err));
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
          <h1 className="auth-box-title">Quên mật khẩu</h1>
          <p className="auth-box-subtitle">Nhập email để nhận liên kết đặt lại mật khẩu</p>
        </div>

        {error && <Alert type="error" message={error} className="mb-4" />}

        {submitted ? (
          <div className="text-center py-4">
            <div className="auth-success-badge mx-auto">
              <CheckIcon size={28} />
            </div>
            <h2 className="auth-box-title mt-4">Yêu cầu đã được gửi!</h2>
            <p className="auth-box-subtitle mt-2">
              Nếu địa chỉ <strong>{email}</strong> tồn tại trong hệ thống, chúng tôi đã gửi liên kết đặt lại mật khẩu có hiệu lực trong 1 giờ.
            </p>
            <div className="mt-6">
              <Link to={ROUTES.AUTH.LOGIN}>
                <Button variant="primary" size="md" block>
                  Quay lại đăng nhập
                </Button>
              </Link>
            </div>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="auth-box-form">
            <div className="form-group">
              <label htmlFor="forgot-email">Email tài khoản của bạn</label>
              <input
                id="forgot-email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="name@example.com"
                required
                className="form-control"
                autoComplete="email"
              />
            </div>

            <Button
              type="submit"
              variant="primary"
              size="md"
              block
              loading={loading}
              disabled={loading}
              id="forgot-submit-btn"
            >
              Gửi liên kết đặt lại mật khẩu
            </Button>

            <div className="auth-box-footer">
              <span>Đã nhớ lại mật khẩu? </span>
              <Link to={ROUTES.AUTH.LOGIN} className="link-accent">
                Đăng nhập
              </Link>
            </div>
          </form>
        )}
      </div>
    </div>
  );
};
