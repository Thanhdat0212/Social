import React, { useState } from 'react';
import { marketplaceApi } from '@/api/marketplaceApi';
import { FormField, Input, Textarea, Button } from '@/components/common';

interface ShopSettingsModalProps {
  isOpen: boolean;
  shop: any;
  onClose: () => void;
  onSuccess: () => void;
}

export const ShopSettingsModal: React.FC<ShopSettingsModalProps> = ({
  isOpen,
  shop,
  onClose,
  onSuccess,
}) => {
  const [settingsForm, setSettingsForm] = useState({
    name: shop?.name || '',
    contactEmail: shop?.contactEmail || '',
    description: shop?.description || '',
  });
  const [settingsSaving, setSettingsSaving] = useState(false);

  if (!isOpen) return null;

  const handleSaveShopSettings = async (e: React.FormEvent) => {
    e.preventDefault();
    setSettingsSaving(true);
    try {
      await marketplaceApi.updateMyShop(settingsForm);
      alert('Đã cập nhật thông tin gian hàng!');
      onClose();
      onSuccess();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi khi cập nhật gian hàng.');
    } finally {
      setSettingsSaving(false);
    }
  };

  return (
    <div
      style={{
        position: 'fixed',
        inset: 0,
        background: 'rgba(0, 0, 0, 0.75)',
        backdropFilter: 'blur(6px)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        zIndex: 1000,
        padding: '1rem',
      }}
    >
      <div
        style={{
          background: 'var(--bg-card)',
          border: '1px solid var(--border-color)',
          borderRadius: '16px',
          maxWidth: '500px',
          width: '100%',
          padding: '1.75rem',
        }}
      >
        <h3 style={{ fontSize: '1.2rem', fontWeight: 800, color: 'var(--text-main)', marginBottom: '1rem' }}>
          Cài đặt thông tin gian hàng
        </h3>
        <form onSubmit={handleSaveShopSettings}>
          <FormField label="Tên gian hàng" required>
            <Input
              value={settingsForm.name}
              onChange={(e) => setSettingsForm({ ...settingsForm, name: e.target.value })}
              required
            />
          </FormField>

          <FormField label="Email nhận thông báo" required>
            <Input
              type="email"
              value={settingsForm.contactEmail}
              onChange={(e) => setSettingsForm({ ...settingsForm, contactEmail: e.target.value })}
              required
            />
          </FormField>

          <FormField label="Mô tả gian hàng">
            <Textarea
              rows={3}
              value={settingsForm.description}
              onChange={(e) => setSettingsForm({ ...settingsForm, description: e.target.value })}
            />
          </FormField>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.25rem' }}>
            <Button variant="ghost" size="sm" onClick={onClose} disabled={settingsSaving}>
              Hủy
            </Button>
            <Button variant="primary" size="sm" type="submit" loading={settingsSaving} disabled={settingsSaving}>
              Lưu thay đổi
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
