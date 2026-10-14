import React from 'react';

export type PageContainerVariant = 'feed' | 'wide' | 'narrow' | 'full';

export interface PageContainerProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: PageContainerVariant;
  className?: string;
  children: React.ReactNode;
}

export const PageContainer: React.FC<PageContainerProps> = ({
  variant = 'feed',
  className = '',
  children,
  ...props
}) => {
  return (
    <div
      className={`app-container app-container--${variant} ${className}`.trim()}
      {...props}
    >
      {children}
    </div>
  );
};
