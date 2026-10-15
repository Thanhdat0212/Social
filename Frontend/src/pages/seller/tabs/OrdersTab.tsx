import React, { useEffect, useState } from 'react';
import { marketplaceApi } from '@/api/marketplaceApi';
import type { SellerOrderDto } from '@/types/marketplace';
import { Button, Badge, Spinner, StoreIcon } from '@/components/common';

export const OrdersTab: React.FC = () => {
  const [shopOrders, setShopOrders] = useState<SellerOrderDto[]>([]);
  const [ordersLoading, setOrdersLoading] = useState(false);
  const [ordersFilterStatus, setOrdersFilterStatus] = useState<string>('all');
  const [ordersPage, setOrdersPage] = useState(1);
  const [ordersTotalCount, setOrdersTotalCount] = useState(0);

  const loadOrders = async (status: string, page: number) => {
    setOrdersLoading(true);
    try {
      const statusParam = status !== 'all' ? status : undefined;
      const res = await marketplaceApi.getShopOrders(statusParam, page, 15);
      setShopOrders(res.items || res || []);
      setOrdersTotalCount(res.totalCount || (res.items || res || []).length);
    } catch (err) {
      console.error('Error loading shop orders:', err);
    } finally {
      setOrdersLoading(false);
    }
  };

  useEffect(() => {
    loadOrders(ordersFilterStatus, ordersPage);
  }, []);

  const handleFilterChange = (newStatus: string) => {
    setOrdersFilterStatus(newStatus);
    setOrdersPage(1);
    loadOrders(newStatus, 1);
  };

  const handlePageChange = (newPage: number) => {
    setOrdersPage(newPage);
    loadOrders(ordersFilterStatus, newPage);
  };

  return (
    <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '16px', padding: '1.5rem' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem', flexWrap: 'wrap', gap: '0.75rem' }}>
        <div>
          <h2 style={{ fontSize: '1.15rem', fontWeight: 700, color: 'var(--text-main)', margin: 0 }}>
            Quản lý đơn hàng bán của Gian hàng ({ordersTotalCount || shopOrders.length} đơn)
          </h2>
          <p style={{ margin: '0.2rem 0 0', fontSize: '0.8rem', color: 'var(--text-dim)' }}>
            Chi tiết các giao dịch bán hàng, doanh thu thực nhận và trạng thái giao mã số tự động
          </p>
        </div>

        {/* Bộ lọc trạng thái đơn hàng */}
        <div style={{ display: 'flex', gap: '0.4rem', flexWrap: 'wrap' }}>
          {[
            { key: 'all', label: 'Tất cả' },
            { key: 'Delivered', label: 'Đã giao mã' },
            { key: 'Completed', label: 'Hoàn tất' },
            { key: 'Cancelled', label: 'Đã hủy' },
          ].map((st) => (
            <Button
              key={st.key}
              type="button"
              variant={ordersFilterStatus === st.key ? 'primary' : 'secondary'}
              size="xs"
              onClick={() => handleFilterChange(st.key)}
            >
              {st.label}
            </Button>
          ))}
        </div>
      </div>

      {ordersLoading ? (
        <div style={{ textAlign: 'center', padding: '3rem' }}>
          <Spinner size="md" />
          <p style={{ marginTop: '0.5rem', fontSize: '0.85rem', color: 'var(--text-dim)' }}>Đang tải danh sách đơn hàng...</p>
        </div>
      ) : shopOrders.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '3.5rem 1rem', color: 'var(--text-muted)' }}>
          <StoreIcon size={40} style={{ color: 'var(--text-dim)', marginBottom: '0.75rem' }} />
          <div style={{ fontWeight: 600, color: 'var(--text-main)' }}>Chưa có đơn hàng nào phù hợp</div>
          <div style={{ fontSize: '0.82rem', color: 'var(--text-dim)', marginTop: '0.25rem' }}>
            Khi khách hàng đặt mua các sản phẩm từ gian hàng của bạn, đơn hàng sẽ hiển thị tại đây.
          </div>
        </div>
      ) : (
        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.85rem' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid var(--border-color)', textAlign: 'left', color: 'var(--text-dim)' }}>
                <th style={{ padding: '0.65rem 0.5rem' }}>Mã đơn</th>
                <th style={{ padding: '0.65rem 0.5rem' }}>Thời gian</th>
                <th style={{ padding: '0.65rem 0.5rem' }}>Mặt hàng & Biến thể</th>
                <th style={{ padding: '0.65rem 0.5rem' }}>Tổng thu</th>
                <th style={{ padding: '0.65rem 0.5rem' }}>Phí sàn</th>
                <th style={{ padding: '0.65rem 0.5rem' }}>Thực nhận về ví</th>
                <th style={{ padding: '0.65rem 0.5rem' }}>Trạng thái</th>
              </tr>
            </thead>
            <tbody>
              {shopOrders.map((ord) => (
                <tr key={ord.id} style={{ borderBottom: '1px solid var(--border-color)' }}>
                  <td style={{ padding: '0.65rem 0.5rem', fontWeight: 700, color: 'var(--accent-primary)' }}>
                    #{ord.orderNo || ord.id.slice(0, 8)}
                  </td>
                  <td style={{ padding: '0.65rem 0.5rem', whiteSpace: 'nowrap', color: 'var(--text-muted)' }}>
                    {new Date(ord.createdAtUtc).toLocaleString('vi-VN')}
                  </td>
                  <td style={{ padding: '0.65rem 0.5rem' }}>
                    {ord.items?.map((it) => (
                      <div key={it.id} style={{ fontSize: '0.82rem', marginBottom: '0.2rem' }}>
                        <strong style={{ color: 'var(--text-main)' }}>{it.productName}</strong>
                        <span style={{ color: 'var(--text-dim)' }}> ({it.variantLabel}) x{it.quantity}</span>
                      </div>
                    ))}
                  </td>
                  <td style={{ padding: '0.65rem 0.5rem', fontWeight: 600 }}>
                    {ord.total.toLocaleString('vi-VN')} đ
                  </td>
                  <td style={{ padding: '0.65rem 0.5rem', color: '#f43f5e', fontSize: '0.82rem' }}>
                    -{ord.commissionTotal.toLocaleString('vi-VN')} đ
                  </td>
                  <td style={{ padding: '0.65rem 0.5rem', fontWeight: 800, color: '#10b981' }}>
                    +{ord.shopNet.toLocaleString('vi-VN')} đ
                  </td>
                  <td style={{ padding: '0.65rem 0.5rem' }}>
                    <Badge
                      variant={ord.status === 'Completed' || ord.status === 'Delivered' ? 'success' : ord.status === 'Cancelled' ? 'danger' : 'neutral'}
                      size="sm"
                    >
                      {ord.status === 'Delivered' ? 'Đã giao mã (Chờ 48h)' : ord.status === 'Completed' ? 'Hoàn tất' : ord.status}
                    </Badge>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {/* Phân trang đơn hàng */}
          {ordersTotalCount > 15 && (
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '1.25rem', paddingTop: '0.75rem', borderTop: '1px solid var(--border-color)' }}>
              <span style={{ fontSize: '0.8rem', color: 'var(--text-dim)' }}>
                Trang {ordersPage} / {Math.ceil(ordersTotalCount / 15)} (Tổng {ordersTotalCount} đơn)
              </span>
              <div style={{ display: 'flex', gap: '0.45rem' }}>
                <Button
                  variant="secondary"
                  size="xs"
                  disabled={ordersPage <= 1 || ordersLoading}
                  onClick={() => handlePageChange(ordersPage - 1)}
                >
                  Trang trước
                </Button>
                <Button
                  variant="secondary"
                  size="xs"
                  disabled={ordersPage >= Math.ceil(ordersTotalCount / 15) || ordersLoading}
                  onClick={() => handlePageChange(ordersPage + 1)}
                >
                  Trang sau
                </Button>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
