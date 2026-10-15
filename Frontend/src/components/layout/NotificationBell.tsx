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

interface ExtraNotificationData {
  targetPostId?: string;
  commentId?: string;
  actorId?: string;
  actorName?: string;
  actorAvatar?: string;
}

export const NotificationBell: React.FC = () => {
  const navigate = useNavigate();
  const { notifications, markAllAsRead, addNotification } = useNotificationStore();
  const { openPostModal } = usePostModalStore();
  const [isOpen, setIsOpen] = useState(false);
  const [systemNotifications, setSystemNotifications] = useState<SystemNotificationDto[]>([]);
  const [systemUnreadCount, setSystemUnreadCount] = useState<number>(0);
  const menuRef = useRef<HTMLDivElement>(null);

  const [hasNewAlert, setHasNewAlert] = useState(false);

  // Tải danh sách thông báo và số lượng chưa đọc từ Backend
  const loadSystemNotifications = async () => {
    try {
      const [list, count] = await Promise.all([
        notificationApi.getMyNotifications(1, 30),
        notificationApi.getUnreadCount(),
      ]);
      setSystemNotifications(list);
      setSystemUnreadCount(count);
    } catch {
      // Fallback im lặng nếu offline
    }
  };

  // 1. Lắng nghe sự kiện ReceiveNotification từ SignalR Hub (Toast realtime + nhảy số badge ngay lập tức)
  useSignalR<UserNotificationEvent>('ReceiveNotification', (notification) => {
    if (notification) {
      addNotification(notification);
      setSystemUnreadCount((prev) => prev + 1);
      setHasNewAlert(true);
      setTimeout(() => setHasNewAlert(false), 1000);
      loadSystemNotifications();
    }
  });

  // 2. Lắng nghe sự kiện ReceivePersistentNotification từ SignalR Hub (Cập nhật tức thì vào danh sách)
  useSignalR<SystemNotificationDto>('ReceivePersistentNotification', (pNotification) => {
    if (pNotification) {
      setSystemNotifications((prev) => [
        pNotification,
        ...prev.filter((item) => item.id !== pNotification.id),
      ]);
      setSystemUnreadCount((prev) => prev + 1);
      setHasNewAlert(true);
      setTimeout(() => setHasNewAlert(false), 1000);
    }
  });

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
    setSystemNotifications((prev) => prev.map((item) => ({ ...item, isRead: true })));
    try {
      await notificationApi.markAllAsRead();
    } catch {
      // ignore
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

    let data: ExtraNotificationData = {};
    try {
      if (sn.dataJson) {
        data = JSON.parse(sn.dataJson);
      }
    } catch {
      // ignore
    }

    const t = (sn.type || '').toLowerCase();
    const title = (sn.title || '').toLowerCase();
    const body = (sn.body || '').toLowerCase();

    // 1. Mạng xã hội: Bài viết (Like, Comment, New Post)
    if (data.targetPostId) {
      openPostModal(data.targetPostId);
      return;
    }

    // 2. Mạng xã hội: Người theo dõi mới
    if (t === 'follow' && data.actorId) {
      navigate(`/profile/${data.actorId}`);
      return;
    }

    // 3. Sàn thương mại & Đơn hàng
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

  // Đếm các thông báo realtime trong store chưa được đồng bộ vào systemNotifications
  const localUnreadCount = notifications.filter(
    (n) => !systemNotifications.some((s) => s.id === n.id)
  ).length;

  const totalUnread = Math.max(
    systemUnreadCount,
    systemNotifications.filter((s) => !s.isRead).length
  ) + localUnreadCount;

  // Lọc thêm các notification realtime cục bộ nếu chưa có trong systemNotifications
  const mergedNotifications = [
    ...systemNotifications,
    ...notifications
      .filter((n) => !systemNotifications.some((s) => s.id === n.id))
      .map((n) => ({
        id: n.id,
        type: n.type,
        title: n.type === 'like' ? 'Lượt thích mới' : n.type === 'comment' ? 'Bình luận mới' : 'Thông báo',
        body: n.message,
        isRead: false,
        dataJson: JSON.stringify({
          targetPostId: n.targetPostId,
          actorId: n.triggeredByUserId,
          actorName: n.triggeredByUserName,
          actorAvatar: n.triggeredByUserAvatar,
        }),
        createdAtUtc: n.createdAtUtc,
      })),
  ];

  return (
    <div className="notification-bell-wrapper" ref={menuRef}>
      <button
        type="button"
        className={`notification-bell-btn ${isOpen ? 'active' : ''} ${hasNewAlert ? 'has-new-alert' : ''}`}
        onClick={handleToggle}
        aria-label="Thông báo"
        title="Thông báo hệ thống và tương tác"
      >
        <BellIcon size={19} />
        {totalUnread > 0 && (
          <span className="notification-badge" key={totalUnread}>
            {totalUnread > 99 ? '99+' : totalUnread}
          </span>
        )}
      </button>

      {isOpen && (
        <div className="notification-dropdown">
          <div className="notification-header">
            <h3 className="notification-title">Thông báo</h3>
            {mergedNotifications.length > 0 && (
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
            {mergedNotifications.length === 0 ? (
              <div className="notification-empty">
                <div className="empty-bell-icon">
                  <BellIcon size={32} />
                </div>
                <p>Bạn chưa có thông báo mới nào</p>
              </div>
            ) : (
              mergedNotifications.map((sn) => {
                let data: ExtraNotificationData = {};
                try {
                  if (sn.dataJson) {
                    data = JSON.parse(sn.dataJson);
                  }
                } catch {
                  // ignore
                }

                return (
                  <div
                    key={sn.id}
                    className={`notification-item ${!sn.isRead ? 'unread' : ''}`}
                    onClick={() => handleSystemNotificationClick(sn)}
                  >
                    <div className="notification-avatar-col">
                      {data.actorAvatar || data.actorName ? (
                        <div style={{ position: 'relative' }}>
                          <Avatar
                            src={data.actorAvatar}
                            name={data.actorName || 'User'}
                            size="sm"
                          />
                          <span className={`notification-type-badge badge-${sn.type.toLowerCase()}`}>
                            {renderTypeIcon(sn.type)}
                          </span>
                        </div>
                      ) : (
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
                      )}
                    </div>

                    <div className="notification-content-col">
                      <p className="notification-message" style={{ fontWeight: !sn.isRead ? 700 : 500 }}>
                        <span style={{ color: 'var(--accent-cyan)', display: 'block', fontSize: '0.8rem', marginBottom: '2px' }}>
                          {sn.title}
                        </span>
                        {sn.body}
                      </p>
                      <span className="notification-time">
                        {formatRelativeTime(sn.createdAtUtc)}
                      </span>
                    </div>
                  </div>
                );
              })
            )}
          </div>
        </div>
      )}
    </div>
  );
};
