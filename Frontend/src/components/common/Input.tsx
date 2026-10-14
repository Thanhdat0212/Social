import React, { forwardRef, useState } from 'react';
import { EyeIcon, EyeOffIcon, CloseIcon } from './Icons';

export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  leftIcon?: React.ReactNode;
  rightIcon?: React.ReactNode;
  isError?: boolean;
  clearable?: boolean;
  onClear?: () => void;
  passwordToggle?: boolean;
  inputSize?: 'sm' | 'md' | 'lg';
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  (
    {
      type = 'text',
      leftIcon,
      rightIcon,
      isError = false,
      clearable = false,
      onClear,
      passwordToggle = false,
      inputSize = 'md',
      className = '',
      value,
      disabled,
      ...props
    },
    ref
  ) => {
    const [showPassword, setShowPassword] = useState(false);

    const actualType = passwordToggle ? (showPassword ? 'text' : 'password') : type;
    const hasValue = value !== undefined && value !== null && String(value).length > 0;

    return (
      <div
        className={`app-input-wrapper size-${inputSize} ${isError ? 'is-error' : ''} ${disabled ? 'is-disabled' : ''}`}
      >
        {leftIcon && <span className="app-input-icon left">{leftIcon}</span>}
        <input
          ref={ref}
          type={actualType}
          value={value}
          disabled={disabled}
          className={`app-input-control ${className}`.trim()}
          {...props}
        />
        {clearable && hasValue && !disabled && (
          <button
            type="button"
            className="app-input-action-btn"
            onClick={onClear}
            tabIndex={-1}
            aria-label="Xóa nội dung"
          >
            <CloseIcon size={14} />
          </button>
        )}
        {passwordToggle && !disabled && (
          <button
            type="button"
            className="app-input-action-btn"
            onClick={() => setShowPassword((prev) => !prev)}
            aria-label={showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
            tabIndex={-1}
          >
            {showPassword ? <EyeOffIcon size={16} /> : <EyeIcon size={16} />}
          </button>
        )}
        {rightIcon && !passwordToggle && <span className="app-input-icon right">{rightIcon}</span>}
      </div>
    );
  }
);

Input.displayName = 'Input';
