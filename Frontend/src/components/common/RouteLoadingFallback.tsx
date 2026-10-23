import React from 'react';
import { Spinner } from './Spinner';

interface RouteLoadingFallbackProps {
  message?: string;
}

export const RouteLoadingFallback: React.FC<RouteLoadingFallbackProps> = ({
  message = 'Đang tải dữ liệu...',
}) => {
  return (
    <div className="route-loading-fallback">
      <div className="route-loading-spinner-wrap">
        <div className="route-loading-glow" />
        <Spinner size="lg" />
      </div>
      <p className="route-loading-text">{message}</p>
    </div>
  );
};
