import React from 'react';
import { Link } from 'react-router-dom';
import { useTitle } from '@/hooks/useTitle';
import { ROUTES } from '@/constants/routes';
import { Button, LogoIcon } from '@/components/common';

export const NotFoundPage: React.FC = () => {
  useTitle('404 - Không tìm thấy trang');

  return (
    <div className="auth-view-container">
      <div className="auth-box-card text-center py-8">
        <div className="auth-logo-badge mx-auto">
          <LogoIcon size={32} />
        </div>
        <h1 className="notfound-code">404</h1>
        <h2 className="auth-box-title">Trang không tồn tại</h2>
        <p className="auth-box-subtitle mt-2">
          Đường dẫn bạn yêu cầu không tồn tại hoặc đã được di chuyển sang địa chỉ mới.
        </p>
        <div className="mt-6">
          <Link to={ROUTES.HOME}>
            <Button variant="primary" size="md" id="not-found-home-btn">
              Trở về trang chủ
            </Button>
          </Link>
        </div>
      </div>
    </div>
  );
};
