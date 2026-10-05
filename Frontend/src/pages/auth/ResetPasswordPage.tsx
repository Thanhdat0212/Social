import React, { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { authApi } from '@/api/authApi';
import { useAuth } from '@/hooks/useAuth';
import { useTitle } from '@/hooks/useTitle';
import { getApiErrorMessage } from '@/utils/error';
import { ROUTES } from '@/constants/routes';
import { Button, Alert, LogoIcon } from '@/components/common';

export const ResetPasswordPage: React.FC = () => {
  useTitle('Đặt lại mật khẩu');
  const [searchParams] = useSearchParams();
  const userId = searchParams.get('userId') || '';
  const token = searchParams.get('token') || '';

  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const { clearAuth } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!userId || !token) {
      setError('Liên kết đặt lại mật khẩu không hợp lệ hoặc thiếu mã xác thực.');
      return;
    }

    if (newPassword !== confirmPassword) {
      setError('Mật khẩu xác nhận không khớp.');
      return;
    }

    setLoading(true);

    try {
      await authApi.resetPassword({ userId, token, newPassword, confirmPassword });
      clearAuth();
      alert('Đặt lại mật khẩu thành công! Vui lòng đăng nhập bằng mật khẩu mới.');
      navigate(ROUTES.AUTH.LOGIN);
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
          <h1 className="auth-box-title">Đặt lại mật khẩu</h1>
          <p className="auth-box-subtitle">Tạo mật khẩu mới an toàn cho tài khoản của bạn</p>
        </div>

        {error && <Alert type="error" message={error} className="mb-4" />}

        <form onSubmit={handleSubmit} className="auth-box-form">
          <div className="form-group">
            <label htmlFor="reset-new-password">Mật khẩu mới (tối thiểu 8 ký tự)</label>
            <input
              id="reset-new-password"
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              placeholder="••••••••"
              required
              className="form-control"
              autoComplete="new-password"
            />
          </div>

          <div className="form-group">
            <label htmlFor="reset-confirm-password">Xác nhận mật khẩu mới</label>
            <input
              id="reset-confirm-password"
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
            id="reset-submit-btn"
          >
            Lưu mật khẩu mới
          </Button>
        </form>

        <div className="auth-box-footer">
          <Link to={ROUTES.AUTH.LOGIN} className="link-subtle">
            &larr; Quay lại đăng nhập
          </Link>
        </div>
      </div>
    </div>
  );
};
