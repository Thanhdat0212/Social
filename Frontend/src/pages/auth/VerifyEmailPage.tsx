import React, { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { authApi } from '@/api/authApi';
import { getApiErrorMessage } from '@/utils/error';
import { useTitle } from '@/hooks/useTitle';
import { ROUTES } from '@/constants/routes';
import { Button, Alert, CheckIcon, CloseIcon } from '@/components/common';

export const VerifyEmailPage: React.FC = () => {
  useTitle('Xác minh email');
  const [searchParams] = useSearchParams();
  const userId = searchParams.get('userId');
  const token = searchParams.get('token');

  const isInvalidParams = !userId || !token;

  const [status, setStatus] = useState<'verifying' | 'success' | 'error'>(() =>
    isInvalidParams ? 'error' : 'verifying'
  );
  const [message, setMessage] = useState<string>(() =>
    isInvalidParams ? 'Liên kết xác minh không hợp lệ hoặc thiếu thông tin xác thực.' : ''
  );
  const [resendEmail, setResendEmail] = useState('');
  const [resending, setResending] = useState(false);
  const [resendSuccess, setResendSuccess] = useState(false);

  useEffect(() => {
    if (!userId || !token) return;

    let isMounted = true;

    const verify = async () => {
      try {
        const res = await authApi.confirmEmail(userId, token);
        if (isMounted) {
          setStatus('success');
          setMessage(res.message || 'Xác minh email thành công!');
        }
      } catch (err) {
        if (isMounted) {
          setStatus('error');
          setMessage(getApiErrorMessage(err));
        }
      }
    };

    verify();

    return () => {
      isMounted = false;
    };
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
    <div className="auth-view-container">
      <div className="auth-box-card text-center">
        {status === 'verifying' && (
          <div className="py-6">
            <span className="spinner-inline" style={{ width: 32, height: 32, borderWidth: 3 }} />
            <h1 className="auth-box-title mt-4">Đang xác minh email...</h1>
            <p className="auth-box-subtitle mt-2">Vui lòng chờ trong giây lát.</p>
          </div>
        )}

        {status === 'success' && (
          <div className="py-6">
            <div className="auth-success-badge mx-auto">
              <CheckIcon size={32} />
            </div>
            <h1 className="auth-box-title mt-4">Xác minh thành công!</h1>
            <p className="auth-box-subtitle mt-2">{message}</p>
            <div className="mt-6">
              <Link to={ROUTES.AUTH.LOGIN}>
                <Button variant="primary" size="md" block id="verified-to-login-btn">
                  Đăng nhập ngay
                </Button>
              </Link>
            </div>
          </div>
        )}

        {status === 'error' && (
          <div className="py-6">
            <div className="auth-error-badge mx-auto">
              <CloseIcon size={32} />
            </div>
            <h1 className="auth-box-title mt-4">Xác minh thất bại</h1>
            <Alert type="error" message={message} className="mt-3 text-left" />

            <div className="resend-confirmation-box mt-6 text-left">
              <h4>Gửi lại liên kết xác minh</h4>
              <p className="text-sm text-muted">Nhập email tài khoản của bạn để nhận liên kết mới:</p>
              {resendSuccess ? (
                <Alert type="success" message="Liên kết xác minh mới đã được gửi! Vui lòng kiểm tra email." className="mt-2" />
              ) : (
                <form onSubmit={handleResend} className="mt-3">
                  <div className="form-group">
                    <input
                      type="email"
                      value={resendEmail}
                      onChange={(e) => setResendEmail(e.target.value)}
                      placeholder="name@example.com"
                      required
                      className="form-control"
                    />
                  </div>
                  <Button
                    type="submit"
                    variant="secondary"
                    size="sm"
                    block
                    loading={resending}
                    disabled={resending}
                  >
                    Gửi lại email
                  </Button>
                </form>
              )}
            </div>

            <div className="mt-6">
              <Link to={ROUTES.AUTH.LOGIN} className="link-subtle">
                &larr; Quay lại đăng nhập
              </Link>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
