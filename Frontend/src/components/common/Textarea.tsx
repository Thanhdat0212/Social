import React, { forwardRef, useEffect, useRef } from 'react';

export interface TextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  isError?: boolean;
  autoGrow?: boolean;
  minRows?: number;
  maxRows?: number;
}

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(
  (
    {
      isError = false,
      autoGrow = false,
      minRows = 3,
      maxRows = 12,
      rows = 3,
      className = '',
      onChange,
      value,
      ...props
    },
    ref
  ) => {
    const internalRef = useRef<HTMLTextAreaElement | null>(null);

    const setRefs = (elem: HTMLTextAreaElement | null) => {
      internalRef.current = elem;
      if (typeof ref === 'function') {
        ref(elem);
      } else if (ref) {
        ref.current = elem;
      }
    };

    const adjustHeight = () => {
      const textarea = internalRef.current;
      if (!textarea || !autoGrow) return;
      textarea.style.height = 'auto';
      const lineHeight = 20; // approximate px line height
      const minHeight = minRows * lineHeight;
      const maxHeight = maxRows * lineHeight;
      const scrollHeight = textarea.scrollHeight;
      const newHeight = Math.min(Math.max(scrollHeight, minHeight), maxHeight);
      textarea.style.height = `${newHeight}px`;
    };

    useEffect(() => {
      adjustHeight();
    }, [value, autoGrow]);

    const handleChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
      adjustHeight();
      onChange?.(e);
    };

    return (
      <div className={`app-textarea-wrapper ${isError ? 'is-error' : ''} ${props.disabled ? 'is-disabled' : ''}`}>
        <textarea
          ref={setRefs}
          rows={rows}
          value={value}
          onChange={handleChange}
          className={`app-textarea-control ${className}`.trim()}
          {...props}
        />
      </div>
    );
  }
);

Textarea.displayName = 'Textarea';
