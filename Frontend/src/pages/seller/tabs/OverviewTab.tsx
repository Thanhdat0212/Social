import React from 'react';
import type { ShopBalanceDto, ShopPerformanceDto, SellerOrderDto } from '@/types/marketplace';
import { Button, Badge, StoreIcon } from '@/components/common';

interface OverviewTabProps {
  balance: ShopBalanceDto | null;
  performance: ShopPerformanceDto | null;
  shopOrders: SellerOrderDto[];
  ordersTotalCount: number;
  onNavigateTab: (tab: 'finance' | 'products' | 'orders' | 'complaints') => void;
}

export const OverviewTab: React.FC<OverviewTabProps> = ({
  balance,
  performance,
  shopOrders,
  ordersTotalCount,
  onNavigateTab,
}) => {
  return (
    <div>
      {/* Hàng 4 thẻ chỉ số tài chính & kinh doanh */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(230px, 1fr))', gap: '1rem', marginBottom: '1.5rem' }}>
        <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '14px', padding: '1.25rem' }}>
          <div style={{ fontSize: '0.78rem', color: 'var(--text-dim)', textTransform: 'uppercase', fontWeight: 600 }}>Tổng doanh thu tích lũy</div>
          <div style={{ fontSize: '1.55rem', fontWeight: 800, color: 'var(--text-main)', marginTop: '0.35rem' }}>
            {(balance?.lifetimeGrossRevenue || 0).toLocaleString('vi-VN')} đ
          </div>
          <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '0.25rem' }}>
            Hoa hồng sàn đã trả: {(balance?.lifetimeCommissionPaid || 0).toLocaleString('vi-VN')} đ
          </div>
        </div>

        <div style={{ background: 'var(--bg-card)', border: '1px solid rgba(245, 158, 11, 0.25)', borderRadius: '14px', padding: '1.25rem', backgroundClip: 'padding-box' }}>
          <div style={{ fontSize: '0.78rem', color: '#f59e0b', textTransform: 'uppercase', fontWeight: 600 }}>Tiền tạm giữ (Bảo hành 48h)</div>
          <div style={{ fontSize: '1.55rem', fontWeight: 800, color: '#f59e0b', marginTop: '0.35rem' }}>
            {(balance?.pendingBalance || 0).toLocaleString('vi-VN')} đ
          </div>
          <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '0.25rem' }}>
            Tự động chuyển vào số dư khả dụng sau 48h
          </div>
        </div>

        <div style={{ background: 'var(--bg-card)', border: '1px solid rgba(16, 185, 129, 0.25)', borderRadius: '14px', padding: '1.25rem' }}>
          <div style={{ fontSize: '0.78rem', color: '#10b981', textTransform: 'uppercase', fontWeight: 600 }}>Số dư khả dụng (Rút tiền)</div>
          <div style={{ fontSize: '1.55rem', fontWeight: 800, color: '#10b981', marginTop: '0.35rem' }}>
            {(balance?.availableBalance || 0).toLocaleString('vi-VN')} đ
          </div>
          <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '0.25rem' }}>
            Đang chờ chuyển khoản: {(balance?.payoutPendingBalance || 0).toLocaleString('vi-VN')} đ
          </div>
        </div>

        <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '14px', padding: '1.25rem' }}>
          <div style={{ fontSize: '0.78rem', color: 'var(--text-dim)', textTransform: 'uppercase', fontWeight: 600 }}>Đơn hàng đã bán</div>
          <div style={{ fontSize: '1.55rem', fontWeight: 800, color: 'var(--accent-primary)', marginTop: '0.35rem' }}>
            {ordersTotalCount || performance?.totalOrders || shopOrders.length} đơn
          </div>
          <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '0.25rem' }}>
            {performance?.completedOrders || 0} đơn đã hoàn tất • Tỷ lệ lỗi: {performance?.defectRate || 0}%
          </div>
        </div>
      </div>

      {/* Khối Đơn hàng mới bán gần đây trên màn hình Tổng quan */}
      <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '16px', padding: '1.5rem', marginBottom: '1.5rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.2rem', flexWrap: 'wrap', gap: '0.5rem' }}>
          <div>
            <h3 style={{ fontSize: '1.1rem', fontWeight: 700, color: 'var(--text-main)', margin: 0 }}>
              Đơn hàng mới bán gần đây
            </h3>
            <p style={{ margin: '0.2rem 0 0', fontSize: '0.8rem', color: 'var(--text-dim)' }}>
              Theo dõi các giao dịch bán hàng số vừa phát sinh của gian hàng
            </p>
          </div>
          <Button
            variant="secondary"
            size="sm"
            onClick={() => onNavigateTab('orders')}
            style={{ fontSize: '0.8rem' }}
          >
            Xem tất cả đơn hàng →
          </Button>
        </div>

        {shopOrders.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '2.5rem 1rem', color: 'var(--text-muted)' }}>
            <StoreIcon size={36} style={{ color: 'var(--text-dim)', marginBottom: '0.5rem' }} />
            <div style={{ fontSize: '0.9rem', fontWeight: 600, color: 'var(--text-main)' }}>Chưa có đơn hàng nào phát sinh</div>
            <div style={{ fontSize: '0.8rem', color: 'var(--text-dim)', marginTop: '0.25rem' }}>
              Khi người mua thanh toán đơn hàng số của bạn, thông tin chi tiết và tiền bán sẽ hiển thị ngay tại đây.
            </div>
          </div>
        ) : (
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.84rem' }}>
              <thead>
                <tr style={{ borderBottom: '1px solid var(--border-color)', textAlign: 'left', color: 'var(--text-dim)' }}>
                  <th style={{ padding: '0.65rem 0.5rem' }}>Mã đơn</th>
                  <th style={{ padding: '0.65rem 0.5rem' }}>Thời gian</th>
                  <th style={{ padding: '0.65rem 0.5rem' }}>Sản phẩm</th>
                  <th style={{ padding: '0.65rem 0.5rem' }}>Tổng thu</th>
                  <th style={{ padding: '0.65rem 0.5rem' }}>Thực nhận về ví</th>
                  <th style={{ padding: '0.65rem 0.5rem' }}>Trạng thái</th>
                </tr>
              </thead>
              <tbody>
                {shopOrders.slice(0, 5).map((ord) => (
                  <tr key={ord.id} style={{ borderBottom: '1px solid var(--border-color)' }}>
                    <td style={{ padding: '0.65rem 0.5rem', fontWeight: 700, color: 'var(--accent-primary)' }}>
                      #{ord.orderNo || ord.id.slice(0, 8)}
                    </td>
                    <td style={{ padding: '0.65rem 0.5rem', whiteSpace: 'nowrap', color: 'var(--text-muted)' }}>
                      {new Date(ord.createdAtUtc).toLocaleString('vi-VN')}
                    </td>
                    <td style={{ padding: '0.65rem 0.5rem' }}>
                      {ord.items?.map((it) => (
                        <div key={it.id} style={{ fontSize: '0.82rem' }}>
                          <strong style={{ color: 'var(--text-main)' }}>{it.productName}</strong>
                          <span style={{ color: 'var(--text-dim)' }}> ({it.variantLabel}) x{it.quantity}</span>
                        </div>
                      ))}
                    </td>
                    <td style={{ padding: '0.65rem 0.5rem', fontWeight: 600 }}>
                      {ord.total.toLocaleString('vi-VN')} đ
                    </td>
                    <td style={{ padding: '0.65rem 0.5rem', fontWeight: 800, color: '#10b981' }}>
                      +{ord.shopNet.toLocaleString('vi-VN')} đ
                    </td>
                    <td style={{ padding: '0.65rem 0.5rem' }}>
                      <Badge
                        variant={ord.status === 'Completed' || ord.status === 'Delivered' ? 'success' : ord.status === 'Cancelled' ? 'danger' : 'neutral'}
                        size="sm"
                      >
                        {ord.status === 'Delivered' ? 'Đã giao mã' : ord.status === 'Completed' ? 'Hoàn tất' : ord.status}
                      </Badge>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};
