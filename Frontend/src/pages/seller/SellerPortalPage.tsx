import React, { useCallback, useEffect, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { marketplaceApi } from '@/api/marketplaceApi';
import type {
  ShopPerformanceDto,
  ShopBalanceDto,
  SellerOrderDto,
} from '@/types/marketplace';
import {
  PageContainer,
  Button,
  Badge,
  Spinner,
  SparklesIcon,
  ClockIcon,
  PackageIcon,
  ScaleIcon,
} from '@/components/common';

import { RegisterShopForm } from './components/RegisterShopForm';
import { ShopSettingsModal } from './components/ShopSettingsModal';
import { ResubmitShopModal } from './components/ResubmitShopModal';

import { OverviewTab } from './tabs/OverviewTab';
import { FinanceTab } from './tabs/FinanceTab';
import { ProductsTab } from './tabs/ProductsTab';
import { OrdersTab } from './tabs/OrdersTab';
import { ComplaintsTab } from './tabs/ComplaintsTab';

type SellerTab = 'overview' | 'finance' | 'products' | 'orders' | 'complaints';

const getTabFromPath = (pathname: string): SellerTab => {
  if (pathname.includes('/seller/finance')) return 'finance';
  if (pathname.includes('/seller/products') || pathname.includes('/seller/inventory')) return 'products';
  if (pathname.includes('/seller/orders')) return 'orders';
  if (pathname.includes('/seller/complaints')) return 'complaints';
  return 'overview';
};

export const SellerPortalPage: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const activeTab = getTabFromPath(location.pathname);

  const handleTabChange = (tab: SellerTab) => {
    navigate(`/seller/${tab}`);
  };

  const [shop, setShop] = useState<any | null>(null);
  const [loading, setLoading] = useState(true);
  const [hasShop, setHasShop] = useState(false);

  // Modals
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [isResubmitOpen, setIsResubmitOpen] = useState(false);

  // Tab stats for badges
  const [performance, setPerformance] = useState<ShopPerformanceDto | null>(null);
  const [balance, setBalance] = useState<ShopBalanceDto | null>(null);
  const [shopOrders, setShopOrders] = useState<SellerOrderDto[]>([]);
  const [ordersTotalCount, setOrdersTotalCount] = useState(0);
  const [productCount, setProductCount] = useState(0);
  const [complaintCount, setComplaintCount] = useState(0);

  const fetchShopData = useCallback(async () => {
    setLoading(true);
    try {
      const shopData = await marketplaceApi.getMyShop();
      if (shopData && shopData.id) {
        setShop(shopData);
        setHasShop(true);
      } else {
        setHasShop(false);
      }
    } catch (err: any) {
      if (err?.response?.status === 404) {
        setHasShop(false);
      }
    } finally {
      setLoading(false);
    }
  }, []);

  const loadSummaryData = useCallback(async () => {
    try {
      const [perf, bal, ordersRes, complaintsRes, productsRes] = await Promise.all([
        marketplaceApi.getShopPerformance().catch(() => null),
        marketplaceApi.getShopBalance().catch(() => null),
        marketplaceApi.getShopOrders(undefined, 1, 10).catch(() => null),
        marketplaceApi.getShopComplaints().catch(() => ({ items: [] })),
        marketplaceApi.getShopProducts().catch(() => ({ items: [] })),
      ]);
      if (perf) setPerformance(perf);
      if (bal) setBalance(bal);
      if (ordersRes) {
        setShopOrders(ordersRes.items || ordersRes || []);
        setOrdersTotalCount(ordersRes.totalCount || (ordersRes.items || ordersRes || []).length);
      }
      if (complaintsRes) {
        setComplaintCount((complaintsRes.items || complaintsRes || []).length);
      }
      if (productsRes) {
        setProductCount((productsRes.items || productsRes || []).length);
      }
    } catch (err) {
      console.error('Error loading summary data:', err);
    }
  }, []);

  useEffect(() => {
    fetchShopData();
  }, [fetchShopData]);

  useEffect(() => {
    if (hasShop) {
      loadSummaryData();
    }
  }, [hasShop, loadSummaryData]);

  const handleToggleOperatingStatus = async () => {
    const isCurrentlyClosed = shop?.operatingStatus === 'Closed' || shop?.operatingStatus === 1;
    const actionText = isCurrentlyClosed ? 'mở cửa hoạt động' : 'tạm đóng cửa hàng';
    if (!window.confirm(`Bạn có chắc chắn muốn ${actionText}?`)) return;
    try {
      if (isCurrentlyClosed) {
        await marketplaceApi.openMyShop();
      } else {
        await marketplaceApi.closeMyShop();
      }
      alert(`Đã ${actionText} thành công!`);
      await fetchShopData();
    } catch (err: any) {
      alert(err.response?.data?.message || `Không thể ${actionText}.`);
    }
  };

  if (loading) {
    return (
      <PageContainer variant="wide" style={{ padding: '4rem 1rem', textAlign: 'center' }}>
        <Spinner size="lg" />
        <p style={{ marginTop: '1rem', color: 'var(--text-muted)' }}>Đang kiểm tra thông tin gian hàng...</p>
      </PageContainer>
    );
  }

  if (!hasShop) {
    return (
      <PageContainer variant="wide" style={{ padding: '2rem 1rem 4rem' }}>
        <RegisterShopForm
          onSuccess={(newShop) => {
            setShop(newShop);
            setHasShop(true);
            handleTabChange('overview');
          }}
        />
      </PageContainer>
    );
  }

  const isShopClosed = shop?.operatingStatus === 'Closed' || shop?.operatingStatus === 1;

  return (
    <PageContainer variant="wide" style={{ padding: '2rem 1rem 4rem' }}>
      {/* Banner Cảnh Báo nếu Shop bị Từ Chối (M1.8) */}
      {shop?.approvalStatus === 'Rejected' && (
        <div style={{ marginBottom: '1.5rem', background: 'rgba(239, 68, 68, 0.12)', border: '1px solid var(--danger)', borderRadius: '12px', padding: '1rem 1.25rem', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <div>
            <div style={{ fontWeight: 800, color: 'var(--danger)', fontSize: '0.95rem' }}>Gian hàng bị Quản trị viên từ chối phê duyệt</div>
            <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)', marginTop: '0.25rem' }}>
              Lý do: <em>{shop?.rejectReasonNote || 'Chưa cung cấp thông tin hợp lệ hoặc vi phạm chính sách.'}</em>
            </div>
          </div>
          <Button variant="danger" size="sm" onClick={() => setIsResubmitOpen(true)}>
            Nộp lại hồ sơ xét duyệt
          </Button>
        </div>
      )}

      {/* Header Dashboard */}
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: '1.5rem',
          flexWrap: 'wrap',
          gap: '1rem',
          background: 'var(--bg-card)',
          border: '1px solid var(--border-color)',
          borderRadius: '16px',
          padding: '1.25rem 1.5rem',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <div
            style={{
              width: '48px',
              height: '48px',
              borderRadius: '12px',
              background: 'var(--gradient-brand)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: '#fff',
              fontWeight: 800,
              fontSize: '1.25rem',
            }}
          >
            {shop?.name?.charAt(0)?.toUpperCase() || 'S'}
          </div>
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.65rem' }}>
              <h1 style={{ fontSize: '1.25rem', fontWeight: 800, color: 'var(--text-main)', margin: 0 }}>
                {shop?.name}
              </h1>
              <Badge variant={isShopClosed ? 'warning' : 'success'} size="sm">
                {isShopClosed ? 'Tạm đóng cửa' : 'Đang mở bán'}
              </Badge>
              {shop?.approvalStatus === 'Pending' && <Badge variant="neutral" size="sm">Chờ duyệt</Badge>}
            </div>
            <p style={{ margin: '0.25rem 0 0', fontSize: '0.82rem', color: 'var(--text-muted)' }}>
              Đường dẫn:{' '}
              <Link
                to={`/marketplace/shops/${shop?.slug}`}
                target="_blank"
                style={{ color: 'var(--accent-primary)', fontWeight: 700, textDecoration: 'underline' }}
                title="Xem trang gian hàng công khai"
              >
                /marketplace/shops/{shop?.slug}
              </Link>{' '}
              • Email: {shop?.contactEmail}
            </p>
          </div>
        </div>

        {/* Cụm điều khiển trạng thái Shop */}
        <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', flexWrap: 'wrap' }}>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={handleToggleOperatingStatus}
          >
            {isShopClosed ? 'Mở cửa bán hàng' : 'Tạm nghỉ bán'}
          </Button>
          <Button
            type="button"
            variant="secondary"
            size="sm"
            onClick={() => setIsSettingsOpen(true)}
          >
            Cài đặt shop
          </Button>
        </div>
      </div>

      {/* Tab Navigation */}
      <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap', marginBottom: '1.5rem' }}>
        <Button
          type="button"
          variant={activeTab === 'overview' ? 'primary' : 'secondary'}
          size="sm"
          icon={<SparklesIcon size={15} />}
          onClick={() => handleTabChange('overview')}
        >
          Tổng quan
        </Button>
        <Button
          type="button"
          variant={activeTab === 'finance' ? 'primary' : 'secondary'}
          size="sm"
          icon={<ClockIcon size={15} />}
          onClick={() => handleTabChange('finance')}
        >
          Ví & Rút tiền
        </Button>
        <Button
          type="button"
          variant={activeTab === 'products' ? 'primary' : 'secondary'}
          size="sm"
          icon={<PackageIcon size={15} />}
          onClick={() => handleTabChange('products')}
        >
          Sản phẩm & Kho mã ({productCount})
        </Button>
        <Button
          type="button"
          variant={activeTab === 'orders' ? 'primary' : 'secondary'}
          size="sm"
          icon={<PackageIcon size={15} />}
          onClick={() => handleTabChange('orders')}
        >
          Đơn hàng ({ordersTotalCount})
        </Button>
        <Button
          type="button"
          variant={activeTab === 'complaints' ? 'primary' : 'secondary'}
          size="sm"
          icon={<ScaleIcon size={15} />}
          onClick={() => handleTabChange('complaints')}
        >
          Khiếu nại ({complaintCount})
        </Button>
      </div>

      {/* TAB CONTENTS */}
      {activeTab === 'overview' && (
        <OverviewTab
          balance={balance}
          performance={performance}
          shopOrders={shopOrders}
          ordersTotalCount={ordersTotalCount}
          onNavigateTab={handleTabChange}
        />
      )}

      {activeTab === 'finance' && (
        <FinanceTab
          balance={balance}
          onRefreshBalance={loadSummaryData}
        />
      )}

      {activeTab === 'products' && (
        <ProductsTab
          onProductCountChange={setProductCount}
        />
      )}

      {activeTab === 'orders' && (
        <OrdersTab />
      )}

      {activeTab === 'complaints' && (
        <ComplaintsTab />
      )}

      {/* MODALS */}
      <ShopSettingsModal
        isOpen={isSettingsOpen}
        shop={shop}
        onClose={() => setIsSettingsOpen(false)}
        onSuccess={fetchShopData}
      />

      <ResubmitShopModal
        isOpen={isResubmitOpen}
        onClose={() => setIsResubmitOpen(false)}
        onSuccess={fetchShopData}
      />
    </PageContainer>
  );
};

export default SellerPortalPage;
