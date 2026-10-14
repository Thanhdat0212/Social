import React from 'react';

export type BadgeVariant = 'primary' | 'success' | 'warning' | 'danger' | 'info' | 'neutral';
export type BadgeSize = 'sm' | 'md';

export interface BadgeProps extends React.HTMLAttributes<HTMLSpanElement> {
  variant?: BadgeVariant;
  size?: BadgeSize;
  icon?: React.ReactNode;
  dot?: boolean;
  children: React.ReactNode;
}

export const Badge: React.FC<BadgeProps> = ({
  variant = 'neutral',
  size = 'md',
  icon,
  dot = false,
  children,
  className = '',
  ...props
}) => {
  return (
    <span
      className={`app-badge badge-${variant} badge-${size} ${className}`.trim()}
      {...props}
    >
      {dot && <span className="app-badge-dot" aria-hidden="true" />}
      {icon && <span className="app-badge-icon" aria-hidden="true">{icon}</span>}
      <span className="app-badge-text">{children}</span>
    </span>
  );
};
