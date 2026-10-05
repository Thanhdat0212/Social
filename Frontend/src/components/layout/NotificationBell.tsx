import React, { useEffect, useRef, useState } from 'react';
import { useNotificationStore } from '@/store';
import { useSignalR } from '@/hooks/useSignalR';
import type { UserNotificationEvent } from '@/types';
import { formatRelativeTime } from '@/utils/date';
import {
  BellIcon,
  HeartIcon,
  CommentIcon,
  UserIcon,
  SparklesIcon,
  Avatar,
} from '@/components/common';

export const NotificationBell: React.FC = () => {
  const { notifications, unreadCount, addNotification, markAllAsRead } = useNotificationStore();
  const [isOpen, setIsOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  // Lắng nghe sự kiện ReceiveNotification từ SignalR Hub
  useSignalR<UserNotificationEvent>('ReceiveNotification', (notification) => {
    if (notification) {
      addNotification(notification);
    }
  });

  // Đóng dropdown khi click ra ngoài
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isOpen]);

  const handleToggle = () => {
    if (!isOpen && unreadCount > 0) {
      markAllAsRead();
    }
    setIsOpen((prev) => !prev);
  };

  const handleItemClick = (targetPostId?: string) => {
    setIsOpen(false);
    if (targetPostId) {
      const element = document.getElementById(`post-${targetPostId}`);
      if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }
    }
  };

  const renderTypeIcon = (type: string) => {
    switch (type) {
      case 'like':
        return <HeartIcon size={12} filled className="type-icon-like" />;
      case 'comment':
        return <CommentIcon size={12} className="type-icon-comment" />;
      case 'follow':
        return <UserIcon size={12} className="type-icon-follow" />;
      case 'new_post':
        return <SparklesIcon size={12} className="type-icon-post" />;
      default:
        return <BellIcon size={12} />;
    }
  };

  return (
    <div className="notification-bell-wrapper" ref={menuRef}>
      <button
        type="button"
        className={`notification-bell-btn ${isOpen ? 'active' : ''}`}
        onClick={handleToggle}
        aria-label="Thông báo"
        title="Thông báo realtime"
      >
        <BellIcon size={19} />
        {unreadCount > 0 && (
          <span className="notification-badge">
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <div className="notification-dropdown">
          <div className="notification-header">
            <h3 className="notification-title">Thông báo</h3>
            {notifications.length > 0 && (
              <button
                type="button"
                className="notification-clear-btn"
                onClick={markAllAsRead}
              >
                Đã đọc tất cả
              </button>
            )}
          </div>

          <div className="notification-list">
            {notifications.length === 0 ? (
              <div className="notification-empty">
                <div className="empty-bell-icon">
                  <BellIcon size={32} />
                </div>
                <p>Bạn chưa có thông báo mới nào</p>
              </div>
            ) : (
              notifications.map((n) => (
                <div
                  key={n.id}
                  className="notification-item"
                  onClick={() => handleItemClick(n.targetPostId)}
                >
                  <div className="notification-avatar-col">
                    <Avatar
                      src={n.triggeredByUserAvatar}
                      name={n.triggeredByUserName}
                      size="sm"
                    />
                    <span className={`notification-type-badge badge-${n.type}`}>
                      {renderTypeIcon(n.type)}
                    </span>
                  </div>

                  <div className="notification-content-col">
                    <p className="notification-message">{n.message}</p>
                    <span className="notification-time">
                      {formatRelativeTime(n.createdAtUtc)}
                    </span>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
};
