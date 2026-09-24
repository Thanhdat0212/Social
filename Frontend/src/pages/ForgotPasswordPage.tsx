import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { authApi } from '@/api/authApi';
import { getApiErrorMessage } from '@/api/client';

export const ForgotPasswordPage: React.FC = () => {
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
    <div className="auth-container">
      <div className="card auth-card">
        <div className="auth-header">
          <h2>Quên mật khẩu</h2>
          <p>Nhập email để nhận liên kết đặt lại mật khẩu</p>
        </div>

        {error && <div className="alert alert-error">{error}</div>}

        {submitted ? (
          <div className="text-center py-4">
            <div className="auth-success-icon">📬</div>
            <h3>Yêu cầu đã được gửi!</h3>
            <p className="text-muted mt-2">
              Nếu email <strong>{email}</strong> tồn tại trong hệ thống, chúng tôi đã gửi liên kết đặt lại mật khẩu có hiệu lực trong 1 giờ.
            </p>
            <div className="mt-6">
              <Link to="/login" className="btn btn-primary btn-block">
                Quay lại đăng nhập
              </Link>
            </div>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="auth-form">
            <div className="form-group">
              <label htmlFor="forgot-email">Email tài khoản</label>
              <input
                id="forgot-email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="name@example.com"
                required
                className="form-input"
              />
            </div>

            <button
              type="submit"
              disabled={loading}
              className="btn btn-primary btn-block"
              id="forgot-submit-btn"
            >
              {loading ? 'Đang gửi...' : 'Gửi liên kết đặt lại mật khẩu'}
            </button>

            <div className="auth-footer">
              Nhớ mật khẩu?{' '}
              <Link to="/login" className="form-link">
                Đăng nhập
              </Link>
            </div>
          </form>
        )}
      </div>
    </div>
  );
};
