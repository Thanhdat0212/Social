import React, { useState, useEffect } from 'react';
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

  useEffect(() => {
    if (!isOpen) return;
    document.body.classList.add('modal-open');
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.body.classList.remove('modal-open');
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [isOpen, onClose]);

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
      className="modal-overlay"
      onClick={onClose}
      role="dialog"
      aria-modal="true"
    >
      <div
        className="modal-content"
        style={{
          maxWidth: '500px',
          width: '100%',
          padding: '1.75rem',
        }}
        onClick={(e) => e.stopPropagation()}
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
