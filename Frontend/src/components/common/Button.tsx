import React from 'react';

export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'accent' | 'danger' | 'outline';
export type ButtonSize = 'xs' | 'sm' | 'md' | 'lg';

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  loading?: boolean;
  block?: boolean;
  icon?: React.ReactNode;
  iconRight?: React.ReactNode;
}

export const Button: React.FC<ButtonProps> = ({
  children,
  variant = 'primary',
  size = 'md',
  loading = false,
  block = false,
  icon,
  iconRight,
  disabled,
  className = '',
  ...props
}) => {
  const classes = [
    'btn',
    `btn-${variant}`,
    size !== 'md' ? `btn-${size}` : '',
    block ? 'btn-block' : '',
    loading ? 'btn-loading' : '',
    className,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <button disabled={disabled || loading} className={classes} {...props}>
      {loading ? (
        <span className="btn-spinner-wrap">
          <span className="spinner-inline" />
          {children && <span>{children}</span>}
        </span>
      ) : (
        <>
          {icon && <span className="btn-icon-left">{icon}</span>}
          {children && <span className="btn-text">{children}</span>}
          {iconRight && <span className="btn-icon-right">{iconRight}</span>}
        </>
      )}
    </button>
  );
};
