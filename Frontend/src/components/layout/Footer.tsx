import React from 'react';
import { ENV } from '@/config';

export const Footer: React.FC = () => {
  return (
    <footer className="footer">
      <div className="container footer-content">
        <p>© 2026 {ENV.APP_NAME} — Phase 1 MVP (Auth &amp; Profile)</p>
        <div className="footer-links">
          <span className="api-status">
            <span className="status-dot"></span> API: <code>{ENV.API_BASE_URL}</code>
            {ENV.IS_DEV && <span> (Proxy: <code>{ENV.BACKEND_URL}</code>)</span>}
          </span>
        </div>
      </div>
    </footer>
  );
};
