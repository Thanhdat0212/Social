import React, { useEffect, useState } from 'react';
import { marketplaceApi } from '@/api/marketplaceApi';
import type { SellerOrderDto } from '@/types/marketplace';
import { Button, Badge, Spinner, StoreIcon } from '@/components/common';
import { ImportOrdersExcelModal } from '../components/ImportOrdersExcelModal';
import { CreateManualOrderModal } from '../components/CreateManualOrderModal';

export const OrdersTab: React.FC = () => {
  const [shopOrders, setShopOrders] = useState<SellerOrderDto[]>([]);
  const [ordersLoading, setOrdersLoading] = useState(false);
  const [ordersFilterStatus, setOrdersFilterStatus] = useState<string>('all');
  const [ordersPage, setOrdersPage] = useState(1);
  const [ordersTotalCount, setOrdersTotalCount] = useState(0);

  // Modals
  const [isExcelModalOpen, setIsExcelModalOpen] = useState(false);
  const [isManualModalOpen, setIsManualModalOpen] = useState(false);

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

        {/* Cụm chức năng nhập đơn & bộ lọc */}
        <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', flexWrap: 'wrap' }}>
          {/* Nút hành động cho Seller */}
          <Button
            type="button"
            variant="secondary"
            size="xs"
            onClick={() => setIsExcelModalOpen(true)}
            style={{ fontWeight: 700 }}
          >
            📥 Nhập đơn Excel
          </Button>

          <Button
            type="button"
            variant="primary"
            size="xs"
            onClick={() => setIsManualModalOpen(true)}
            style={{ fontWeight: 700 }}
          >
            + Tạo đơn mới
          </Button>

          {/* Bộ lọc trạng thái đơn hàng */}
          <div style={{ display: 'flex', gap: '0.35rem', marginLeft: '0.25rem' }}>
            {[
              { key: 'all', label: 'Tất cả' },
              { key: 'Delivered', label: 'Đã giao mã' },
              { key: 'Completed', label: 'Hoàn tất' },
              { key: 'Cancelled', label: 'Đã hủy' },
            ].map((st) => (
              <Button
                key={st.key}
                type="button"
                variant={ordersFilterStatus === st.key ? 'primary' : 'ghost'}
                size="xs"
                onClick={() => handleFilterChange(st.key)}
              >
                {st.label}
              </Button>
            ))}
          </div>
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
            Khi khách hàng đặt mua các sản phẩm hoặc bạn tự nhập đơn ngoài, đơn hàng sẽ hiển thị tại đây.
          </div>
        </div>
      ) : (
        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.85rem' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid var(--border-color)', textAlign: 'left', color: 'var(--text-dim)' }}>
                <th style={{ padding: '0.65rem 0.5rem' }}>Mã đơn</th>
                <th style={{ padding: '0.65rem 0.5rem' }}>Nguồn đơn</th>
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
                  <td style={{ padding: '0.65rem 0.5rem' }}>
                    {ord.source === 'ExcelImport' ? (
                      <span style={{ fontSize: '0.75rem', fontWeight: 700, padding: '2px 8px', borderRadius: '6px', background: 'rgba(168, 85, 247, 0.15)', color: '#a855f7', border: '1px solid rgba(168, 85, 247, 0.3)' }}>
                        Nhập Excel
                      </span>
                    ) : ord.source === 'ManualEntry' ? (
                      <span style={{ fontSize: '0.75rem', fontWeight: 700, padding: '2px 8px', borderRadius: '6px', background: 'rgba(245, 158, 11, 0.15)', color: '#f59e0b', border: '1px solid rgba(245, 158, 11, 0.3)' }}>
                        Bán tại quầy
                      </span>
                    ) : (
                      <span style={{ fontSize: '0.75rem', fontWeight: 600, padding: '2px 8px', borderRadius: '6px', background: 'rgba(100, 116, 139, 0.15)', color: '#94a3b8' }}>
                        Sàn PayOS
                      </span>
                    )}
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
                    {ord.externalCustomerInfo && (
                      <div style={{ fontSize: '0.75rem', color: 'var(--text-dim)', marginTop: '2px' }}>
                        Khách: {ord.externalCustomerInfo}
                      </div>
                    )}
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

      {/* Modal Nhập đơn hàng bằng Excel */}
      <ImportOrdersExcelModal
        isOpen={isExcelModalOpen}
        onClose={() => setIsExcelModalOpen(false)}
        onSuccess={() => loadOrders(ordersFilterStatus, 1)}
      />

      {/* Modal Tạo đơn bán lẻ thủ công */}
      <CreateManualOrderModal
        isOpen={isManualModalOpen}
        onClose={() => setIsManualModalOpen(false)}
        onSuccess={() => loadOrders(ordersFilterStatus, 1)}
      />
    </div>
  );
};

