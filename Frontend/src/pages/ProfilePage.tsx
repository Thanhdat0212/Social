import React, { useEffect, useState, useRef } from 'react';
import { useAuthStore } from '@/store/authStore';
import { profileApi } from '@/api/profileApi';
import { getApiErrorMessage } from '@/api/client';
import type { ProfileDto } from '@/types/profile';

export const ProfilePage: React.FC = () => {
  const { user, setUser } = useAuthStore();
  const [profile, setProfile] = useState<ProfileDto | null>(null);
  const [displayName, setDisplayName] = useState('');
  const [bio, setBio] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [uploadingAvatar, setUploadingAvatar] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    const fetchProfile = async () => {
      try {
        setLoading(true);
        const data = await profileApi.getMyProfile();
        setProfile(data);
        setDisplayName(data.displayName);
        setBio(data.bio || '');
      } catch (err) {
        setError(getApiErrorMessage(err));
      } finally {
        setLoading(false);
      }
    };

    fetchProfile();
  }, []);

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

    // Kiểm tra định dạng và dung lượng tối đa 5MB
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
    return (
      <div className="container py-12 flex-center">
        <div className="spinner"></div>
        <p className="loading-text">Đang tải hồ sơ cá nhân...</p>
      </div>
    );
  }

  return (
    <div className="container profile-page">
      <div className="page-header">
        <h1>Hồ sơ cá nhân</h1>
        <p className="text-muted">Quản lý thông tin tài khoản và hình ảnh đại diện của bạn</p>
      </div>

      {message && <div className="alert alert-success">{message}</div>}
      {error && <div className="alert alert-error">{error}</div>}

      <div className="profile-layout">
        {/* Cột ảnh đại diện */}
        <div className="card profile-avatar-card text-center">
          <div className="avatar-wrapper">
            {profile?.avatarUrl ? (
              <img src={profile.avatarUrl} alt={profile.displayName} className="profile-avatar-img" />
            ) : (
              <div className="profile-avatar-placeholder">
                {profile?.displayName ? profile.displayName.charAt(0).toUpperCase() : 'U'}
              </div>
            )}
            {uploadingAvatar && <div className="avatar-loading-overlay"><div className="spinner"></div></div>}
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
    </div>
  );
};
