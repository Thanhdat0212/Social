import React from 'react';
import { PackageIcon } from './Icons';

export interface EmptyStateProps {
  icon?: React.ReactNode;
  title: string;
  description?: string;
  action?: React.ReactNode;
  compact?: boolean;
  className?: string;
}

export const EmptyState: React.FC<EmptyStateProps> = ({
  icon,
  title,
  description,
  action,
  compact = false,
  className = '',
}) => {
  return (
    <div className={`app-empty-state ${compact ? 'is-compact' : ''} ${className}`.trim()}>
      <div className="empty-state-icon" aria-hidden="true">
        {icon || <PackageIcon size={compact ? 32 : 48} />}
      </div>
      <h3 className="empty-state-title">{title}</h3>
      {description && <p className="empty-state-desc">{description}</p>}
      {action && <div className="empty-state-action">{action}</div>}
    </div>
  );
};
