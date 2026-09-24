import React, { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { authApi } from '@/api/authApi';
import { getApiErrorMessage } from '@/api/client';

export const VerifyEmailPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const userId = searchParams.get('userId');
  const token = searchParams.get('token');

  const [status, setStatus] = useState<'verifying' | 'success' | 'error'>('verifying');
  const [message, setMessage] = useState('');
  const [resendEmail, setResendEmail] = useState('');
  const [resending, setResending] = useState(false);
  const [resendSuccess, setResendSuccess] = useState(false);

  useEffect(() => {
    if (!userId || !token) {
      setStatus('error');
      setMessage('Liên kết xác minh không hợp lệ hoặc thiếu thông tin.');
      return;
    }

    const verify = async () => {
      try {
        const res = await authApi.confirmEmail(userId, token);
        setStatus('success');
        setMessage(res.message || 'Xác minh email thành công!');
      } catch (err) {
        setStatus('error');
        setMessage(getApiErrorMessage(err));
      }
    };

    verify();
  }, [userId, token]);

  const handleResend = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!resendEmail) return;

    setResending(true);
    try {
      await authApi.resendConfirmation({ email: resendEmail });
      setResendSuccess(true);
    } catch (err) {
      alert(getApiErrorMessage(err));
    } finally {
      setResending(false);
    }
  };

  return (
    <div className="auth-container">
      <div className="card auth-card text-center">
        {status === 'verifying' && (
          <div className="py-8">
            <div className="spinner mx-auto"></div>
            <h2 className="mt-4">Đang xác minh email...</h2>
            <p className="text-muted mt-2">Vui lòng chờ trong giây lát.</p>
          </div>
        )}

        {status === 'success' && (
          <div className="py-6">
            <div className="auth-success-icon">✅</div>
            <h2>Xác minh thành công!</h2>
            <p className="text-muted mt-2">{message}</p>
            <div className="mt-6">
              <Link to="/login" className="btn btn-primary btn-block" id="verified-to-login-btn">
                Đăng nhập ngay
              </Link>
            </div>
          </div>
        )}

        {status === 'error' && (
          <div className="py-6">
            <div className="auth-error-icon">⚠️</div>
            <h2>Xác minh thất bại</h2>
            <p className="alert alert-error mt-3">{message}</p>

            <div className="resend-section mt-6 text-left">
              <h4>Gửi lại liên kết xác minh</h4>
              <p className="text-sm text-muted">Nhập email tài khoản của bạn để nhận liên kết mới:</p>
              {resendSuccess ? (
                <div className="alert alert-success mt-2">
                  Liên kết xác minh mới đã được gửi! Vui lòng kiểm tra email.
                </div>
              ) : (
                <form onSubmit={handleResend} className="mt-3">
                  <div className="form-group">
                    <input
                      type="email"
                      value={resendEmail}
                      onChange={(e) => setResendEmail(e.target.value)}
                      placeholder="name@example.com"
                      required
                      className="form-input"
                    />
                  </div>
                  <button
                    type="submit"
                    disabled={resending}
                    className="btn btn-secondary btn-block"
                  >
                    {resending ? 'Đang gửi...' : 'Gửi lại email'}
                  </button>
                </form>
              )}
            </div>

            <div className="mt-6">
              <Link to="/login" className="form-link">
                Quay lại đăng nhập
              </Link>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
