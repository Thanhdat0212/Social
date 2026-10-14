import React from 'react';
import { Spinner } from './Spinner';

interface RouteLoadingFallbackProps {
  message?: string;
}

export const RouteLoadingFallback: React.FC<RouteLoadingFallbackProps> = ({
  message = 'Đang tải dữ liệu...',
}) => {
  return (
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        minHeight: '60vh',
        width: '100%',
        padding: '2rem',
        animation: 'fadeIn 0.25s ease-out',
      }}
    >
      <div
        style={{
          position: 'relative',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          marginBottom: '1rem',
        }}
      >
        <div
          style={{
            position: 'absolute',
            width: '48px',
            height: '48px',
            borderRadius: '50%',
            background: 'radial-gradient(circle, rgba(99, 102, 241, 0.2) 0%, transparent 70%)',
            animation: 'pulse 2s infinite',
          }}
        />
        <Spinner size="lg" />
      </div>

      <p
        style={{
          fontSize: '0.9rem',
          color: 'var(--text-muted, #94a3b8)',
          fontWeight: 500,
          letterSpacing: '0.02em',
        }}
      >
        {message}
      </p>

      <style>{`
        @keyframes fadeIn {
          from { opacity: 0; transform: translateY(4px); }
          to { opacity: 1; transform: translateY(0); }
        }
        @keyframes pulse {
          0%, 100% { transform: scale(1); opacity: 0.5; }
          50% { transform: scale(1.4); opacity: 0.15; }
        }
      `}</style>
    </div>
  );
};
