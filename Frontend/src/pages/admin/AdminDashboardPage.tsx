import React, { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import {
  ShieldIcon,
  ScaleIcon,
  StoreIcon,
  ClockIcon,
  AlertTriangleIcon,
  RefreshIcon,
  UsersIcon,
  FileTextIcon,
  TagIcon,
  PackageIcon,
} from '@/components/common/Icons';
import { AdminShopsTab } from './components/AdminShopsTab';
import { AdminPayoutsTab } from './components/AdminPayoutsTab';
import { AdminComplaintsTab } from './components/AdminComplaintsTab';
import { AdminReportsTab } from './components/AdminReportsTab';
import { AdminAuditTab } from './components/AdminAuditTab';
import { AdminUsersTab } from './components/AdminUsersTab';
import { AdminCategoriesTab } from './components/AdminCategoriesTab';
import { AdminProductsTab } from './components/AdminProductsTab';

type AdminTab = 'shops' | 'payouts' | 'complaints' | 'reports' | 'audit' | 'users' | 'categories' | 'products';

const getAdminTabFromPath = (pathname: string): AdminTab => {
  if (pathname.includes('/admin/payouts')) return 'payouts';
  if (pathname.includes('/admin/complaints')) return 'complaints';
  if (pathname.includes('/admin/reports')) return 'reports';
  if (pathname.includes('/admin/audit')) return 'audit';
  if (pathname.includes('/admin/users')) return 'users';
  if (pathname.includes('/admin/categories')) return 'categories';
  if (pathname.includes('/admin/products')) return 'products';
  return 'shops';
};

export const AdminDashboardPage: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const activeTab = getAdminTabFromPath(location.pathname);

  const [refreshKey, setRefreshKey] = useState<number>(0);
  const [message, setMessage] = useState<{ text: string; type: 'success' | 'error' } | null>(null);

  const handleTabChange = (tab: AdminTab) => {
    navigate(`/admin/${tab}`);
  };

  const showFlash = (text: string, type: 'success' | 'error' = 'success') => {
    setMessage({ text, type });
    setTimeout(() => setMessage(null), 5000);
  };

  const handleRefresh = () => {
    setRefreshKey((prev) => prev + 1);
  };

  return (
    <div className="marketplace-container" style={{ paddingBottom: '4rem' }}>
      {/* Top Banner Header */}
      <div className="admin-banner">
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.85rem', marginBottom: '0.65rem' }}>
            <span className="admin-banner-icon-wrap">
              <ShieldIcon size={24} />
            </span>
            <h1 className="admin-banner-title">
              Trung Tâm Quản Trị Toàn Sàn
            </h1>
          </div>
          <p className="admin-banner-desc">
            Giám sát vận hành sàn hàng số, phân xử tranh chấp, kiểm toán sổ cái kép bất biến và quản lý quyền truy cập.
          </p>
        </div>

        <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
          <button
            type="button"
            className="btn btn-secondary btn-sm"
            onClick={handleRefresh}
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '6px',
              background: '#ffffff',
              color: '#0f172a',
              fontWeight: 700,
              boxShadow: '0 2px 4px rgba(0,0,0,0.15)',
              border: 'none',
              padding: '0.55rem 1rem',
            }}
          >
            <RefreshIcon size={15} /> Làm mới
          </button>
        </div>
      </div>

      {/* Flash Message Banner */}
      {message && (
        <div
          className={`alert ${message.type === 'success' ? 'alert-success' : 'alert-danger'}`}
          style={{ marginBottom: '1.5rem', borderRadius: '12px' }}
        >
          {message.text}
        </div>
      )}

      {/* Navigation Tabs Bar */}
      <div className="admin-tabs-nav">
        {[
          { key: 'shops' as const, label: 'Gian hàng & Chế tài', icon: <StoreIcon size={18} /> },
          { key: 'payouts' as const, label: 'Hàng đợi Rút tiền', icon: <ClockIcon size={18} /> },
          { key: 'complaints' as const, label: 'Phán quyết Khiếu nại', icon: <ScaleIcon size={18} /> },
          { key: 'reports' as const, label: 'Báo cáo Vi phạm', icon: <AlertTriangleIcon size={18} /> },
          { key: 'audit' as const, label: 'Kiểm toán Sổ cái kép', icon: <FileTextIcon size={18} /> },
          { key: 'users' as const, label: 'Người dùng & Phân quyền', icon: <UsersIcon size={18} /> },
          { key: 'categories' as const, label: 'Danh mục & Thuộc tính', icon: <TagIcon size={18} /> },
          { key: 'products' as const, label: 'Sản phẩm & Kiểm duyệt', icon: <PackageIcon size={18} /> },
        ].map((tab) => (
          <button
            key={tab.key}
            type="button"
            className={`admin-tab-btn ${activeTab === tab.key ? 'active' : ''}`}
            onClick={() => handleTabChange(tab.key)}
          >
            {tab.icon}
            <span>{tab.label}</span>
          </button>
        ))}
      </div>

      {/* Active Tab Component */}
      <div key={refreshKey}>
        {activeTab === 'shops' && <AdminShopsTab showFlash={showFlash} />}
        {activeTab === 'payouts' && <AdminPayoutsTab showFlash={showFlash} />}
        {activeTab === 'complaints' && <AdminComplaintsTab showFlash={showFlash} />}
        {activeTab === 'reports' && <AdminReportsTab showFlash={showFlash} />}
        {activeTab === 'audit' && <AdminAuditTab showFlash={showFlash} />}
        {activeTab === 'users' && <AdminUsersTab showFlash={showFlash} />}
        {activeTab === 'categories' && <AdminCategoriesTab showFlash={showFlash} />}
        {activeTab === 'products' && <AdminProductsTab showFlash={showFlash} />}
      </div>
    </div>
  );
};
