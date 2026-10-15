import React, { useEffect, useState } from 'react';
import { adminApi } from '@/api/adminApi';
import type { ReconciliationSummary } from '@/types/admin';
import {
  RefreshIcon,
  CheckCircleIcon,
  AlertTriangleIcon,
  StoreIcon,
} from '@/components/common/Icons';

interface AdminAuditTabProps {
  showFlash: (text: string, type?: 'success' | 'error') => void;
}

export const AdminAuditTab: React.FC<AdminAuditTabProps> = ({ showFlash }) => {
  const [reconciliation, setReconciliation] = useState<ReconciliationSummary | null>(null);
  const [loading, setLoading] = useState(false);
  const [reconciling, setReconciling] = useState(false);

  const loadAuditData = async () => {
    setLoading(true);
    try {
      const res = await adminApi.getReconciliation();
      setReconciliation(res);
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Có lỗi xảy ra khi tải báo cáo đối soát.', 'error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadAuditData();
  }, []);

  const handleRunReconciliation = async () => {
    setReconciling(true);
    try {
      const res = await adminApi.getReconciliation();
      setReconciliation(res);
      showFlash('Đã hoàn tất tiến trình kiểm toán đối soát sổ cái!');
    } catch {
      showFlash('Lỗi khi chạy đối soát.', 'error');
    } finally {
      setReconciling(false);
    }
  };

  const handleFixBalance = async (shopId: string) => {
    try {
      await adminApi.fixShopBalance(shopId);
      showFlash('Đã đồng bộ và tự động sửa lệch số dư thành công từ Sổ cái kép!');
      const updated = await adminApi.getReconciliation();
      setReconciliation(updated);
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Lỗi khi sửa lệch số dư.', 'error');
    }
  };

  if (loading && !reconciliation) {
    return (
      <div style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-muted)' }}>
        Đang tải bảng kiểm toán sổ cái...
      </div>
    );
  }

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem', flexWrap: 'wrap', gap: '1rem' }}>
        <div>
          <h3 style={{ fontSize: '1.2rem', fontWeight: 800, margin: '0 0 0.25rem' }}>
            Bảng Kiểm Toán Sổ Cái Kép Toàn Hệ Thống
          </h3>
          <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem', margin: 0 }}>
            Kiểm chứng bất biến toán học: Tổng Nợ (`Debit`) = Tổng Có (`Credit`), tự động phát hiện và khắc phục lệch số dư.
          </p>
        </div>
        <button
          type="button"
          className="btn btn-primary"
          disabled={reconciling}
          onClick={handleRunReconciliation}
          style={{ display: 'inline-flex', alignItems: 'center', gap: '6px' }}
        >
          <RefreshIcon size={16} /> Chạy đối soát ngay
        </button>
      </div>

      {reconciliation && (
        <div>
          {/* Metric Cards Grid */}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))', gap: '1.25rem', marginBottom: '2rem' }}>
            {/* 1. Sổ cái Balance */}
            <div
              className="card-glass"
              style={{
                padding: '1.5rem',
                borderRadius: '16px',
                border: `1px solid ${reconciliation.isBalanced ? 'rgba(16, 185, 129, 0.4)' : 'rgba(244, 63, 94, 0.5)'}`,
                background: reconciliation.isBalanced
                  ? 'linear-gradient(135deg, rgba(16, 185, 129, 0.08) 0%, transparent 100%)'
                  : 'linear-gradient(135deg, rgba(244, 63, 94, 0.08) 0%, transparent 100%)',
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
                <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Cân bằng Nợ / Có Sổ cái</span>
                {reconciliation.isBalanced ? (
                  <CheckCircleIcon size={20} className="text-success" />
                ) : (
                  <AlertTriangleIcon size={20} className="text-danger" />
                )}
              </div>
              <div style={{ fontSize: '1.35rem', fontWeight: 800, color: reconciliation.isBalanced ? 'var(--accent-emerald)' : 'var(--accent-rose)' }}>
                {reconciliation.isBalanced ? 'CÂN BẰNG 100%' : `LỆCH ${reconciliation.discrepancy.toLocaleString('vi-VN')} đ`}
              </div>
              <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '0.5rem' }}>
                Nợ: {reconciliation.totalDebit.toLocaleString('vi-VN')} đ • Có: {reconciliation.totalCredit.toLocaleString('vi-VN')} đ
              </div>
            </div>

            {/* 2. Quỹ Payouts */}
            <div
              className="card-glass"
              style={{
                padding: '1.5rem',
                borderRadius: '16px',
                border: `1px solid ${reconciliation.payoutsMatch ? 'rgba(16, 185, 129, 0.4)' : 'rgba(244, 63, 94, 0.5)'}`,
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
                <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Quỹ Lệnh Rút PayoutPending</span>
                {reconciliation.payoutsMatch ? (
                  <CheckCircleIcon size={20} className="text-success" />
                ) : (
                  <AlertTriangleIcon size={20} className="text-danger" />
                )}
              </div>
              <div style={{ fontSize: '1.35rem', fontWeight: 800, color: '#fff' }}>
                {reconciliation.payoutPendingBalance.toLocaleString('vi-VN')} đ
              </div>
              <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '0.5rem' }}>
                Khớp 100% với tổng các lệnh rút đang mở ({reconciliation.activePayoutsTotal.toLocaleString('vi-VN')} đ)
              </div>
            </div>

            {/* 3. Gian hàng Audited */}
            <div
              className="card-glass"
              style={{
                padding: '1.5rem',
                borderRadius: '16px',
                border: '1px solid var(--border-color)',
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
                <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Kiểm tra Ví Gian hàng</span>
                <StoreIcon size={20} style={{ color: 'var(--accent-cyan)' }} />
              </div>
              <div style={{ fontSize: '1.35rem', fontWeight: 800, color: '#fff' }}>
                {reconciliation.totalShopsAudited} gian hàng
              </div>
              <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '0.5rem' }}>
                {reconciliation.discrepantShopCount === 0 ? (
                  <span style={{ color: 'var(--accent-emerald)' }}>Toàn bộ cache số dư khớp hoàn hảo</span>
                ) : (
                  <span style={{ color: 'var(--accent-rose)' }}>{reconciliation.discrepantShopCount} gian hàng bị lệch cache</span>
                )}
              </div>
            </div>
          </div>

          {/* Discrepant Shops Table & Self-Healing Action */}
          {reconciliation.discrepantShops && reconciliation.discrepantShops.length > 0 && (
            <div
              className="card-glass"
              style={{
                borderRadius: '16px',
                padding: '1.5rem',
                border: '1px solid rgba(244, 63, 94, 0.4)',
                background: 'rgba(244, 63, 94, 0.04)',
              }}
            >
              <h4 style={{ color: 'var(--accent-rose)', margin: '0 0 1rem', fontWeight: 700 }}>
                Phát hiện sai lệch Cache ({reconciliation.discrepantShops.length} gian hàng)
              </h4>
              <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.9rem' }}>
                <thead>
                  <tr style={{ borderBottom: '1px solid var(--border-color)' }}>
                    <th style={{ padding: '0.75rem' }}>Gian hàng</th>
                    <th style={{ padding: '0.75rem' }}>Số dư Cache</th>
                    <th style={{ padding: '0.75rem' }}>Thực tính từ Sổ cái</th>
                    <th style={{ padding: '0.75rem' }}>Chênh lệch</th>
                    <th style={{ padding: '0.75rem', textAlign: 'right' }}>Khắc phục</th>
                  </tr>
                </thead>
                <tbody>
                  {reconciliation.discrepantShops.map((ds) => (
                    <tr key={ds.shopId} style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                      <td style={{ padding: '0.75rem' }}>{ds.shopName}</td>
                      <td style={{ padding: '0.75rem' }}>{ds.cachedAvailable.toLocaleString('vi-VN')} đ</td>
                      <td style={{ padding: '0.75rem', fontWeight: 700 }}>{ds.computedAvailable.toLocaleString('vi-VN')} đ</td>
                      <td style={{ padding: '0.75rem', color: 'var(--accent-rose)' }}>{ds.difference.toLocaleString('vi-VN')} đ</td>
                      <td style={{ padding: '0.75rem', textAlign: 'right' }}>
                        <button
                          type="button"
                          className="btn btn-primary btn-sm"
                          onClick={() => handleFixBalance(ds.shopId)}
                        >
                          Sửa lệch (Self-healing)
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
