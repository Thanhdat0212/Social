import React, { useEffect, useState, useRef } from 'react';
import { useAuth } from '@/hooks/useAuth';
import { useTitle } from '@/hooks/useTitle';
import { profileApi } from '@/api/profileApi';
import { followApi } from '@/api/followApi';
import { getApiErrorMessage } from '@/utils/error';
import { LoadingScreen } from '@/components/feedback/LoadingScreen';
import {
  Avatar,
  Button,
  Alert,
  CloseIcon,
  ImageIcon,
  UsersIcon,
  UserIcon,
} from '@/components/common';
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
    <div className="container profile-page-container">
      {/* Profile Header Card with Cover */}
      <section className="profile-hero-card">
        <div className="profile-cover-banner" />

        <div className="profile-hero-content">
          <div className="profile-avatar-stack">
            <Avatar
              src={profile?.avatarUrl}
              name={profile?.displayName}
              size="xl"
              className="profile-hero-avatar"
            />
            {uploadingAvatar && (
              <div className="avatar-loading-overlay">
                <span className="spinner-inline" />
              </div>
            )}
            <input
              type="file"
              ref={fileInputRef}
              onChange={handleAvatarChange}
              accept="image/jpeg,image/png,image/webp"
              className="hidden"
            />
            <button
              type="button"
              className="btn-change-avatar"
              onClick={() => fileInputRef.current?.click()}
              disabled={uploadingAvatar}
              title="Thay đổi ảnh đại diện"
              aria-label="Thay đổi ảnh đại diện"
            >
              <ImageIcon size={15} />
            </button>
          </div>

          <div className="profile-identity-col">
            <div className="profile-names-row">
              <h1 className="profile-fullname">{profile?.displayName || 'Thành viên'}</h1>
              {profile?.emailConfirmed && (
                <span className="badge-verified" title="Tài khoản đã xác minh email">
                  ✓ Đã xác minh
                </span>
              )}
            </div>
            <p className="profile-email-meta">{profile?.email}</p>
            {profile?.bio && <p className="profile-bio-text">{profile.bio}</p>}

            {/* Follow Stats Chips */}
            <div className="profile-stats-row">
              <button
                type="button"
                className="profile-stat-chip"
                onClick={() => handleOpenFollowModal('followers')}
                id="profile-followers-btn"
              >
                <UsersIcon size={16} />
                <span className="stat-count">{followStats?.followersCount || 0}</span>
                <span className="stat-name">Người theo dõi</span>
              </button>

              <button
                type="button"
                className="profile-stat-chip"
                onClick={() => handleOpenFollowModal('following')}
                id="profile-following-btn"
              >
                <UserIcon size={16} />
                <span className="stat-count">{followStats?.followingCount || 0}</span>
                <span className="stat-name">Đang theo dõi</span>
              </button>
            </div>
          </div>
        </div>
      </section>

      {/* Profile Edit Section */}
      <section className="profile-edit-section">
        <div className="edit-card">
          <div className="edit-card-header">
            <h2>Chỉnh sửa thông tin</h2>
            <p>Cập nhật tên hiển thị và tiểu sử cá nhân của bạn</p>
          </div>

          {message && <Alert type="success" message={message} className="mb-4" />}
          {error && <Alert type="error" message={error} className="mb-4" />}

          <form onSubmit={handleUpdateProfile} className="profile-form">
            <div className="form-group">
              <label>Địa chỉ Email (chỉ đọc)</label>
              <input
                type="email"
                value={profile?.email || ''}
                disabled
                className="form-control disabled-input"
              />
            </div>

            <div className="form-group">
              <label htmlFor="profile-displayName">Tên hiển thị *</label>
              <input
                id="profile-displayName"
                type="text"
                value={displayName}
                onChange={(e) => setDisplayName(e.target.value)}
                required
                className="form-control"
                placeholder="Nhập tên hiển thị của bạn"
              />
            </div>

            <div className="form-group">
              <div className="form-label-row">
                <label htmlFor="profile-bio">Tiểu sử (Bio)</label>
                <span className="char-hint">{bio.length}/500</span>
              </div>
              <textarea
                id="profile-bio"
                value={bio}
                onChange={(e) => setBio(e.target.value)}
                rows={3}
                placeholder="Giới thiệu ngắn về chuyên môn, sở thích hoặc dự án..."
                className="form-control"
                maxLength={500}
              />
            </div>

            <div className="profile-form-footer">
              <Button
                type="submit"
                variant="primary"
                size="md"
                disabled={saving}
                loading={saving}
                id="save-profile-btn"
              >
                Lưu thông tin hồ sơ
              </Button>
            </div>
          </form>
        </div>
      </section>

      {/* Modal danh sách Followers / Following */}
      {followModal && (
        <div className="modal-overlay" onClick={() => setFollowModal(null)} role="dialog">
          <div className="modal-content follow-list-modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>{followModal === 'followers' ? 'Người theo dõi' : 'Đang theo dõi'}</h2>
              <button
                type="button"
                className="btn-close-modal"
                onClick={() => setFollowModal(null)}
                aria-label="Đóng"
              >
                <CloseIcon size={16} />
              </button>
            </div>

            <div className="modal-body">
              {followListLoading ? (
                <div className="modal-loading-state">
                  <span className="spinner-inline" />
                  <span>Đang tải danh sách...</span>
                </div>
              ) : followUsersList.length === 0 ? (
                <div className="modal-empty-state">
                  <p>
                    {followModal === 'followers'
                      ? 'Chưa có người dùng nào theo dõi bạn.'
                      : 'Bạn chưa theo dõi người dùng nào.'}
                  </p>
                </div>
              ) : (
                <div className="follow-users-stream">
                  {followUsersList.map((item) => (
                    <div key={item.id} className="follow-user-item">
                      <Avatar
                        src={item.avatarUrl}
                        name={item.displayName}
                        size="md"
                      />
                      <div className="follow-user-details">
                        <span className="follow-user-name">{item.displayName}</span>
                        {item.bio && <p className="follow-user-bio">{item.bio}</p>}
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
