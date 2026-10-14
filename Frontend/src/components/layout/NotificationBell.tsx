import React, { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useNotificationStore, usePostModalStore } from '@/store';
import { useSignalR } from '@/hooks/useSignalR';
import { notificationApi, type SystemNotificationDto } from '@/api/notificationApi';
import type { UserNotificationEvent } from '@/types';
import { formatRelativeTime } from '@/utils/date';
import {
  BellIcon,
  HeartIcon,
  CommentIcon,
  UserIcon,
  SparklesIcon,
  StoreIcon,
  PackageIcon,
  ShieldIcon,
  Avatar,
} from '@/components/common';

export const NotificationBell: React.FC = () => {
  const navigate = useNavigate();
  const { notifications, unreadCount, addNotification, markAllAsRead } = useNotificationStore();
  const { openPostModal } = usePostModalStore();
  const [isOpen, setIsOpen] = useState(false);
  const [systemNotifications, setSystemNotifications] = useState<SystemNotificationDto[]>([]);
  const [systemUnreadCount, setSystemUnreadCount] = useState<number>(0);
  const menuRef = useRef<HTMLDivElement>(null);

  // Lắng nghe sự kiện ReceiveNotification từ SignalR Hub
  useSignalR<UserNotificationEvent>('ReceiveNotification', (notification) => {
    if (notification) {
      addNotification(notification);
    }
  });

  // Tải thông báo hệ thống và chợ từ Backend
  const loadSystemNotifications = async () => {
    try {
      const [list, count] = await Promise.all([
        notificationApi.getMyNotifications(1, 20),
        notificationApi.getUnreadCount(),
      ]);
      setSystemNotifications(list);
      setSystemUnreadCount(count);
    } catch {
      // Fallback im lặng nếu offline
    }
  };

  useEffect(() => {
    loadSystemNotifications();
  }, []);

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
    if (!isOpen) {
      loadSystemNotifications();
    }
    setIsOpen((prev) => !prev);
  };

  const handleMarkAllRead = async () => {
    markAllAsRead();
    setSystemUnreadCount(0);
    try {
      await notificationApi.markAllAsRead();
    } catch {
      // ignore
    }
  };

  const handleItemClick = (targetPostId?: string) => {
    setIsOpen(false);
    if (targetPostId) {
      openPostModal(targetPostId);
    }
  };

  const handleSystemNotificationClick = (sn: SystemNotificationDto) => {
    if (!sn.isRead) {
      notificationApi.markAsRead(sn.id).catch(() => {});
      setSystemNotifications((prev) =>
        prev.map((item) => (item.id === sn.id ? { ...item, isRead: true } : item))
      );
      setSystemUnreadCount((prev) => Math.max(0, prev - 1));
    }
    setIsOpen(false);

    const t = (sn.type || '').toLowerCase();
    const title = (sn.title || '').toLowerCase();
    const body = (sn.body || '').toLowerCase();

    if (t.includes('order') || title.includes('đơn hàng') || body.includes('đơn hàng')) {
      if (title.includes('bán') || body.includes('người mua') || body.includes('khách')) {
        navigate('/seller/orders');
      } else {
        navigate('/orders');
      }
    } else if (t.includes('complaint') || title.includes('khiếu nại') || body.includes('khiếu nại')) {
      navigate('/seller/complaints');
    } else if (t.includes('payout') || title.includes('rút tiền') || body.includes('rút tiền')) {
      navigate('/seller/finance');
    } else if (t.includes('shop') || title.includes('gian hàng') || body.includes('gian hàng')) {
      navigate('/seller/overview');
    } else if (t.includes('product') || title.includes('sản phẩm') || body.includes('sản phẩm')) {
      navigate('/seller/products');
    }
  };

  const totalUnread = unreadCount + systemUnreadCount;

  const renderTypeIcon = (type: string) => {
    switch (type.toLowerCase()) {
      case 'like':
        return <HeartIcon size={12} filled className="type-icon-like" />;
      case 'comment':
        return <CommentIcon size={12} className="type-icon-comment" />;
      case 'follow':
        return <UserIcon size={12} className="type-icon-follow" />;
      case 'new_post':
        return <SparklesIcon size={12} className="type-icon-post" />;
      case 'shop':
      case 'shop_approved':
      case 'shop_rejected':
      case 'shop_sanctioned':
        return <StoreIcon size={12} />;
      case 'order':
      case 'order_paid':
      case 'order_completed':
        return <PackageIcon size={12} />;
      case 'admin':
      case 'security':
        return <ShieldIcon size={12} />;
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
        title="Thông báo hệ thống và tương tác"
      >
        <BellIcon size={19} />
        {totalUnread > 0 && (
          <span className="notification-badge">
            {totalUnread > 99 ? '99+' : totalUnread}
          </span>
        )}
      </button>

      {isOpen && (
        <div className="notification-dropdown">
          <div className="notification-header">
            <h3 className="notification-title">Thông báo</h3>
            {(notifications.length > 0 || systemNotifications.length > 0) && (
              <button
                type="button"
                className="notification-clear-btn"
                onClick={handleMarkAllRead}
              >
                Đã đọc tất cả
              </button>
            )}
          </div>

          <div className="notification-list">
            {notifications.length === 0 && systemNotifications.length === 0 ? (
              <div className="notification-empty">
                <div className="empty-bell-icon">
                  <BellIcon size={32} />
                </div>
                <p>Bạn chưa có thông báo mới nào</p>
              </div>
            ) : (
              <>
                {/* Thông báo Hệ thống & Chợ số */}
                {systemNotifications.map((sn) => (
                  <div
                    key={sn.id}
                    className={`notification-item ${!sn.isRead ? 'unread' : ''}`}
                    onClick={() => handleSystemNotificationClick(sn)}
                  >
                    <div className="notification-avatar-col">
                      <div
                        style={{
                          width: '36px',
                          height: '36px',
                          borderRadius: '50%',
                          background: 'rgba(99, 102, 241, 0.15)',
                          color: 'var(--accent-primary)',
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                        }}
                      >
                        {renderTypeIcon(sn.type)}
                      </div>
                    </div>

                    <div className="notification-content-col">
                      <p className="notification-message" style={{ fontWeight: !sn.isRead ? 700 : 500 }}>
                        <span style={{ color: 'var(--accent-cyan)', display: 'block', fontSize: '0.8rem' }}>
                          {sn.title}
                        </span>
                        {sn.body}
                      </p>
                      <span className="notification-time">
                        {formatRelativeTime(sn.createdAtUtc)}
                      </span>
                    </div>
                  </div>
                ))}

                {/* Thông báo Realtime Mạng xã hội */}
                {notifications.map((n) => (
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
                ))}
              </>
            )}
          </div>
        </div>
      )}
    </div>
  );
};
