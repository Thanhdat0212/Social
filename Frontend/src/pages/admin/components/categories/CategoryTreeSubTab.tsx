import React, { useState } from 'react';
import { adminApi } from '@/api/adminApi';
import type { AdminCategoryNode, AdminAttributeDefinition } from '@/types/admin';
import { TrashIcon } from '@/components/common/Icons';

interface CategoryTreeSubTabProps {
  categoryTree: AdminCategoryNode[];
  allFlatCategories: { id: string; name: string; depth: number }[];
  attributes: AdminAttributeDefinition[];
  loadCategoryTree: () => Promise<void>;
  showFlash: (text: string, type?: 'success' | 'error') => void;
}

export const CategoryTreeSubTab: React.FC<CategoryTreeSubTabProps> = ({
  categoryTree,
  allFlatCategories,
  attributes,
  loadCategoryTree,
  showFlash,
}) => {
  const [createCategoryModal, setCreateCategoryModal] = useState<boolean>(false);
  const [editingCategory, setEditingCategory] = useState<AdminCategoryNode | null>(null);
  const [categoryForm, setCategoryForm] = useState({
    name: '',
    slug: '',
    parentId: '' as string,
    displayOrder: 0,
    productType: 1, // 1: Digital, 2: Service
    moderationPolicy: 1, // 1: DirectListing, 2: RequiresReview
    commissionRateBp: 500, // 5% default
    isActive: true,
  });

  const [assignAttributeModal, setAssignAttributeModal] = useState<{
    isOpen: boolean;
    category: AdminCategoryNode;
  } | null>(null);
  const [assignForm, setAssignForm] = useState({
    attributeId: '',
    requirement: 3, // 1: Required, 2: Recommended, 3: Optional
    isFilterable: true,
    isVariantAxis: false,
  });

  const handleOpenCreateCategory = (parentId?: string) => {
    setEditingCategory(null);
    setCategoryForm({
      name: '',
      slug: '',
      parentId: parentId || '',
      displayOrder: 0,
      productType: 1,
      moderationPolicy: 1,
      commissionRateBp: 500,
      isActive: true,
    });
    setCreateCategoryModal(true);
  };

  const handleOpenEditCategory = (node: AdminCategoryNode) => {
    setEditingCategory(node);
    setCategoryForm({
      name: node.name,
      slug: node.slug,
      parentId: (node as any).parentId || '',
      displayOrder: node.displayOrder,
      productType: node.productType === 'Service' || (node.productType as any) === 2 ? 2 : 1,
      moderationPolicy: node.moderationPolicy === 'RequiresReview' || (node.moderationPolicy as any) === 2 ? 2 : 1,
      commissionRateBp: node.commissionRateBp ?? 500,
      isActive: node.isActive,
    });
    setCreateCategoryModal(true);
  };

  const handleSaveCategory = async () => {
    if (!categoryForm.name.trim()) {
      alert('Vui lòng nhập tên danh mục.');
      return;
    }
    try {
      if (editingCategory) {
        await adminApi.updateCategory(editingCategory.id, {
          name: categoryForm.name.trim(),
          slug: categoryForm.slug.trim() || undefined as any,
          displayOrder: Number(categoryForm.displayOrder),
          isActive: categoryForm.isActive,
          productType: Number(categoryForm.productType),
          moderationPolicy: Number(categoryForm.moderationPolicy),
          commissionRateBp: categoryForm.commissionRateBp ? Number(categoryForm.commissionRateBp) : undefined,
        });
        showFlash('Cập nhật danh mục thành công!');
      } else {
        await adminApi.createCategory({
          name: categoryForm.name.trim(),
          slug: categoryForm.slug.trim() || undefined,
          parentId: categoryForm.parentId || null,
          displayOrder: Number(categoryForm.displayOrder),
          productType: Number(categoryForm.productType),
          moderationPolicy: Number(categoryForm.moderationPolicy),
          commissionRateBp: categoryForm.commissionRateBp ? Number(categoryForm.commissionRateBp) : undefined,
        });
        showFlash('Tạo danh mục mới thành công!');
      }
      setCreateCategoryModal(false);
      loadCategoryTree();
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Lỗi khi lưu danh mục.', 'error');
    }
  };

  const handleDeleteCategory = async (id: string, name: string) => {
    if (!window.confirm(`Bạn có chắc chắn muốn xóa danh mục "${name}"? Thao tác này chỉ thực hiện được khi danh mục không có danh mục con.`)) return;
    try {
      await adminApi.deleteCategory(id);
      showFlash(`Đã xóa danh mục "${name}" thành công!`);
      loadCategoryTree();
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Không thể xóa danh mục này.', 'error');
    }
  };

  const handleAssignAttribute = async () => {
    if (!assignAttributeModal || !assignForm.attributeId) {
      alert('Vui lòng chọn thuộc tính cần gán.');
      return;
    }
    try {
      await adminApi.assignCategoryAttribute(assignAttributeModal.category.id, {
        attributeId: assignForm.attributeId,
        requirement: Number(assignForm.requirement),
        isFilterable: assignForm.isFilterable,
        isVariantAxis: assignForm.isVariantAxis,
      });
      showFlash('Đã gán thuộc tính cho danh mục thành công!');
      setAssignAttributeModal(null);
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Lỗi khi gán thuộc tính.', 'error');
    }
  };

  // Helper render cây đệ quy
  const renderRows = (nodes: AdminCategoryNode[], depth: number = 0): React.ReactNode => {
    return nodes.map((node) => {
      const isRoot = depth === 0;
      return (
        <React.Fragment key={node.id}>
          <tr style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
            <td style={{ padding: '0.85rem 1.25rem', paddingLeft: `${depth * 1.5 + 1.25}rem` }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <span style={{ color: isRoot ? '#c084fc' : 'var(--text-dim)', fontWeight: isRoot ? 800 : 600 }}>
                  {isRoot ? '📁' : '↳'} {node.name}
                </span>
                {node.children && node.children.length > 0 && (
                  <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                    ({node.children.length})
                  </span>
                )}
              </div>
            </td>
            <td style={{ padding: '0.85rem 1.25rem', color: 'var(--text-muted)', fontSize: '0.82rem' }}>
              /{node.slug}
            </td>
            <td style={{ padding: '0.85rem 1.25rem' }}>
              <span
                style={{
                  fontSize: '0.75rem',
                  padding: '2px 8px',
                  borderRadius: '6px',
                  background: (node.productType === 'Digital' || (node.productType as any) === 1) ? 'rgba(56, 189, 248, 0.1)' : 'rgba(244, 63, 94, 0.1)',
                  color: (node.productType === 'Digital' || (node.productType as any) === 1) ? '#38bdf8' : '#f43f5e',
                }}
              >
                {node.productType === 'Digital' || (node.productType as any) === 1 ? 'Hàng số' : 'Dịch vụ'}
              </span>
            </td>
            <td style={{ padding: '0.85rem 1.25rem' }}>
              <span
                style={{
                  fontSize: '0.75rem',
                  padding: '2px 8px',
                  borderRadius: '6px',
                  background: (node.moderationPolicy === 'DirectListing' || (node.moderationPolicy as any) === 1) ? 'rgba(34, 197, 94, 0.1)' : 'rgba(245, 158, 11, 0.1)',
                  color: (node.moderationPolicy === 'DirectListing' || (node.moderationPolicy as any) === 1) ? '#22c55e' : '#f59e0b',
                }}
              >
                {node.moderationPolicy === 'DirectListing' || (node.moderationPolicy as any) === 1 ? 'Tự động' : 'Duyệt trước'}
              </span>
            </td>
            <td style={{ padding: '0.85rem 1.25rem', fontWeight: 600 }}>
              {node.commissionRateBp ? `${node.commissionRateBp / 100}%` : '5% (Mặc định)'}
            </td>
            <td style={{ padding: '0.85rem 1.25rem' }}>
              <span className={`badge ${node.isActive ? 'badge-success' : 'badge-danger'}`}>
                {node.isActive ? 'Bật' : 'Tắt'}
              </span>
            </td>
            <td style={{ padding: '0.85rem 1.25rem', textAlign: 'right' }}>
              <div style={{ display: 'inline-flex', gap: '6px' }}>
                <button
                  type="button"
                  className="btn btn-secondary btn-sm"
                  title="Thêm danh mục con"
                  onClick={() => handleOpenCreateCategory(node.id)}
                  style={{ padding: '4px 8px', fontSize: '0.75rem' }}
                >
                  + Con
                </button>
                <button
                  type="button"
                  className="btn btn-secondary btn-sm"
                  title="Cấu hình thuộc tính"
                  onClick={() => {
                    setAssignAttributeModal({ isOpen: true, category: node });
                    setAssignForm({ attributeId: '', requirement: 3, isFilterable: true, isVariantAxis: false });
                  }}
                  style={{ padding: '4px 8px', fontSize: '0.75rem' }}
                >
                  ⚙️ Thuộc tính
                </button>
                <button
                  type="button"
                  className="btn btn-secondary btn-sm"
                  title="Chỉnh sửa danh mục"
                  onClick={() => handleOpenEditCategory(node)}
                  style={{ padding: '4px 8px', fontSize: '0.75rem' }}
                >
                  Sửa
                </button>
                <button
                  type="button"
                  className="btn btn-danger btn-sm"
                  title="Xóa danh mục"
                  onClick={() => handleDeleteCategory(node.id, node.name)}
                  style={{ padding: '4px 8px', fontSize: '0.75rem' }}
                >
                  <TrashIcon size={12} />
                </button>
              </div>
            </td>
          </tr>
          {node.children && node.children.length > 0 && renderRows(node.children, depth + 1)}
        </React.Fragment>
      );
    });
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: '1rem' }}>
        <button
          type="button"
          className="btn btn-primary btn-sm"
          onClick={() => handleOpenCreateCategory()}
          style={{ display: 'inline-flex', alignItems: 'center', gap: '6px' }}
        >
          + Thêm danh mục gốc
        </button>
      </div>

      <div className="card-glass" style={{ borderRadius: '16px', overflow: 'hidden', border: '1px solid var(--border-color)' }}>
        <div style={{ overflowX: 'auto' }}>
          <table className="table" style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.9rem' }}>
            <thead>
              <tr style={{ background: 'rgba(255,255,255,0.03)', borderBottom: '1px solid var(--border-color)', textAlign: 'left' }}>
                <th style={{ padding: '1rem 1.25rem' }}>Tên danh mục & Phân cấp</th>
                <th style={{ padding: '1rem 1.25rem' }}>Slug & Đường dẫn</th>
                <th style={{ padding: '1rem 1.25rem' }}>Loại hàng</th>
                <th style={{ padding: '1rem 1.25rem' }}>Kiểm duyệt</th>
                <th style={{ padding: '1rem 1.25rem' }}>Hoa hồng</th>
                <th style={{ padding: '1rem 1.25rem' }}>Trạng thái</th>
                <th style={{ padding: '1rem 1.25rem', textAlign: 'right' }}>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {categoryTree.length === 0 ? (
                <tr>
                  <td colSpan={7} style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-muted)' }}>
                    Chưa có danh mục nào. Hãy tạo danh mục đầu tiên!
                  </td>
                </tr>
              ) : (
                renderRows(categoryTree)
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* MODAL: CREATE / EDIT CATEGORY */}
      {createCategoryModal && (
        <div className="modal-backdrop" style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.7)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
          <div className="card-glass" style={{ width: '90%', maxWidth: '540px', padding: '2rem', borderRadius: '16px', border: '1px solid var(--border-color)', maxHeight: '90vh', overflowY: 'auto' }}>
            <h3 style={{ margin: '0 0 1.25rem', fontWeight: 700 }}>
              {editingCategory ? `Chỉnh sửa: ${editingCategory.name}` : 'Tạo danh mục mới'}
            </h3>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                  Tên danh mục *
                </label>
                <input
                  type="text"
                  className="form-control"
                  placeholder="Ví dụ: Tài khoản Netflix, Bản quyền Windows"
                  value={categoryForm.name}
                  onChange={(e) => setCategoryForm({ ...categoryForm, name: e.target.value })}
                />
              </div>

              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                  Slug (Đường dẫn tĩnh) - Tùy chọn
                </label>
                <input
                  type="text"
                  className="form-control"
                  placeholder="tai-khoan-netflix (bỏ trống tự tạo)"
                  value={categoryForm.slug}
                  onChange={(e) => setCategoryForm({ ...categoryForm, slug: e.target.value })}
                />
              </div>

              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                  Danh mục cha (Parent Category)
                </label>
                <select
                  className="form-control"
                  value={categoryForm.parentId}
                  onChange={(e) => setCategoryForm({ ...categoryForm, parentId: e.target.value })}
                >
                  <option value="">-- Là danh mục gốc (Level 1) --</option>
                  {allFlatCategories
                    .filter((c) => !editingCategory || c.id !== editingCategory.id)
                    .map((c) => (
                      <option key={c.id} value={c.id}>
                        {'- '.repeat(c.depth)} {c.name}
                      </option>
                    ))}
                </select>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                <div>
                  <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                    Loại sản phẩm
                  </label>
                  <select
                    className="form-control"
                    value={categoryForm.productType}
                    onChange={(e) => setCategoryForm({ ...categoryForm, productType: Number(e.target.value) })}
                  >
                    <option value={1}>Hàng số (Digital)</option>
                    <option value={2}>Dịch vụ (Service)</option>
                  </select>
                </div>

                <div>
                  <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                    Chính sách kiểm duyệt
                  </label>
                  <select
                    className="form-control"
                    value={categoryForm.moderationPolicy}
                    onChange={(e) => setCategoryForm({ ...categoryForm, moderationPolicy: Number(e.target.value) })}
                  >
                    <option value={1}>Đăng trực tiếp (Direct)</option>
                    <option value={2}>Admin duyệt trước (Review)</option>
                  </select>
                </div>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                <div>
                  <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                    Tỷ lệ hoa hồng sàn (Basis Points)
                  </label>
                  <input
                    type="number"
                    className="form-control"
                    placeholder="500 = 5%"
                    value={categoryForm.commissionRateBp}
                    onChange={(e) => setCategoryForm({ ...categoryForm, commissionRateBp: Number(e.target.value) })}
                  />
                  <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>500 = 5%, 800 = 8%</span>
                </div>

                <div>
                  <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                    Thứ tự hiển thị
                  </label>
                  <input
                    type="number"
                    className="form-control"
                    value={categoryForm.displayOrder}
                    onChange={(e) => setCategoryForm({ ...categoryForm, displayOrder: Number(e.target.value) })}
                  />
                </div>
              </div>

              <div style={{ marginTop: '0.5rem' }}>
                <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '0.85rem', cursor: 'pointer' }}>
                  <input
                    type="checkbox"
                    checked={categoryForm.isActive}
                    onChange={(e) => setCategoryForm({ ...categoryForm, isActive: e.target.checked })}
                  />
                  Kích hoạt danh mục này hiển thị công khai trên chợ
                </label>
              </div>
            </div>

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.5rem' }}>
              <button type="button" className="btn btn-secondary" onClick={() => setCreateCategoryModal(false)}>
                Hủy
              </button>
              <button type="button" className="btn btn-primary" onClick={handleSaveCategory}>
                {editingCategory ? 'Lưu cập nhật' : 'Tạo danh mục'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* MODAL: ASSIGN ATTRIBUTE TO CATEGORY */}
      {assignAttributeModal && (
        <div className="modal-backdrop" style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.7)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
          <div className="card-glass" style={{ width: '90%', maxWidth: '480px', padding: '2rem', borderRadius: '16px', border: '1px solid var(--border-color)' }}>
            <h3 style={{ margin: '0 0 1rem', fontWeight: 700 }}>
              Gán thuộc tính cho: {assignAttributeModal.category.name}
            </h3>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                  Chọn thuộc tính định nghĩa *
                </label>
                <select
                  className="form-control"
                  value={assignForm.attributeId}
                  onChange={(e) => setAssignForm({ ...assignForm, attributeId: e.target.value })}
                >
                  <option value="">-- Chọn thuộc tính --</option>
                  {attributes.map((a) => (
                    <option key={a.id} value={a.id}>
                      {a.name} ({a.code}) - {a.dataType}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                  Mức độ bắt buộc
                </label>
                <select
                  className="form-control"
                  value={assignForm.requirement}
                  onChange={(e) => setAssignForm({ ...assignForm, requirement: Number(e.target.value) })}
                >
                  <option value={1}>Bắt buộc điền (Required)</option>
                  <option value={2}>Khuyến nghị điền (Recommended)</option>
                  <option value={3}>Tùy chọn (Optional)</option>
                </select>
              </div>

              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem', marginTop: '0.5rem' }}>
                <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '0.85rem', cursor: 'pointer' }}>
                  <input
                    type="checkbox"
                    checked={assignForm.isFilterable}
                    onChange={(e) => setAssignForm({ ...assignForm, isFilterable: e.target.checked })}
                  />
                  Cho phép người mua lọc sản phẩm theo thuộc tính này (IsFilterable)
                </label>
                <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '0.85rem', cursor: 'pointer' }}>
                  <input
                    type="checkbox"
                    checked={assignForm.isVariantAxis}
                    onChange={(e) => setAssignForm({ ...assignForm, isVariantAxis: e.target.checked })}
                  />
                  Làm trục phân loại biến thể (IsVariantAxis - ví dụ Dung lượng, Màu sắc, Gói cước)
                </label>
              </div>
            </div>

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.5rem' }}>
              <button type="button" className="btn btn-secondary" onClick={() => setAssignAttributeModal(null)}>
                Đóng
              </button>
              <button type="button" className="btn btn-primary" onClick={handleAssignAttribute}>
                Gán thuộc tính
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
