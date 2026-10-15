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
      <div
        className="card-glass"
        style={{
          padding: '2rem 2.5rem',
          borderRadius: '20px',
          marginBottom: '2rem',
          border: '1px solid rgba(168, 85, 247, 0.35)',
          background: 'linear-gradient(135deg, #1a0f30 0%, #111827 50%, #0b1329 100%)',
          boxShadow: '0 10px 25px -5px rgba(15, 23, 42, 0.35), 0 8px 10px -6px rgba(15, 23, 42, 0.2)',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          flexWrap: 'wrap',
          gap: '1.5rem',
        }}
      >
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '0.5rem' }}>
            <span
              style={{
                width: '40px',
                height: '40px',
                borderRadius: '12px',
                background: 'rgba(168, 85, 247, 0.25)',
                color: '#d8b4fe',
                border: '1px solid rgba(168, 85, 247, 0.4)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <ShieldIcon size={22} />
            </span>
            <h1 style={{ fontSize: '1.85rem', fontWeight: 800, margin: 0, letterSpacing: '-0.02em', color: '#ffffff' }}>
              Trung Tâm Quản Trị Toàn Sàn
            </h1>
          </div>
          <p style={{ color: '#e2e8f0', margin: 0, fontSize: '0.95rem', maxWidth: '700px', lineHeight: 1.6 }}>
            Giám sát vận hành sàn hàng số, phân xử tranh chấp, kiểm toán sổ cái kép bất biến và quản lý quyền truy cập.
          </p>
        </div>

        <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
          <button
            type="button"
            className="btn btn-secondary btn-sm"
            onClick={handleRefresh}
            style={{ display: 'inline-flex', alignItems: 'center', gap: '6px' }}
          >
            <RefreshIcon size={14} /> Làm mới
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
      <div
        style={{
          display: 'flex',
          gap: '0.5rem',
          borderBottom: '1px solid var(--border-color)',
          marginBottom: '2rem',
          overflowX: 'auto',
          paddingBottom: '4px',
        }}
      >
        <button
          type="button"
          className={`tab-btn ${activeTab === 'shops' ? 'active' : ''}`}
          onClick={() => handleTabChange('shops')}
          style={{
            padding: '0.75rem 1.25rem',
            background: 'none',
            border: 'none',
            borderBottom: activeTab === 'shops' ? '3px solid #c084fc' : '3px solid transparent',
            color: activeTab === 'shops' ? '#c084fc' : 'var(--text-muted)',
            fontWeight: 700,
            cursor: 'pointer',
            display: 'inline-flex',
            alignItems: 'center',
            gap: '8px',
          }}
        >
          <StoreIcon size={17} /> Gian hàng & Chế tài
        </button>

        <button
          type="button"
          className={`tab-btn ${activeTab === 'payouts' ? 'active' : ''}`}
          onClick={() => handleTabChange('payouts')}
          style={{
            padding: '0.75rem 1.25rem',
            background: 'none',
            border: 'none',
            borderBottom: activeTab === 'payouts' ? '3px solid #c084fc' : '3px solid transparent',
            color: activeTab === 'payouts' ? '#c084fc' : 'var(--text-muted)',
            fontWeight: 700,
            cursor: 'pointer',
            display: 'inline-flex',
            alignItems: 'center',
            gap: '8px',
          }}
        >
          <ClockIcon size={17} /> Hàng đợi Rút tiền
        </button>

        <button
          type="button"
          className={`tab-btn ${activeTab === 'complaints' ? 'active' : ''}`}
          onClick={() => handleTabChange('complaints')}
          style={{
            padding: '0.75rem 1.25rem',
            background: 'none',
            border: 'none',
            borderBottom: activeTab === 'complaints' ? '3px solid #c084fc' : '3px solid transparent',
            color: activeTab === 'complaints' ? '#c084fc' : 'var(--text-muted)',
            fontWeight: 700,
            cursor: 'pointer',
            display: 'inline-flex',
            alignItems: 'center',
            gap: '8px',
          }}
        >
          <ScaleIcon size={17} /> Phán quyết Khiếu nại
        </button>

        <button
          type="button"
          className={`tab-btn ${activeTab === 'reports' ? 'active' : ''}`}
          onClick={() => handleTabChange('reports')}
          style={{
            padding: '0.75rem 1.25rem',
            background: 'none',
            border: 'none',
            borderBottom: activeTab === 'reports' ? '3px solid #c084fc' : '3px solid transparent',
            color: activeTab === 'reports' ? '#c084fc' : 'var(--text-muted)',
            fontWeight: 700,
            cursor: 'pointer',
            display: 'inline-flex',
            alignItems: 'center',
            gap: '8px',
          }}
        >
          <AlertTriangleIcon size={17} /> Báo cáo Vi phạm
        </button>

        <button
          type="button"
          className={`tab-btn ${activeTab === 'audit' ? 'active' : ''}`}
          onClick={() => handleTabChange('audit')}
          style={{
            padding: '0.75rem 1.25rem',
            background: 'none',
            border: 'none',
            borderBottom: activeTab === 'audit' ? '3px solid #c084fc' : '3px solid transparent',
            color: activeTab === 'audit' ? '#c084fc' : 'var(--text-muted)',
            fontWeight: 700,
            cursor: 'pointer',
            display: 'inline-flex',
            alignItems: 'center',
            gap: '8px',
          }}
        >
          <FileTextIcon size={17} /> Kiểm toán Sổ cái kép
        </button>

        <button
          type="button"
          className={`tab-btn ${activeTab === 'users' ? 'active' : ''}`}
          onClick={() => handleTabChange('users')}
          style={{
            padding: '0.75rem 1.25rem',
            background: 'none',
            border: 'none',
            borderBottom: activeTab === 'users' ? '3px solid #c084fc' : '3px solid transparent',
            color: activeTab === 'users' ? '#c084fc' : 'var(--text-muted)',
            fontWeight: 700,
            cursor: 'pointer',
            display: 'inline-flex',
            alignItems: 'center',
            gap: '8px',
          }}
        >
          <UsersIcon size={17} /> Người dùng & Phân quyền
        </button>

        <button
          type="button"
          className={`tab-btn ${activeTab === 'categories' ? 'active' : ''}`}
          onClick={() => handleTabChange('categories')}
          style={{
            padding: '0.75rem 1.25rem',
            background: 'none',
            border: 'none',
            borderBottom: activeTab === 'categories' ? '3px solid #c084fc' : '3px solid transparent',
            color: activeTab === 'categories' ? '#c084fc' : 'var(--text-muted)',
            fontWeight: 700,
            cursor: 'pointer',
            display: 'inline-flex',
            alignItems: 'center',
            gap: '8px',
          }}
        >
          <TagIcon size={17} /> Danh mục & Thuộc tính
        </button>

        <button
          type="button"
          className={`tab-btn ${activeTab === 'products' ? 'active' : ''}`}
          onClick={() => handleTabChange('products')}
          style={{
            padding: '0.75rem 1.25rem',
            background: 'none',
            border: 'none',
            borderBottom: activeTab === 'products' ? '3px solid #c084fc' : '3px solid transparent',
            color: activeTab === 'products' ? '#c084fc' : 'var(--text-muted)',
            fontWeight: 700,
            cursor: 'pointer',
            display: 'inline-flex',
            alignItems: 'center',
            gap: '8px',
          }}
        >
          <PackageIcon size={17} /> Sản phẩm & Kiểm duyệt
        </button>
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
