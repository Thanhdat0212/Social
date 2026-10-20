import React, { useEffect, useState } from 'react';
import { adminApi } from '@/api/adminApi';
import type { AdminUserItem } from '@/types/admin';
import { SearchIcon } from '@/components/common/Icons';

interface AdminUsersTabProps {
  showFlash: (text: string, type?: 'success' | 'error') => void;
}

export const AdminUsersTab: React.FC<AdminUsersTabProps> = ({ showFlash }) => {
  const [users, setUsers] = useState<AdminUserItem[]>([]);
  const [userSearch, setUserSearch] = useState<string>('');
  const [loading, setLoading] = useState(false);

  const loadUsers = async (searchQuery?: string) => {
    setLoading(true);
    try {
      const res = await adminApi.getUsers({ search: searchQuery || undefined, pageSize: 30 });
      setUsers(res.items || []);
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Có lỗi xảy ra khi tải danh sách người dùng.', 'error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadUsers();
  }, []);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    loadUsers(userSearch);
  };

  const handleToggleUserStatus = async (user: AdminUserItem) => {
    const nextStatus = user.status === 'Active' ? 'Suspended' : 'Active';
    const actionLabel = nextStatus === 'Suspended' ? 'khóa' : 'mở khóa';
    if (!window.confirm(`Bạn có chắc muốn ${actionLabel} tài khoản của ${user.displayName}?`)) return;
    try {
      await adminApi.updateUserStatus(user.id, nextStatus);
      showFlash(`Đã ${actionLabel} tài khoản thành công!`);
      loadUsers(userSearch);
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Lỗi khi cập nhật trạng thái.', 'error');
    }
  };

  const handleToggleUserRole = async (user: AdminUserItem) => {
    const nextRole = user.role === 'Admin' ? 'Member' : 'Admin';
    if (!window.confirm(`Chuyển vai trò của ${user.displayName} sang ${nextRole}?`)) return;
    try {
      await adminApi.updateUserRole(user.id, nextRole);
      showFlash(`Đã đổi vai trò sang ${nextRole}!`);
      loadUsers(userSearch);
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Lỗi khi đổi quyền vai trò.', 'error');
    }
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem', flexWrap: 'wrap', gap: '1rem' }}>
        <form
          onSubmit={handleSearchSubmit}
          style={{ display: 'flex', gap: '0.5rem', maxWidth: '360px', width: '100%' }}
        >
          <div style={{ position: 'relative', width: '100%' }}>
            <SearchIcon
              size={16}
              style={{ position: 'absolute', left: '12px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }}
            />
            <input
              type="text"
              className="form-control"
              placeholder="Tìm theo email hoặc tên..."
              value={userSearch}
              onChange={(e) => setUserSearch(e.target.value)}
              style={{
                paddingLeft: '36px',
                height: '38px',
                borderRadius: '10px',
                background: '#ffffff',
                border: '1px solid #cbd5e1',
                color: '#0f172a',
              }}
            />
          </div>
          <button
            type="submit"
            className="btn btn-secondary btn-sm"
            style={{
              height: '38px',
              padding: '0 1rem',
              background: '#ffffff',
              border: '1px solid #cbd5e1',
              color: '#0f172a',
              fontWeight: 600,
              borderRadius: '10px',
            }}
          >
            Tìm
          </button>
        </form>
        <span style={{ color: '#475569', fontSize: '0.88rem', fontWeight: 600 }}>
          Hiển thị <strong style={{ color: '#0f172a' }}>{users.length}</strong> tài khoản
        </span>
      </div>

      <div className="admin-table-card">
        <div style={{ overflowX: 'auto' }}>
          <table className="admin-table">
            <thead>
              <tr>
                <th>Người dùng</th>
                <th>Email</th>
                <th>Vai trò</th>
                <th>Trạng thái</th>
                <th style={{ textAlign: 'right' }}>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={5} style={{ padding: '3rem', textAlign: 'center', color: '#64748b' }}>
                    Đang tải danh sách người dùng...
                  </td>
                </tr>
              ) : users.length === 0 ? (
                <tr>
                  <td colSpan={5} style={{ padding: '3rem', textAlign: 'center', color: '#64748b' }}>
                    Không tìm thấy người dùng nào phù hợp.
                  </td>
                </tr>
              ) : (
                users.map((u) => (
                  <tr key={u.id}>
                    <td style={{ fontWeight: 700, color: '#0f172a' }}>{u.displayName}</td>
                    <td style={{ color: '#334155', fontWeight: 500 }}>{u.email}</td>
                    <td>
                      <span className={`badge ${u.role === 'Admin' ? 'badge-primary' : 'badge-secondary'}`} style={{ fontWeight: 700 }}>
                        {u.role}
                      </span>
                    </td>
                    <td>
                      <span className={`badge ${u.status === 'Active' ? 'badge-success' : 'badge-danger'}`} style={{ fontWeight: 700 }}>
                        {u.status === 'Active' ? 'Hoạt động' : 'Đã khóa'}
                      </span>
                    </td>
                    <td style={{ textAlign: 'right' }}>
                      <div style={{ display: 'inline-flex', gap: '0.5rem' }}>
                        <button
                          type="button"
                          className={`btn btn-sm ${u.status === 'Active' ? 'btn-danger' : 'btn-success'}`}
                          style={{ fontWeight: 700 }}
                          onClick={() => handleToggleUserStatus(u)}
                        >
                          {u.status === 'Active' ? 'Khóa' : 'Mở khóa'}
                        </button>
                        <button
                          type="button"
                          className="btn btn-secondary btn-sm"
                          style={{ background: '#f1f5f9', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 600 }}
                          onClick={() => handleToggleUserRole(u)}
                        >
                          {u.role === 'Admin' ? 'Bỏ quyền Admin' : 'Cấp Admin'}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
