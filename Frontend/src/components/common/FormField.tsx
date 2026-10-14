import React from 'react';

export interface FormFieldProps {
  id?: string;
  label?: React.ReactNode;
  required?: boolean;
  hint?: React.ReactNode;
  error?: string | null;
  extraAction?: React.ReactNode;
  className?: string;
  children: React.ReactNode;
}

export const FormField: React.FC<FormFieldProps> = ({
  id,
  label,
  required = false,
  hint,
  error,
  extraAction,
  className = '',
  children,
}) => {
  return (
    <div className={`form-field-group ${error ? 'has-error' : ''} ${className}`.trim()}>
      {(label || extraAction) && (
        <div className="form-field-header">
          {label && (
            <label htmlFor={id} className="form-field-label">
              {label}
              {required && <span className="field-required-star" aria-hidden="true"> *</span>}
            </label>
          )}
          {extraAction && <div className="form-field-extra">{extraAction}</div>}
        </div>
      )}
      <div className="form-field-control-wrapper">{children}</div>
      {error ? (
        <p className="form-field-error" role="alert" id={id ? `${id}-error` : undefined}>
          {error}
        </p>
      ) : hint ? (
        <p className="form-field-hint" id={id ? `${id}-hint` : undefined}>
          {hint}
        </p>
      ) : null}
    </div>
  );
};
