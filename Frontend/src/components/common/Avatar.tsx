import React, { useState } from 'react';

export type AvatarSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

interface AvatarProps {
  src?: string | null;
  name?: string | null;
  size?: AvatarSize;
  className?: string;
  alt?: string;
}

const sizeMap: Record<AvatarSize, number> = {
  xs: 24,
  sm: 32,
  md: 40,
  lg: 56,
  xl: 88,
};

// Deterministic subtle gradient based on user display name
const getFallbackGradient = (name: string): string => {
  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash);
  }
  const hues = [
    ['#4f46e5', '#6366f1'], // Indigo
    ['#2563eb', '#3b82f6'], // Blue
    ['#0891b2', '#06b6d4'], // Cyan
    ['#059669', '#10b981'], // Emerald
    ['#d97706', '#f59e0b'], // Amber
    ['#e11d48', '#f43f5e'], // Rose
    ['#7c3aed', '#8b5cf6'], // Purple
  ];
  const pair = hues[Math.abs(hash) % hues.length];
  return `linear-gradient(135deg, ${pair[0]}, ${pair[1]})`;
};

export const Avatar: React.FC<AvatarProps> = ({
  src,
  name = 'User',
  size = 'md',
  className = '',
  alt,
}) => {
  const [imageError, setImageError] = useState(false);
  const px = sizeMap[size];
  const safeName = name?.trim() || 'U';
  const initial = safeName.charAt(0).toUpperCase();

  const containerStyle: React.CSSProperties = {
    width: px,
    height: px,
    minWidth: px,
    minHeight: px,
  };

  const hasValidImage = src && !imageError;

  return (
    <div
      className={`app-avatar avatar-${size} ${className}`.trim()}
      style={containerStyle}
      title={safeName}
    >
      {hasValidImage ? (
        <img
          src={src}
          alt={alt || safeName}
          className="app-avatar-img"
          onError={() => setImageError(true)}
          loading="lazy"
        />
      ) : (
        <div
          className="app-avatar-fallback"
          style={{ background: getFallbackGradient(safeName) }}
        >
          {initial}
        </div>
      )}
    </div>
  );
};
