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
          <tr>
            <td style={{ padding: '0.95rem 1.25rem', paddingLeft: `${depth * 1.5 + 1.25}rem` }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <span style={{ color: isRoot ? '#0f172a' : '#334155', fontWeight: isRoot ? 800 : 600, fontSize: isRoot ? '0.95rem' : '0.88rem' }}>
                  {isRoot ? '📁' : '↳'} {node.name}
                </span>
                {node.children && node.children.length > 0 && (
                  <span style={{ fontSize: '0.75rem', color: '#64748b', fontWeight: 600 }}>
                    ({node.children.length})
                  </span>
                )}
              </div>
            </td>
            <td style={{ padding: '0.95rem 1.25rem', color: '#64748b', fontSize: '0.85rem', fontFamily: 'monospace' }}>
              /{node.slug}
            </td>
            <td style={{ padding: '0.95rem 1.25rem' }}>
              <span
                className={`admin-pill-badge ${(node.productType === 'Digital' || (node.productType as any) === 1) ? 'admin-pill-blue' : 'admin-pill-purple'}`}
              >
                {node.productType === 'Digital' || (node.productType as any) === 1 ? 'Hàng số' : 'Dịch vụ'}
              </span>
            </td>
            <td style={{ padding: '0.95rem 1.25rem' }}>
              <span
                className={`admin-pill-badge ${(node.moderationPolicy === 'DirectListing' || (node.moderationPolicy as any) === 1) ? 'admin-pill-green' : 'admin-pill-amber'}`}
              >
                {node.moderationPolicy === 'DirectListing' || (node.moderationPolicy as any) === 1 ? 'Tự động' : 'Duyệt trước'}
              </span>
            </td>
            <td style={{ padding: '0.95rem 1.25rem', fontWeight: 700, color: '#0f172a' }}>
              {node.commissionRateBp ? `${node.commissionRateBp / 100}%` : '5% (Mặc định)'}
            </td>
            <td style={{ padding: '0.95rem 1.25rem' }}>
              <span
                className={`admin-pill-badge ${node.isActive ? 'admin-pill-green' : 'admin-pill-red'}`}
              >
                {node.isActive ? 'Đang bật' : 'Tắt'}
              </span>
            </td>
            <td style={{ padding: '0.95rem 1.25rem', textAlign: 'right' }}>
              <div style={{ display: 'inline-flex', gap: '6px' }}>
                <button
                  type="button"
                  className="btn btn-secondary btn-sm"
                  title="Thêm danh mục con"
                  onClick={() => handleOpenCreateCategory(node.id)}
                  style={{ padding: '4px 8px', fontSize: '0.78rem', fontWeight: 600 }}
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
                  style={{ padding: '4px 8px', fontSize: '0.78rem', fontWeight: 600 }}
                >
                  ⚙️ Thuộc tính
                </button>
                <button
                  type="button"
                  className="btn btn-secondary btn-sm"
                  title="Chỉnh sửa danh mục"
                  onClick={() => handleOpenEditCategory(node)}
                  style={{ padding: '4px 8px', fontSize: '0.78rem', fontWeight: 600 }}
                >
                  Sửa
                </button>
                <button
                  type="button"
                  className="btn btn-danger btn-sm"
                  title="Xóa danh mục"
                  onClick={() => handleDeleteCategory(node.id, node.name)}
                  style={{ padding: '4px 8px', fontSize: '0.78rem', fontWeight: 700 }}
                >
                  <TrashIcon size={13} />
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
          style={{ display: 'inline-flex', alignItems: 'center', gap: '6px', fontWeight: 700 }}
        >
          + Thêm danh mục gốc
        </button>
      </div>

      <div className="admin-table-card">
        <div style={{ overflowX: 'auto' }}>
          <table className="admin-table">
            <thead>
              <tr>
                <th>Tên danh mục & Phân cấp</th>
                <th>Slug & Đường dẫn</th>
                <th>Loại hàng</th>
                <th>Kiểm duyệt</th>
                <th>Hoa hồng</th>
                <th>Trạng thái</th>
                <th style={{ textAlign: 'right' }}>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {categoryTree.length === 0 ? (
                <tr>
                  <td colSpan={7} style={{ textAlign: 'center', padding: '3.5rem', color: '#64748b', fontWeight: 500 }}>
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
        <div className="modal-backdrop">
          <div className="admin-modal-card" style={{ width: '90%', maxWidth: '540px', padding: '2rem', borderRadius: '16px', maxHeight: '90vh', overflowY: 'auto' }}>
            <h3 style={{ margin: '0 0 1.25rem', fontWeight: 800, color: '#0f172a' }}>
              {editingCategory ? `Chỉnh sửa: ${editingCategory.name}` : 'Tạo danh mục mới'}
            </h3>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '5px', color: '#334155', fontWeight: 600 }}>
                  Tên danh mục *
                </label>
                <input
                  type="text"
                  className="form-control"
                  placeholder="Ví dụ: Tài khoản Netflix, Bản quyền Windows"
                  value={categoryForm.name}
                  onChange={(e) => setCategoryForm({ ...categoryForm, name: e.target.value })}
                  style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 500 }}
                />
              </div>

              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '5px', color: '#334155', fontWeight: 600 }}>
                  Slug (Đường dẫn tĩnh) - Tùy chọn
                </label>
                <input
                  type="text"
                  className="form-control"
                  placeholder="tai-khoan-netflix (bỏ trống tự tạo)"
                  value={categoryForm.slug}
                  onChange={(e) => setCategoryForm({ ...categoryForm, slug: e.target.value })}
                  style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 500 }}
                />
              </div>

              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '5px', color: '#334155', fontWeight: 600 }}>
                  Danh mục cha (Parent Category)
                </label>
                <select
                  className="form-control"
                  value={categoryForm.parentId}
                  onChange={(e) => setCategoryForm({ ...categoryForm, parentId: e.target.value })}
                  style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 500 }}
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
                  <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '5px', color: '#334155', fontWeight: 600 }}>
                    Loại sản phẩm
                  </label>
                  <select
                    className="form-control"
                    value={categoryForm.productType}
                    onChange={(e) => setCategoryForm({ ...categoryForm, productType: Number(e.target.value) })}
                    style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 500 }}
                  >
                    <option value={1}>Hàng số (Digital)</option>
                    <option value={2}>Dịch vụ (Service)</option>
                  </select>
                </div>

                <div>
                  <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '5px', color: '#334155', fontWeight: 600 }}>
                    Chính sách kiểm duyệt
                  </label>
                  <select
                    className="form-control"
                    value={categoryForm.moderationPolicy}
                    onChange={(e) => setCategoryForm({ ...categoryForm, moderationPolicy: Number(e.target.value) })}
                    style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 500 }}
                  >
                    <option value={1}>Đăng trực tiếp (Direct)</option>
                    <option value={2}>Admin duyệt trước (Review)</option>
                  </select>
                </div>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                <div>
                  <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '5px', color: '#334155', fontWeight: 600 }}>
                    Hoa hồng sàn (Basis Points)
                  </label>
                  <input
                    type="number"
                    className="form-control"
                    placeholder="500 = 5%"
                    value={categoryForm.commissionRateBp}
                    onChange={(e) => setCategoryForm({ ...categoryForm, commissionRateBp: Number(e.target.value) })}
                    style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 500 }}
                  />
                  <span style={{ fontSize: '0.78rem', color: '#64748b' }}>500 = 5%, 800 = 8%</span>
                </div>

                <div>
                  <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '5px', color: '#334155', fontWeight: 600 }}>
                    Thứ tự hiển thị
                  </label>
                  <input
                    type="number"
                    className="form-control"
                    value={categoryForm.displayOrder}
                    onChange={(e) => setCategoryForm({ ...categoryForm, displayOrder: Number(e.target.value) })}
                    style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 500 }}
                  />
                </div>
              </div>

              <div style={{ marginTop: '0.5rem' }}>
                <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '0.88rem', color: '#334155', fontWeight: 600, cursor: 'pointer' }}>
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
              <button type="button" className="btn btn-secondary" onClick={() => setCreateCategoryModal(false)} style={{ fontWeight: 600 }}>
                Hủy
              </button>
              <button type="button" className="btn btn-primary" onClick={handleSaveCategory} style={{ fontWeight: 700 }}>
                {editingCategory ? 'Lưu cập nhật' : 'Tạo danh mục'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* MODAL: ASSIGN ATTRIBUTE TO CATEGORY */}
      {assignAttributeModal && (
        <div className="modal-backdrop">
          <div className="admin-modal-card" style={{ width: '90%', maxWidth: '480px', padding: '2rem', borderRadius: '16px' }}>
            <h3 style={{ margin: '0 0 1rem', fontWeight: 800, color: '#0f172a' }}>
              Gán thuộc tính cho: {assignAttributeModal.category.name}
            </h3>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '5px', color: '#334155', fontWeight: 600 }}>
                  Chọn thuộc tính định nghĩa *
                </label>
                <select
                  className="form-control"
                  value={assignForm.attributeId}
                  onChange={(e) => setAssignForm({ ...assignForm, attributeId: e.target.value })}
                  style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 500 }}
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
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '5px', color: '#334155', fontWeight: 600 }}>
                  Mức độ bắt buộc
                </label>
                <select
                  className="form-control"
                  value={assignForm.requirement}
                  onChange={(e) => setAssignForm({ ...assignForm, requirement: Number(e.target.value) })}
                  style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 500 }}
                >
                  <option value={1}>Bắt buộc điền (Required)</option>
                  <option value={2}>Khuyến nghị điền (Recommended)</option>
                  <option value={3}>Tùy chọn (Optional)</option>
                </select>
              </div>

              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem', marginTop: '0.5rem' }}>
                <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '0.88rem', color: '#334155', fontWeight: 600, cursor: 'pointer' }}>
                  <input
                    type="checkbox"
                    checked={assignForm.isFilterable}
                    onChange={(e) => setAssignForm({ ...assignForm, isFilterable: e.target.checked })}
                  />
                  Cho phép người mua lọc sản phẩm theo thuộc tính này (IsFilterable)
                </label>
                <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '0.88rem', color: '#334155', fontWeight: 600, cursor: 'pointer' }}>
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
              <button type="button" className="btn btn-secondary" onClick={() => setAssignAttributeModal(null)} style={{ fontWeight: 600 }}>
                Đóng
              </button>
              <button type="button" className="btn btn-primary" onClick={handleAssignAttribute} style={{ fontWeight: 700 }}>
                Gán thuộc tính
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
