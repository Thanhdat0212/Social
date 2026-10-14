import React from 'react';
import { AlertCircleIcon, RefreshIcon } from './Icons';
import { Button } from './Button';

export interface ErrorStateProps {
  icon?: React.ReactNode;
  title?: string;
  message: string;
  onRetry?: () => void;
  retryText?: string;
  className?: string;
}

export const ErrorState: React.FC<ErrorStateProps> = ({
  icon,
  title = 'Đã xảy ra sự cố',
  message,
  onRetry,
  retryText = 'Thử lại ngay',
  className = '',
}) => {
  return (
    <div className={`app-error-state ${className}`.trim()} role="alert">
      <div className="error-state-icon" aria-hidden="true">
        {icon || <AlertCircleIcon size={40} />}
      </div>
      <h3 className="error-state-title">{title}</h3>
      <p className="error-state-message">{message}</p>
      {onRetry && (
        <div className="error-state-action">
          <Button
            type="button"
            variant="secondary"
            size="sm"
            icon={<RefreshIcon size={15} />}
            onClick={onRetry}
          >
            {retryText}
          </Button>
        </div>
      )}
    </div>
  );
};
