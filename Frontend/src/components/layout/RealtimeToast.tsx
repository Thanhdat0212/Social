import React, { useEffect } from 'react';
import { useNotificationStore, usePostModalStore } from '@/store';
import {
  Avatar,
  HeartIcon,
  CommentIcon,
  UserIcon,
  BellIcon,
  CloseIcon,
  SparklesIcon,
} from '@/components/common';

export const RealtimeToast: React.FC = () => {
  const { activeToast, clearActiveToast } = useNotificationStore();
  const { openPostModal } = usePostModalStore();

  useEffect(() => {
    if (!activeToast) return;
    const timer = setTimeout(() => {
      clearActiveToast();
    }, 4500);

    return () => clearTimeout(timer);
  }, [activeToast, clearActiveToast]);

  if (!activeToast) return null;

  const handleToastClick = () => {
    if (activeToast.targetPostId) {
      openPostModal(activeToast.targetPostId);
    }
    clearActiveToast();
  };

  const renderBadge = () => {
    if (activeToast.triggeredByUserAvatar) {
      return (
        <Avatar
          src={activeToast.triggeredByUserAvatar}
          name={activeToast.triggeredByUserName}
          size="sm"
        />
      );
    }

    switch (activeToast.type) {
      case 'like':
        return <HeartIcon size={18} filled className="toast-icon-like" />;
      case 'comment':
        return <CommentIcon size={18} className="toast-icon-comment" />;
      case 'follow':
        return <UserIcon size={18} className="toast-icon-follow" />;
      case 'new_post':
        return <SparklesIcon size={18} className="toast-icon-post" />;
      default:
        return <BellIcon size={18} className="toast-icon-default" />;
    }
  };

  return (
    <div className="realtime-toast-container" role="status" aria-live="polite">
      <div className="realtime-toast-card" onClick={handleToastClick}>
        <div className="toast-avatar-col">{renderBadge()}</div>
        <div className="toast-content">
          <span className="toast-title">Thông báo</span>
          <p className="toast-message">{activeToast.message}</p>
        </div>
        <button
          type="button"
          className="toast-close-btn"
          onClick={(e) => {
            e.stopPropagation();
            clearActiveToast();
          }}
          aria-label="Đóng thông báo"
        >
          <CloseIcon size={14} />
        </button>
      </div>
    </div>
  );
};
