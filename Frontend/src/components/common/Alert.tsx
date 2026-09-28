import React from 'react';

interface AlertProps {
  type?: 'error' | 'success' | 'warning' | 'info';
  message?: string | null;
  children?: React.ReactNode;
  className?: string;
}

export const Alert: React.FC<AlertProps> = ({
  type = 'info',
  message,
  children,
  className = '',
}) => {
  if (!message && !children) return null;

  return (
    <div className={`alert alert-${type} ${className}`.trim()} role="alert">
      {message || children}
    </div>
  );
};
