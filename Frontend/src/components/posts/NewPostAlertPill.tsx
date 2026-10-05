import React from 'react';
import { ArrowUpIcon } from '@/components/common';

interface NewPostAlertPillProps {
  count: number;
  onClick: () => void;
}

export const NewPostAlertPill: React.FC<NewPostAlertPillProps> = ({ count, onClick }) => {
  if (count <= 0) return null;

  return (
    <div className="new-post-pill-container">
      <button
        type="button"
        className="new-post-alert-pill"
        onClick={onClick}
        aria-label={`Xem ${count} bài viết mới`}
      >
        <span className="pill-arrow-wrap">
          <ArrowUpIcon size={14} />
        </span>
        <span className="pill-text">
          Có <strong>{count}</strong> bài viết mới &bull; Cuộn lên
        </span>
      </button>
    </div>
  );
};
