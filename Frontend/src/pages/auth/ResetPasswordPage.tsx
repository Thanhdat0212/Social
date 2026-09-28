import React, { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { authApi } from '@/api/authApi';
import { useAuth } from '@/hooks/useAuth';
import { useTitle } from '@/hooks/useTitle';
import { getApiErrorMessage } from '@/utils/error';
import { ROUTES } from '@/constants/routes';

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
    <div className="auth-container">
      <div className="card auth-card">
        <div className="auth-header">
          <h2>Đặt lại mật khẩu</h2>
          <p>Tạo mật khẩu mới an toàn cho tài khoản của bạn</p>
        </div>

        {error && <div className="alert alert-error">{error}</div>}

        <form onSubmit={handleSubmit} className="auth-form">
          <div className="form-group">
            <label htmlFor="reset-new-password">Mật khẩu mới (tối thiểu 8 ký tự, gồm chữ hoa, thường và số)</label>
            <input
              id="reset-new-password"
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              placeholder="••••••••"
              required
              className="form-input"
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
              className="form-input"
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            className="btn btn-primary btn-block"
            id="reset-submit-btn"
          >
            {loading ? 'Đang lưu mật khẩu...' : 'Xác nhận đổi mật khẩu'}
          </button>
        </form>

        <div className="auth-footer">
          <Link to={ROUTES.AUTH.LOGIN} className="form-link">
            Quay lại đăng nhập
          </Link>
        </div>
      </div>
    </div>
  );
};
