import React from 'react';
import { Link } from 'react-router-dom';

export const NotFoundPage: React.FC = () => {
  return (
    <div className="auth-container">
      <div className="card auth-card text-center py-12">
        <h1 className="text-6xl font-extrabold text-accent">404</h1>
        <h2 className="mt-2">Trang không tồn tại</h2>
        <p className="text-muted mt-2">
          Đường dẫn bạn yêu cầu không tồn tại hoặc đã được di chuyển.
        </p>
        <div className="mt-6">
          <Link to="/" className="btn btn-primary" id="not-found-home-btn">
            Về trang chủ
          </Link>
        </div>
      </div>
    </div>
  );
};
