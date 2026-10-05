import React from 'react';

interface LoadingScreenProps {
  message?: string;
}

export const LoadingScreen: React.FC<LoadingScreenProps> = ({
  message = 'Đang tải dữ liệu...',
}) => {
  return (
    <div className="loading-screen-wrap">
      <span className="spinner-inline" style={{ width: 36, height: 36, borderWidth: 3 }} />
      <p className="loading-screen-text">{message}</p>
    </div>
  );
};
