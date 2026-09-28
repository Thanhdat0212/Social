import React from 'react';

interface LoadingScreenProps {
  message?: string;
}

export const LoadingScreen: React.FC<LoadingScreenProps> = ({
  message = 'Đang tải dữ liệu...',
}) => {
  return (
    <div className="flex-center min-h-[60vh] flex-col gap-4">
      <div className="spinner"></div>
      <p className="loading-text">{message}</p>
    </div>
  );
};
