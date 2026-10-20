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

      {reconciliation && (() => {
        const isBalanced = reconciliation.isBalanced ?? reconciliation.ledgerIntegrity?.isBalanced ?? true;
        const totalDebit = Number(reconciliation.totalDebit ?? reconciliation.ledgerIntegrity?.totalDebit ?? 0);
        const totalCredit = Number(reconciliation.totalCredit ?? reconciliation.ledgerIntegrity?.totalCredit ?? 0);
        const discrepancy = Number(reconciliation.discrepancy ?? reconciliation.ledgerIntegrity?.discrepancy ?? 0);
        const payoutsMatch = reconciliation.payoutsMatch ?? reconciliation.payoutAudit?.isMatched ?? true;
        const payoutPendingBalance = Number(reconciliation.payoutPendingBalance ?? reconciliation.payoutAudit?.ledgerPayoutPendingTotal ?? 0);
        const activePayoutsTotal = Number(reconciliation.activePayoutsTotal ?? reconciliation.payoutAudit?.activePayoutsTotal ?? 0);
        const totalShopsAudited = Number(reconciliation.totalShopsAudited ?? 0);
        const discrepantShops = reconciliation.discrepantShops || reconciliation.mismatchedShops || [];
        const discrepantShopCount = reconciliation.discrepantShopCount ?? discrepantShops.length;

        return (
          <div>
            {/* Metric Cards Grid */}
            <div className="admin-stats-grid">
              {/* 1. Sổ cái Balance */}
              <div
                className="admin-stat-card"
                style={{
                  borderTop: `3px solid ${isBalanced ? '#059669' : '#dc2626'}`,
                }}
              >
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <span className="admin-stat-label">Cân bằng Nợ / Có Sổ cái</span>
                  <span className={`admin-stat-icon ${isBalanced ? 'icon-green' : 'icon-amber'}`}>
                    {isBalanced ? (
                      <CheckCircleIcon size={20} />
                    ) : (
                      <AlertTriangleIcon size={20} />
                    )}
                  </span>
                </div>
                <div className={`admin-stat-value ${isBalanced ? 'val-green' : 'val-amber'}`} style={{ fontSize: '1.4rem' }}>
                  {isBalanced ? 'CÂN BẰNG 100%' : `LỆCH ${discrepancy.toLocaleString('vi-VN')} đ`}
                </div>
                <div className="admin-stat-sub">
                  Nợ: <strong style={{ color: '#0f172a' }}>{totalDebit.toLocaleString('vi-VN')} đ</strong> • Có: <strong style={{ color: '#0f172a' }}>{totalCredit.toLocaleString('vi-VN')} đ</strong>
                </div>
              </div>

              {/* 2. Quỹ Payouts */}
              <div
                className="admin-stat-card stat-blue"
              >
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <span className="admin-stat-label">Quỹ Lệnh Rút PayoutPending</span>
                  <span className="admin-stat-icon icon-blue">
                    {payoutsMatch ? (
                      <CheckCircleIcon size={20} />
                    ) : (
                      <AlertTriangleIcon size={20} />
                    )}
                  </span>
                </div>
                <div className="admin-stat-value val-blue" style={{ fontSize: '1.4rem' }}>
                  {payoutPendingBalance.toLocaleString('vi-VN')} đ
                </div>
                <div className="admin-stat-sub">
                  {payoutsMatch ? '✓ Khớp 100%' : '⚠ Lệch'} với tổng các lệnh rút đang mở ({activePayoutsTotal.toLocaleString('vi-VN')} đ)
                </div>
              </div>

              {/* 3. Gian hàng Audited */}
              <div
                className="admin-stat-card stat-purple"
              >
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <span className="admin-stat-label">Kiểm tra Ví Gian hàng</span>
                  <span className="admin-stat-icon icon-purple">
                    <StoreIcon size={20} />
                  </span>
                </div>
                <div className="admin-stat-value val-purple" style={{ fontSize: '1.4rem' }}>
                  {totalShopsAudited} gian hàng
                </div>
                <div className="admin-stat-sub">
                  {discrepantShopCount === 0 ? (
                    <span style={{ color: '#059669', fontWeight: 600 }}>✓ Toàn bộ cache số dư khớp hoàn hảo</span>
                  ) : (
                    <span style={{ color: '#dc2626', fontWeight: 600 }}>⚠ {discrepantShopCount} gian hàng bị lệch cache</span>
                  )}
                </div>
              </div>
            </div>

            {/* Discrepant Shops Table & Self-Healing Action */}
            {discrepantShops.length > 0 && (
              <div
                className="admin-table-card"
                style={{
                  padding: '1.5rem',
                  border: '1px solid #fecdd3',
                  marginBottom: '2rem',
                }}
              >
                <h4 style={{ color: '#be123c', margin: '0 0 1rem', fontWeight: 800 }}>
                  Phát hiện sai lệch Cache ({discrepantShops.length} gian hàng)
                </h4>
                <table className="admin-table">
                  <thead>
                    <tr>
                      <th>Gian hàng</th>
                      <th>Số dư Cache</th>
                      <th>Thực tính từ Sổ cái</th>
                      <th>Chênh lệch</th>
                      <th style={{ textAlign: 'right' }}>Khắc phục</th>
                    </tr>
                  </thead>
                  <tbody>
                    {discrepantShops.map((ds) => {
                      const cached = Number(ds.cachedAvailable ?? ds.cacheAvailable ?? 0);
                      const computed = Number(ds.computedAvailable ?? ds.realAvailable ?? 0);
                      const diff = Number(ds.difference ?? Math.abs(cached - computed));
                      return (
                        <tr key={ds.shopId}>
                          <td style={{ fontWeight: 700, color: '#0f172a' }}>{ds.shopName}</td>
                          <td style={{ color: '#334155' }}>{cached.toLocaleString('vi-VN')} đ</td>
                          <td style={{ fontWeight: 700, color: '#0f172a' }}>{computed.toLocaleString('vi-VN')} đ</td>
                          <td style={{ color: '#dc2626', fontWeight: 700 }}>{diff.toLocaleString('vi-VN')} đ</td>
                          <td style={{ textAlign: 'right' }}>
                            <button
                              type="button"
                              className="btn btn-primary btn-sm"
                              style={{ fontWeight: 700 }}
                              onClick={() => handleFixBalance(ds.shopId)}
                            >
                              Sửa lệch (Self-healing)
                            </button>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        );
      })()}
    </div>
  );
};
