import React, { useEffect, useState } from 'react';

interface ScrollToTopBtnProps {
  hasNewPosts?: boolean;
  onClick?: () => void;
}

export const ScrollToTopBtn: React.FC<ScrollToTopBtnProps> = ({ hasNewPosts, onClick }) => {
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    const handleScroll = () => {
      setVisible(window.scrollY > 450);
    };

    window.addEventListener('scroll', handleScroll, { passive: true });
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  if (!visible) return null;

  const handleClick = () => {
    window.scrollTo({ top: 0, behavior: 'smooth' });
    onClick?.();
  };

  return (
    <button
      type="button"
      className={`scroll-to-top-btn ${hasNewPosts ? 'has-new-posts' : ''}`}
      onClick={handleClick}
      aria-label="Cuộn lên đầu trang"
      title="Cuộn lên đầu trang"
    >
      <span className="scroll-arrow">↑</span>
      {hasNewPosts && <span className="scroll-dot" />}
    </button>
  );
};
