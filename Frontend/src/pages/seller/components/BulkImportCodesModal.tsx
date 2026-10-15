import React, { useState } from 'react';
import { marketplaceApi } from '@/api/marketplaceApi';
import { Button, Textarea, Alert } from '@/components/common';

interface BulkImportCodesModalProps {
  variantId: string | null;
  onClose: () => void;
  onSuccess: () => void;
}

export const BulkImportCodesModal: React.FC<BulkImportCodesModalProps> = ({
  variantId,
  onClose,
  onSuccess,
}) => {
  const [bulkCodesText, setBulkCodesText] = useState('');
  const [loading, setLoading] = useState(false);
  const [successMsg, setSuccessMsg] = useState('');

  if (!variantId) return null;

  const handleBulkImport = async () => {
    if (!bulkCodesText.trim()) return;
    const lines = bulkCodesText.split('\n').map((l) => l.trim()).filter((l) => l.length > 0);
    if (lines.length === 0) return;

    setLoading(true);
    setSuccessMsg('');
    try {
      const res = await marketplaceApi.addDigitalCodesBulk(variantId, lines);
      setSuccessMsg(`Đã nạp thành công ${res.importedCount || lines.length} mã vào kho số an toàn!`);
      setBulkCodesText('');
      onSuccess();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi khi nạp mã kho số.');
    } finally {
      setLoading(false);
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
          maxWidth: '520px',
          width: '100%',
          padding: '1.75rem',
        }}
      >
        <h3 style={{ fontSize: '1.15rem', fontWeight: 800, color: 'var(--text-main)', marginBottom: '0.5rem' }}>
          Nạp mã kho số hàng loạt
        </h3>
        <p style={{ fontSize: '0.82rem', color: 'var(--text-muted)', marginBottom: '1rem', lineHeight: '1.5' }}>
          Nhập danh sách mã key hoặc tài khoản. Mỗi mã trên một dòng riêng biệt. Hệ thống mã hóa AES-256-GCM tự động.
        </p>

        {successMsg && (
          <Alert type="success" message={successMsg} className="mb-3" />
        )}

        <Textarea
          rows={6}
          placeholder={`KEY-XXXX-1111\nKEY-YYYY-2222\nuser@example.com|password123`}
          value={bulkCodesText}
          onChange={(e) => setBulkCodesText(e.target.value)}
          style={{ fontFamily: 'monospace', fontSize: '0.85rem' }}
        />

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.25rem' }}>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={onClose}
            disabled={loading}
          >
            Đóng
          </Button>
          <Button
            type="button"
            variant="primary"
            size="sm"
            onClick={handleBulkImport}
            loading={loading}
            disabled={loading || !bulkCodesText.trim()}
          >
            Xác nhận nạp kho
          </Button>
        </div>
      </div>
    </div>
  );
};
