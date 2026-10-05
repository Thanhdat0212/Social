import React from 'react';
import { ENV } from '@/config';

export const Footer: React.FC = () => {
  return (
    <footer className="app-footer">
      <div className="container footer-content">
        <div className="footer-brand-info">
          <span className="footer-brand">{ENV.APP_NAME}</span>
          <span className="footer-copyright">&copy; {new Date().getFullYear()} &bull; Nền tảng chia sẻ và kết nối cộng đồng</span>
        </div>
      </div>
    </footer>
  );
};
