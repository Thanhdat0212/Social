import React, { forwardRef } from 'react';
import { ChevronDownIcon } from './Icons';

export interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  isError?: boolean;
  selectSize?: 'sm' | 'md' | 'lg';
}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(
  ({ isError = false, selectSize = 'md', className = '', children, disabled, ...props }, ref) => {
    return (
      <div
        className={`app-select-wrapper size-${selectSize} ${isError ? 'is-error' : ''} ${disabled ? 'is-disabled' : ''}`}
      >
        <select
          ref={ref}
          disabled={disabled}
          className={`app-select-control ${className}`.trim()}
          {...props}
        >
          {children}
        </select>
        <span className="app-select-arrow" aria-hidden="true">
          <ChevronDownIcon size={16} />
        </span>
      </div>
    );
  }
);

Select.displayName = 'Select';
