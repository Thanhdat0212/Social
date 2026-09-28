import React, { useEffect, useState, useRef } from 'react';
import { useAuth } from '@/hooks/useAuth';
import { useTitle } from '@/hooks/useTitle';
import { profileApi } from '@/api/profileApi';
import { followApi } from '@/api/followApi';
import { getApiErrorMessage } from '@/utils/error';
import { LoadingScreen } from '@/components/feedback/LoadingScreen';
import type { ProfileDto, FollowStatsDto, UserFollowDto } from '@/types';

type FollowModalType = 'followers' | 'following' | null;

export const ProfilePage: React.FC = () => {
  useTitle('Hồ sơ cá nhân');
  const { user, setUser } = useAuth();
  const [profile, setProfile] = useState<ProfileDto | null>(null);
  const [displayName, setDisplayName] = useState('');
  const [bio, setBio] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [uploadingAvatar, setUploadingAvatar] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Follow Stats & Lists
  const [followStats, setFollowStats] = useState<FollowStatsDto | null>(null);
  const [followModal, setFollowModal] = useState<FollowModalType>(null);
  const [followUsersList, setFollowUsersList] = useState<UserFollowDto[]>([]);
  const [followListLoading, setFollowListLoading] = useState(false);

  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    let isMounted = true;

    const fetchAllData = async () => {
      try {
        setLoading(true);
        const profileData = await profileApi.getMyProfile();

        if (isMounted) {
          setProfile(profileData);
          setDisplayName(profileData.displayName);
          setBio(profileData.bio || '');

          // Fetch follow stats
          if (profileData.id) {
            const stats = await followApi.getFollowStats(profileData.id).catch(() => null);
            if (isMounted && stats) {
              setFollowStats(stats);
            }
          }
        }
      } catch (err) {
        if (isMounted) {
          setError(getApiErrorMessage(err));
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };

    fetchAllData();

    return () => {
      isMounted = false;
    };
  }, []);

  const handleOpenFollowModal = async (type: 'followers' | 'following') => {
    if (!profile?.id) return;
    setFollowModal(type);
    setFollowListLoading(true);

    try {
      const list =
        type === 'followers'
          ? await followApi.getFollowers(profile.id)
          : await followApi.getFollowing(profile.id);
      setFollowUsersList(list);
    } catch {
      setFollowUsersList([]);
    } finally {
      setFollowListLoading(false);
    }
  };

  const handleUpdateProfile = async (e: React.FormEvent) => {
    e.preventDefault();
    setMessage(null);
    setError(null);
    setSaving(true);

    try {
      const updated = await profileApi.updateProfile({ displayName, bio });
      setProfile(updated);
      if (user) {
        setUser({
          ...user,
          displayName: updated.displayName,
          bio: updated.bio,
        });
      }
      setMessage('Cập nhật thông tin hồ sơ thành công!');
    } catch (err) {
      setError(getApiErrorMessage(err));
    } finally {
      setSaving(false);
    }
  };

  const handleAvatarChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (file.size > 5 * 1024 * 1024) {
      setError('Kích thước ảnh đại diện không được vượt quá 5MB.');
      return;
    }

    setMessage(null);
    setError(null);
    setUploadingAvatar(true);

    try {
      const res = await profileApi.uploadAvatar(file);
      if (profile) {
        setProfile({ ...profile, avatarUrl: res.avatarUrl });
      }
      if (user) {
        setUser({ ...user, avatarUrl: res.avatarUrl });
      }
      setMessage('Tải ảnh đại diện thành công!');
    } catch (err) {
      setError(getApiErrorMessage(err));
    } finally {
      setUploadingAvatar(false);
    }
  };

  if (loading) {
    return <LoadingScreen message="Đang tải hồ sơ cá nhân..." />;
  }

  return (
    <div className="container profile-page">
      <div className="page-header">
        <h1>Hồ sơ cá nhân</h1>
        <p className="text-muted">Quản lý thông tin tài khoản, sở thích và mạng lưới quan hệ của bạn</p>
      </div>

      {message && <div className="alert alert-success">{message}</div>}
      {error && <div className="alert alert-error">{error}</div>}

      <div className="profile-layout">
        {/* Cột ảnh đại diện & Thống kê Follow */}
        <div className="profile-left-col">
          <div className="card profile-avatar-card text-center">
            <div className="avatar-wrapper">
              {profile?.avatarUrl ? (
                <img src={profile.avatarUrl} alt={profile.displayName} className="profile-avatar-img" />
              ) : (
                <div className="profile-avatar-placeholder">
                  {profile?.displayName ? profile.displayName.charAt(0).toUpperCase() : 'U'}
                </div>
              )}
              {uploadingAvatar && (
                <div className="avatar-loading-overlay">
                  <div className="spinner"></div>
                </div>
              )}
            </div>

            <input
              type="file"
              ref={fileInputRef}
              onChange={handleAvatarChange}
              accept="image/jpeg,image/png,image/webp"
              className="hidden"
            />

            <button
              type="button"
              onClick={() => fileInputRef.current?.click()}
              disabled={uploadingAvatar}
              className="btn btn-secondary btn-sm mt-4"
              id="upload-avatar-btn"
            >
              {uploadingAvatar ? 'Đang tải ảnh...' : 'Thay đổi ảnh đại diện'}
            </button>
            <p className="text-xs text-muted mt-2">Định dạng JPG, PNG, WebP (Tối đa 5MB)</p>

            {/* Follow Statistics Bar */}
            <div className="profile-follow-stats">
              <button
                type="button"
                className="follow-stat-btn"
                onClick={() => handleOpenFollowModal('followers')}
                id="profile-followers-btn"
              >
                <span className="stat-number">{followStats?.followersCount || 0}</span>
                <span className="stat-label">Người theo dõi</span>
              </button>

              <div className="stat-divider"></div>

              <button
                type="button"
                className="follow-stat-btn"
                onClick={() => handleOpenFollowModal('following')}
                id="profile-following-btn"
              >
                <span className="stat-number">{followStats?.followingCount || 0}</span>
                <span className="stat-label">Đang theo dõi</span>
              </button>
            </div>
          </div>
        </div>

        {/* Cột form chỉnh sửa */}
        <div className="card profile-details-card">
          <h3>Thông tin tài khoản</h3>

          <form onSubmit={handleUpdateProfile} className="profile-form mt-4">
            <div className="form-group">
              <label>Địa chỉ Email (chỉ đọc)</label>
              <input
                type="email"
                value={profile?.email || ''}
                disabled
                className="form-input disabled"
              />
              <span className="text-xs text-muted">
                Trạng thái: {profile?.emailConfirmed ? '✅ Đã xác minh' : '⚠️ Chưa xác minh'}
              </span>
            </div>

            <div className="form-group">
              <label htmlFor="profile-displayName">Tên hiển thị</label>
              <input
                id="profile-displayName"
                type="text"
                value={displayName}
                onChange={(e) => setDisplayName(e.target.value)}
                required
                className="form-input"
              />
            </div>

            <div className="form-group">
              <label htmlFor="profile-bio">Tiểu sử (Bio)</label>
              <textarea
                id="profile-bio"
                value={bio}
                onChange={(e) => setBio(e.target.value)}
                rows={4}
                placeholder="Giới thiệu đôi nét về bản thân..."
                className="form-input"
              />
            </div>

            <button
              type="submit"
              disabled={saving}
              className="btn btn-primary"
              id="save-profile-btn"
            >
              {saving ? 'Đang lưu thay đổi...' : 'Lưu thông tin hồ sơ'}
            </button>
          </form>
        </div>
      </div>

      {/* Modal xem danh sách Followers / Following */}
      {followModal && (
        <div className="modal-overlay" onClick={() => setFollowModal(null)}>
          <div className="modal-content follow-list-modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>{followModal === 'followers' ? '👥 Người theo dõi' : '👤 Đang theo dõi'}</h2>
              <button
                type="button"
                className="btn-close-modal"
                onClick={() => setFollowModal(null)}
              >
                ✕
              </button>
            </div>

            <div className="modal-body">
              {followListLoading ? (
                <div className="text-center py-4 text-muted">Đang tải danh sách...</div>
              ) : followUsersList.length === 0 ? (
                <div className="text-center py-4 text-muted">
                  {followModal === 'followers'
                    ? 'Chưa có ai theo dõi bạn.'
                    : 'Bạn chưa theo dõi người dùng nào.'}
                </div>
              ) : (
                <div className="follow-users-list">
                  {followUsersList.map((item) => (
                    <div key={item.id} className="follow-user-row">
                      <div className="avatar user-row-avatar">
                        {item.avatarUrl ? (
                          <img src={item.avatarUrl} alt={item.displayName} />
                        ) : (
                          <span>{item.displayName.charAt(0)}</span>
                        )}
                      </div>
                      <div className="user-row-info">
                        <div className="user-row-name">{item.displayName}</div>
                        {item.bio && <div className="user-row-bio">{item.bio}</div>}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
