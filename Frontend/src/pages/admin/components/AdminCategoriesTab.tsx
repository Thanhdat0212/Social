import React, { useState, useEffect } from 'react';
import { adminApi } from '@/api/adminApi';
import type {
  AdminCategoryNode,
  AdminAttributeDefinition,
  AdminCategoryRequestItem,
} from '@/types/admin';
import { TrashIcon } from '@/components/common/Icons';


interface AdminCategoriesTabProps {
  showFlash: (text: string, type?: 'success' | 'error') => void;
}

export const AdminCategoriesTab: React.FC<AdminCategoriesTabProps> = ({ showFlash }) => {
  const [subTab, setSubTab] = useState<'tree' | 'attributes' | 'requests'>('tree');
  const [loading, setLoading] = useState<boolean>(false);

  // Data states
  const [categoryTree, setCategoryTree] = useState<AdminCategoryNode[]>([]);
  const [attributes, setAttributes] = useState<AdminAttributeDefinition[]>([]);
  const [requests, setRequests] = useState<AdminCategoryRequestItem[]>([]);
  const [requestFilter, setRequestFilter] = useState<string>('Pending');

  // Modals state
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

  // Assign attribute modal
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

  // Create attribute modal
  const [createAttributeModal, setCreateAttributeModal] = useState<boolean>(false);
  const [attributeForm, setAttributeForm] = useState({
    code: '',
    name: '',
    dataType: 1, // 1: Enum, 2: Number, 3: Text, 4: Boolean
    unit: '',
    min: '',
    max: '',
    initialOptions: '',
  });

  // Add enum option modal
  const [addOptionModal, setAddOptionModal] = useState<{
    isOpen: boolean;
    attributeId: string;
    attributeName: string;
    value: string;
  } | null>(null);

  // Review request modal
  const [reviewRequestModal, setReviewRequestModal] = useState<{
    isOpen: boolean;
    request: AdminCategoryRequestItem;
    isApproved: boolean;
    resolvedCategoryId: string;
    adminNote: string;
  } | null>(null);

  // Loaders
  const loadCategoryTree = async () => {
    try {
      setLoading(true);
      const res = await adminApi.getCategoriesTree();
      setCategoryTree(res || []);
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Không thể tải cây danh mục.', 'error');
    } finally {
      setLoading(false);
    }
  };

  const loadAttributes = async () => {
    try {
      setLoading(true);
      const res = await adminApi.getAttributes();
      setAttributes(res || []);
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Không thể tải danh sách thuộc tính.', 'error');
    } finally {
      setLoading(false);
    }
  };

  const loadRequests = async () => {
    try {
      setLoading(true);
      const statusParam = requestFilter === 'Pending' ? 1 : requestFilter === 'Approved' ? 2 : requestFilter === 'Rejected' ? 3 : undefined;
      const res = await adminApi.getCategoryRequests({ status: statusParam, pageSize: 50 });
      setRequests(res.items || []);
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Không thể tải danh sách yêu cầu mở danh mục.', 'error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (subTab === 'tree') {
      loadCategoryTree();
    } else if (subTab === 'attributes') {
      loadAttributes();
    } else if (subTab === 'requests') {
      loadRequests();
    }
  }, [subTab, requestFilter]);

  // Flatten category tree for select dropdown and table render
  const flattenCategories = (nodes: AdminCategoryNode[], result: AdminCategoryNode[] = []): AdminCategoryNode[] => {
    for (const node of nodes) {
      result.push(node);
      if (node.children && node.children.length > 0) {
        flattenCategories(node.children, result);
      }
    }
    return result;
  };

  const allFlatCategories = flattenCategories(categoryTree);

  // Handlers for Category CRUD
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
      parentId: node.parentId || '',
      displayOrder: node.displayOrder,
      productType: node.productType === 'Service' ? 2 : 1,
      moderationPolicy: node.moderationPolicy === 'RequiresReview' ? 2 : 1,
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

  // Handlers for Attribute Assignment
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

  // Handlers for Attribute Definition
  const handleCreateAttribute = async () => {
    if (!attributeForm.code.trim() || !attributeForm.name.trim()) {
      alert('Vui lòng nhập mã và tên thuộc tính.');
      return;
    }
    try {
      const initialOptions = attributeForm.dataType === 1 && attributeForm.initialOptions.trim()
        ? attributeForm.initialOptions.split(',').map((s) => s.trim()).filter(Boolean)
        : undefined;

      await adminApi.createAttribute({
        code: attributeForm.code.trim().toLowerCase(),
        name: attributeForm.name.trim(),
        dataType: Number(attributeForm.dataType),
        unit: attributeForm.unit.trim() || undefined,
        min: attributeForm.min ? Number(attributeForm.min) : undefined,
        max: attributeForm.max ? Number(attributeForm.max) : undefined,
        initialOptions,
      });
      showFlash('Tạo thuộc tính mới thành công!');
      setCreateAttributeModal(false);
      loadAttributes();
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Lỗi khi tạo thuộc tính.', 'error');
    }
  };

  const handleAddOption = async () => {
    if (!addOptionModal || !addOptionModal.value.trim()) return;
    try {
      await adminApi.addAttributeOption(addOptionModal.attributeId, {
        value: addOptionModal.value.trim(),
      });
      showFlash(`Đã thêm tùy chọn "${addOptionModal.value.trim()}"!`);
      setAddOptionModal(null);
      loadAttributes();
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Lỗi khi thêm tùy chọn.', 'error');
    }
  };

  const handleDeleteOption = async (optionId: string, val: string) => {
    if (!window.confirm(`Xóa tùy chọn "${val}"?`)) return;
    try {
      await adminApi.deleteAttributeOption(optionId);
      showFlash(`Đã xóa tùy chọn "${val}"!`);
      loadAttributes();
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Lỗi khi xóa tùy chọn.', 'error');
    }
  };

  // Handlers for Review Category Request
  const handleConfirmReviewRequest = async () => {
    if (!reviewRequestModal) return;
    try {
      await adminApi.reviewCategoryRequest(reviewRequestModal.request.id, {
        isApproved: reviewRequestModal.isApproved,
        resolvedCategoryId: reviewRequestModal.resolvedCategoryId || undefined,
        adminNote: reviewRequestModal.adminNote.trim() || undefined,
      });
      showFlash(reviewRequestModal.isApproved ? 'Đã phê duyệt yêu cầu mở danh mục!' : 'Đã từ chối yêu cầu.');
      setReviewRequestModal(null);
      loadRequests();
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Lỗi khi xử lý yêu cầu.', 'error');
    }
  };

  return (
    <div>
      {/* Sub-navigation Tabs */}
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: '1.5rem',
          flexWrap: 'wrap',
          gap: '1rem',
        }}
      >
        <div style={{ display: 'flex', gap: '0.5rem', background: 'rgba(255,255,255,0.03)', padding: '4px', borderRadius: '12px' }}>
          <button
            type="button"
            className={`btn btn-sm ${subTab === 'tree' ? 'btn-primary' : 'btn-ghost'}`}
            onClick={() => setSubTab('tree')}
          >
            🌳 Cây danh mục ({allFlatCategories.length})
          </button>
          <button
            type="button"
            className={`btn btn-sm ${subTab === 'attributes' ? 'btn-primary' : 'btn-ghost'}`}
            onClick={() => setSubTab('attributes')}
          >
            ⚙️ Từ điển thuộc tính ({attributes.length})
          </button>
          <button
            type="button"
            className={`btn btn-sm ${subTab === 'requests' ? 'btn-primary' : 'btn-ghost'}`}
            onClick={() => setSubTab('requests')}
          >
            📬 Yêu cầu từ Shop
          </button>
        </div>

        {subTab === 'tree' && (
          <button
            type="button"
            className="btn btn-primary btn-sm"
            onClick={() => handleOpenCreateCategory()}
            style={{ display: 'inline-flex', alignItems: 'center', gap: '6px' }}
          >
            + Thêm danh mục gốc
          </button>
        )}

        {subTab === 'attributes' && (
          <button
            type="button"
            className="btn btn-primary btn-sm"
            onClick={() => {
              setAttributeForm({
                code: '',
                name: '',
                dataType: 1,
                unit: '',
                min: '',
                max: '',
                initialOptions: '',
              });
              setCreateAttributeModal(true);
            }}
            style={{ display: 'inline-flex', alignItems: 'center', gap: '6px' }}
          >
            + Tạo thuộc tính mới
          </button>
        )}
      </div>

      {loading && (
        <div style={{ padding: '2rem', textAlign: 'center' }}>
          <div className="spinner" style={{ margin: '0 auto 0.5rem' }} />
          <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>Đang nạp dữ liệu danh mục & thuộc tính...</p>
        </div>
      )}


      {/* VIEW 1: CATEGORY TREE */}
      {subTab === 'tree' && (
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
                {allFlatCategories.length === 0 ? (
                  <tr>
                    <td colSpan={7} style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-muted)' }}>
                      Chưa có danh mục nào trên hệ thống. Hãy bấm &quot;Thêm danh mục gốc&quot; để khởi tạo.
                    </td>
                  </tr>
                ) : (
                  allFlatCategories.map((node) => {
                    const indent = node.depth * 24;
                    return (
                      <tr
                        key={node.id}
                        style={{
                          borderBottom: '1px solid rgba(255,255,255,0.05)',
                          transition: 'background 0.2s',
                        }}
                      >
                        <td style={{ padding: '0.85rem 1.25rem' }}>
                          <div style={{ display: 'flex', alignItems: 'center', paddingLeft: `${indent}px` }}>
                            {node.depth > 0 && (
                              <span style={{ color: 'var(--text-muted)', marginRight: '8px', fontSize: '1rem' }}>
                                ↳
                              </span>
                            )}
                            <span style={{ fontWeight: node.depth === 0 ? 700 : 500, color: node.depth === 0 ? 'var(--text-primary)' : 'var(--text-secondary)' }}>
                              {node.name}
                            </span>
                            <span
                              style={{
                                marginLeft: '8px',
                                fontSize: '0.7rem',
                                padding: '2px 6px',
                                borderRadius: '4px',
                                background: 'rgba(255,255,255,0.06)',
                                color: 'var(--text-muted)',
                              }}
                            >
                              Cấp {node.depth}
                            </span>
                          </div>
                        </td>
                        <td style={{ padding: '0.85rem 1.25rem', fontFamily: 'monospace', fontSize: '0.82rem', color: 'var(--text-muted)' }}>
                          {node.path || node.slug}
                        </td>
                        <td style={{ padding: '0.85rem 1.25rem' }}>
                          <span
                            style={{
                              fontSize: '0.75rem',
                              padding: '2px 8px',
                              borderRadius: '6px',
                              fontWeight: 600,
                              background: node.productType === 'Service' ? 'rgba(56, 189, 248, 0.15)' : 'rgba(168, 85, 247, 0.15)',
                              color: node.productType === 'Service' ? '#38bdf8' : '#c084fc',
                            }}
                          >
                            {node.productType === 'Service' ? 'Dịch vụ' : 'Hàng số'}
                          </span>
                        </td>
                        <td style={{ padding: '0.85rem 1.25rem' }}>
                          <span
                            style={{
                              fontSize: '0.75rem',
                              padding: '2px 8px',
                              borderRadius: '6px',
                              fontWeight: 600,
                              background: node.moderationPolicy === 'RequiresReview' ? 'rgba(245, 158, 11, 0.15)' : 'rgba(34, 197, 94, 0.15)',
                              color: node.moderationPolicy === 'RequiresReview' ? '#f59e0b' : '#22c55e',
                            }}
                          >
                            {node.moderationPolicy === 'RequiresReview' ? 'Cần duyệt trước' : 'Đăng trực tiếp'}
                          </span>
                        </td>
                        <td style={{ padding: '0.85rem 1.25rem', fontWeight: 600, color: '#38bdf8' }}>
                          {node.commissionRateBp != null ? `${(node.commissionRateBp / 100).toFixed(1)}%` : 'Mặc định (5%)'}
                        </td>
                        <td style={{ padding: '0.85rem 1.25rem' }}>
                          <span
                            style={{
                              fontSize: '0.75rem',
                              padding: '2px 8px',
                              borderRadius: '6px',
                              fontWeight: 600,
                              background: node.isActive ? 'rgba(34, 197, 94, 0.15)' : 'rgba(239, 68, 68, 0.15)',
                              color: node.isActive ? '#22c55e' : '#ef4444',
                            }}
                          >
                            {node.isActive ? 'Đang bật' : 'Tạm ẩn'}
                          </span>
                        </td>
                        <td style={{ padding: '0.85rem 1.25rem', textAlign: 'right' }}>
                          <div style={{ display: 'inline-flex', gap: '6px' }}>
                            <button
                              type="button"
                              className="btn btn-secondary btn-sm"
                              title="Tạo danh mục con"
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
                    );
                  })
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* VIEW 2: ATTRIBUTE DEFINITIONS */}
      {subTab === 'attributes' && (
        <div className="card-glass" style={{ borderRadius: '16px', overflow: 'hidden', border: '1px solid var(--border-color)' }}>
          <div style={{ overflowX: 'auto' }}>
            <table className="table" style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.9rem' }}>
              <thead>
                <tr style={{ background: 'rgba(255,255,255,0.03)', borderBottom: '1px solid var(--border-color)', textAlign: 'left' }}>
                  <th style={{ padding: '1rem 1.25rem' }}>Mã thuộc tính</th>
                  <th style={{ padding: '1rem 1.25rem' }}>Tên hiển thị</th>
                  <th style={{ padding: '1rem 1.25rem' }}>Kiểu dữ liệu</th>
                  <th style={{ padding: '1rem 1.25rem' }}>Đơn vị / Giới hạn</th>
                  <th style={{ padding: '1rem 1.25rem' }}>Các tùy chọn (Options)</th>
                  <th style={{ padding: '1rem 1.25rem', textAlign: 'right' }}>Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {attributes.length === 0 ? (
                  <tr>
                    <td colSpan={6} style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-muted)' }}>
                      Chưa có định nghĩa thuộc tính nào. Bấm &quot;Tạo thuộc tính mới&quot; để thiết lập.
                    </td>
                  </tr>
                ) : (
                  attributes.map((attr) => (
                    <tr key={attr.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                      <td style={{ padding: '0.85rem 1.25rem', fontFamily: 'monospace', fontWeight: 600, color: '#38bdf8' }}>
                        {attr.code}
                      </td>
                      <td style={{ padding: '0.85rem 1.25rem', fontWeight: 600 }}>
                        {attr.name}
                      </td>
                      <td style={{ padding: '0.85rem 1.25rem' }}>
                        <span
                          style={{
                            fontSize: '0.75rem',
                            padding: '2px 8px',
                            borderRadius: '6px',
                            background: 'rgba(255,255,255,0.08)',
                            color: 'var(--text-secondary)',
                          }}
                        >
                          {attr.dataType}
                        </span>
                      </td>
                      <td style={{ padding: '0.85rem 1.25rem', color: 'var(--text-muted)' }}>
                        {attr.unit ? `Đơn vị: ${attr.unit}` : ''}
                        {attr.min != null || attr.max != null ? ` [${attr.min ?? '-∞'} .. ${attr.max ?? '+∞'}]` : ''}
                        {!attr.unit && attr.min == null && attr.max == null ? '-' : ''}
                      </td>
                      <td style={{ padding: '0.85rem 1.25rem' }}>
                        {attr.dataType === 'Enum' ? (
                          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '6px', alignItems: 'center' }}>
                            {attr.options && attr.options.length > 0 ? (
                              attr.options.map((opt) => (
                                <span
                                  key={opt.id}
                                  style={{
                                    display: 'inline-flex',
                                    alignItems: 'center',
                                    gap: '4px',
                                    padding: '2px 8px',
                                    background: 'rgba(168, 85, 247, 0.15)',
                                    color: '#c084fc',
                                    borderRadius: '6px',
                                    fontSize: '0.75rem',
                                  }}
                                >
                                  {opt.value}
                                  <button
                                    type="button"
                                    onClick={() => handleDeleteOption(opt.id, opt.value)}
                                    style={{
                                      background: 'none',
                                      border: 'none',
                                      color: 'rgba(239, 68, 68, 0.8)',
                                      cursor: 'pointer',
                                      padding: '0 2px',
                                      fontSize: '0.7rem',
                                      lineHeight: 1,
                                    }}
                                    title="Xóa tùy chọn này"
                                  >
                                    ×
                                  </button>
                                </span>
                              ))
                            ) : (
                              <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>Chưa có options</span>
                            )}
                            <button
                              type="button"
                              className="btn btn-secondary btn-sm"
                              onClick={() => setAddOptionModal({ isOpen: true, attributeId: attr.id, attributeName: attr.name, value: '' })}
                              style={{ padding: '2px 6px', fontSize: '0.7rem' }}
                            >
                              + Thêm
                            </button>
                          </div>
                        ) : (
                          <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Nhập tự do ({attr.dataType})</span>
                        )}
                      </td>
                      <td style={{ padding: '0.85rem 1.25rem', textAlign: 'right' }}>
                        <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>Hệ thống</span>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* VIEW 3: CATEGORY REQUESTS */}
      {subTab === 'requests' && (
        <div>
          <div style={{ display: 'flex', gap: '0.75rem', marginBottom: '1.25rem', alignItems: 'center' }}>
            <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Lọc trạng thái:</span>
            {['Pending', 'Approved', 'Rejected', ''].map((st) => (
              <button
                key={st || 'All'}
                type="button"
                className={`btn btn-sm ${requestFilter === st ? 'btn-primary' : 'btn-secondary'}`}
                onClick={() => setRequestFilter(st)}
              >
                {st === 'Pending' ? 'Chờ duyệt' : st === 'Approved' ? 'Đã duyệt' : st === 'Rejected' ? 'Bị từ chối' : 'Tất cả'}
              </button>
            ))}
          </div>

          <div className="card-glass" style={{ borderRadius: '16px', overflow: 'hidden', border: '1px solid var(--border-color)' }}>
            <div style={{ overflowX: 'auto' }}>
              <table className="table" style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.9rem' }}>
                <thead>
                  <tr style={{ background: 'rgba(255,255,255,0.03)', borderBottom: '1px solid var(--border-color)', textAlign: 'left' }}>
                    <th style={{ padding: '1rem 1.25rem' }}>Gian hàng</th>
                    <th style={{ padding: '1rem 1.25rem' }}>Tên danh mục đề xuất</th>
                    <th style={{ padding: '1rem 1.25rem' }}>Danh mục cha gợi ý</th>
                    <th style={{ padding: '1rem 1.25rem' }}>Lý do / Thuyết minh</th>
                    <th style={{ padding: '1rem 1.25rem' }}>Trạng thái</th>
                    <th style={{ padding: '1rem 1.25rem' }}>Thời gian</th>
                    <th style={{ padding: '1rem 1.25rem', textAlign: 'right' }}>Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  {requests.length === 0 ? (
                    <tr>
                      <td colSpan={7} style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-muted)' }}>
                        Không có yêu cầu mở danh mục nào.
                      </td>
                    </tr>
                  ) : (
                    requests.map((req) => (
                      <tr key={req.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                        <td style={{ padding: '0.85rem 1.25rem', fontWeight: 600 }}>{req.shopName}</td>
                        <td style={{ padding: '0.85rem 1.25rem', color: '#c084fc', fontWeight: 600 }}>{req.name}</td>
                        <td style={{ padding: '0.85rem 1.25rem', color: 'var(--text-muted)' }}>
                          {req.suggestedParentName || '(Gốc)'}
                        </td>
                        <td style={{ padding: '0.85rem 1.25rem', maxWidth: '280px', color: 'var(--text-secondary)' }}>
                          {req.reasonNote || 'Không có mô tả'}
                          {req.adminNote && (
                            <div style={{ fontSize: '0.75rem', color: 'var(--accent-rose)', marginTop: '4px' }}>
                              Phản hồi: {req.adminNote}
                            </div>
                          )}
                        </td>
                        <td style={{ padding: '0.85rem 1.25rem' }}>
                          <span
                            style={{
                              fontSize: '0.75rem',
                              padding: '2px 8px',
                              borderRadius: '6px',
                              fontWeight: 600,
                              background:
                                req.status === 'Approved'
                                  ? 'rgba(34, 197, 94, 0.15)'
                                  : req.status === 'Rejected'
                                  ? 'rgba(239, 68, 68, 0.15)'
                                  : 'rgba(245, 158, 11, 0.15)',
                              color:
                                req.status === 'Approved'
                                  ? '#22c55e'
                                  : req.status === 'Rejected'
                                  ? '#ef4444'
                                  : '#f59e0b',
                            }}
                          >
                            {req.status === 'Approved' ? 'Đã duyệt' : req.status === 'Rejected' ? 'Từ chối' : 'Chờ duyệt'}
                          </span>
                        </td>
                        <td style={{ padding: '0.85rem 1.25rem', fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                          {new Date(req.createdAtUtc).toLocaleDateString('vi-VN')}
                        </td>
                        <td style={{ padding: '0.85rem 1.25rem', textAlign: 'right' }}>
                          {req.status === 'Pending' && (
                            <div style={{ display: 'inline-flex', gap: '6px' }}>
                              <button
                                type="button"
                                className="btn btn-primary btn-sm"
                                onClick={() =>
                                  setReviewRequestModal({
                                    isOpen: true,
                                    request: req,
                                    isApproved: true,
                                    resolvedCategoryId: '',
                                    adminNote: '',
                                  })
                                }
                                style={{ padding: '4px 8px', fontSize: '0.75rem' }}
                              >
                                Duyệt
                              </button>
                              <button
                                type="button"
                                className="btn btn-danger btn-sm"
                                onClick={() =>
                                  setReviewRequestModal({
                                    isOpen: true,
                                    request: req,
                                    isApproved: false,
                                    resolvedCategoryId: '',
                                    adminNote: '',
                                  })
                                }
                                style={{ padding: '4px 8px', fontSize: '0.75rem' }}
                              >
                                Từ chối
                              </button>
                            </div>
                          )}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* MODAL 1: CREATE / EDIT CATEGORY */}
      {createCategoryModal && (
        <div className="modal-backdrop" style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.7)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
          <div className="card-glass" style={{ width: '90%', maxWidth: '540px', padding: '2rem', borderRadius: '16px', border: '1px solid var(--border-color)', maxHeight: '90vh', overflowY: 'auto' }}>
            <h3 style={{ margin: '0 0 1.25rem', fontWeight: 700 }}>
              {editingCategory ? `Chỉnh sửa: ${editingCategory.name}` : 'Tạo danh mục mới'}
            </h3>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                  Danh mục cha (để trống nếu là danh mục gốc)
                </label>
                <select
                  className="form-control"
                  value={categoryForm.parentId}
                  onChange={(e) => setCategoryForm({ ...categoryForm, parentId: e.target.value })}
                  disabled={!!editingCategory} // Không đổi parent trực tiếp qua update thông thường
                >
                  <option value="">-- Danh mục gốc (Cấp 0) --</option>
                  {allFlatCategories
                    .filter((c) => !editingCategory || c.id !== editingCategory.id)
                    .map((c) => (
                      <option key={c.id} value={c.id}>
                        {'- '.repeat(c.depth)} {c.name}
                      </option>
                    ))}
                </select>
              </div>

              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                  Tên danh mục *
                </label>
                <input
                  type="text"
                  className="form-control"
                  placeholder="Ví dụ: Tài khoản Netflix, VPS Cloud..."
                  value={categoryForm.name}
                  onChange={(e) => setCategoryForm({ ...categoryForm, name: e.target.value })}
                />
              </div>

              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                  Slug (tùy chọn, để trống sẽ tự sinh)
                </label>
                <input
                  type="text"
                  className="form-control"
                  placeholder="tai-khoan-netflix"
                  value={categoryForm.slug}
                  onChange={(e) => setCategoryForm({ ...categoryForm, slug: e.target.value })}
                />
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
                    <option value={1}>Đăng trực tiếp</option>
                    <option value={2}>Cần duyệt trước</option>
                  </select>
                </div>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                <div>
                  <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                    Hoa hồng sàn (bps, ví dụ 500 = 5%)
                  </label>
                  <input
                    type="number"
                    className="form-control"
                    placeholder="500"
                    value={categoryForm.commissionRateBp}
                    onChange={(e) => setCategoryForm({ ...categoryForm, commissionRateBp: Number(e.target.value) })}
                  />
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

              {editingCategory && (
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginTop: '0.5rem' }}>
                  <input
                    type="checkbox"
                    id="catIsActive"
                    checked={categoryForm.isActive}
                    onChange={(e) => setCategoryForm({ ...categoryForm, isActive: e.target.checked })}
                  />
                  <label htmlFor="catIsActive" style={{ fontSize: '0.9rem', cursor: 'pointer' }}>
                    Cho phép hoạt động (hiển thị trên chợ)
                  </label>
                </div>
              )}
            </div>

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.5rem' }}>
              <button type="button" className="btn btn-secondary" onClick={() => setCreateCategoryModal(false)}>
                Hủy
              </button>
              <button type="button" className="btn btn-primary" onClick={handleSaveCategory}>
                {editingCategory ? 'Lưu thay đổi' : 'Tạo danh mục'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* MODAL 2: ASSIGN ATTRIBUTE TO CATEGORY */}
      {assignAttributeModal && (
        <div className="modal-backdrop" style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.7)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
          <div className="card-glass" style={{ width: '90%', maxWidth: '480px', padding: '2rem', borderRadius: '16px', border: '1px solid var(--border-color)' }}>
            <h3 style={{ margin: '0 0 0.5rem', fontWeight: 700 }}>
              Cấu hình thuộc tính danh mục
            </h3>
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.85rem', marginBottom: '1.25rem' }}>
              Danh mục: <strong>{assignAttributeModal.category.name}</strong>
            </p>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                  Chọn thuộc tính từ từ điển *
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

      {/* MODAL 3: CREATE ATTRIBUTE DEFINITION */}
      {createAttributeModal && (
        <div className="modal-backdrop" style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.7)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
          <div className="card-glass" style={{ width: '90%', maxWidth: '500px', padding: '2rem', borderRadius: '16px', border: '1px solid var(--border-color)' }}>
            <h3 style={{ margin: '0 0 1.25rem', fontWeight: 700 }}>Tạo thuộc tính mới</h3>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                  Mã thuộc tính (Code) *
                </label>
                <input
                  type="text"
                  className="form-control"
                  placeholder="Ví dụ: region, duration, subscription_tier"
                  value={attributeForm.code}
                  onChange={(e) => setAttributeForm({ ...attributeForm, code: e.target.value })}
                />
              </div>

              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                  Tên hiển thị (Name) *
                </label>
                <input
                  type="text"
                  className="form-control"
                  placeholder="Ví dụ: Khu vực / Quốc gia, Thời hạn gói"
                  value={attributeForm.name}
                  onChange={(e) => setAttributeForm({ ...attributeForm, name: e.target.value })}
                />
              </div>

              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                  Kiểu dữ liệu (Data Type)
                </label>
                <select
                  className="form-control"
                  value={attributeForm.dataType}
                  onChange={(e) => setAttributeForm({ ...attributeForm, dataType: Number(e.target.value) })}
                >
                  <option value={1}>Enum (Danh sách chọn trước)</option>
                  <option value={2}>Number (Con số)</option>
                  <option value={3}>Text (Chuỗi văn bản)</option>
                  <option value={4}>Boolean (Đúng / Sai)</option>
                </select>
              </div>

              {attributeForm.dataType === 1 && (
                <div>
                  <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                    Các tùy chọn ban đầu (phân tách bằng dấu phẩy)
                  </label>
                  <input
                    type="text"
                    className="form-control"
                    placeholder="Ví dụ: Việt Nam, Thổ Nhĩ Kỳ, Toàn cầu, Mỹ"
                    value={attributeForm.initialOptions}
                    onChange={(e) => setAttributeForm({ ...attributeForm, initialOptions: e.target.value })}
                  />
                </div>
              )}

              {attributeForm.dataType === 2 && (
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '0.5rem' }}>
                  <div>
                    <label style={{ display: 'block', fontSize: '0.75rem', marginBottom: '2px', color: 'var(--text-muted)' }}>
                      Đơn vị tính
                    </label>
                    <input
                      type="text"
                      className="form-control"
                      placeholder="tháng, GB"
                      value={attributeForm.unit}
                      onChange={(e) => setAttributeForm({ ...attributeForm, unit: e.target.value })}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', fontSize: '0.75rem', marginBottom: '2px', color: 'var(--text-muted)' }}>
                      Tối thiểu
                    </label>
                    <input
                      type="number"
                      className="form-control"
                      value={attributeForm.min}
                      onChange={(e) => setAttributeForm({ ...attributeForm, min: e.target.value })}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', fontSize: '0.75rem', marginBottom: '2px', color: 'var(--text-muted)' }}>
                      Tối đa
                    </label>
                    <input
                      type="number"
                      className="form-control"
                      value={attributeForm.max}
                      onChange={(e) => setAttributeForm({ ...attributeForm, max: e.target.value })}
                    />
                  </div>
                </div>
              )}
            </div>

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.5rem' }}>
              <button type="button" className="btn btn-secondary" onClick={() => setCreateAttributeModal(false)}>
                Hủy
              </button>
              <button type="button" className="btn btn-primary" onClick={handleCreateAttribute}>
                Tạo thuộc tính
              </button>
            </div>
          </div>
        </div>
      )}

      {/* MODAL 4: ADD OPTION TO ENUM ATTRIBUTE */}
      {addOptionModal && (
        <div className="modal-backdrop" style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.7)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
          <div className="card-glass" style={{ width: '90%', maxWidth: '400px', padding: '1.75rem', borderRadius: '16px', border: '1px solid var(--border-color)' }}>
            <h4 style={{ margin: '0 0 1rem', fontWeight: 700 }}>
              Thêm tùy chọn cho: {addOptionModal.attributeName}
            </h4>
            <div style={{ marginBottom: '1.25rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                Giá trị tùy chọn *
              </label>
              <input
                type="text"
                className="form-control"
                placeholder="Ví dụ: 1 Tháng, 12 Tháng, Ultra HD..."
                value={addOptionModal.value}
                onChange={(e) => setAddOptionModal({ ...addOptionModal, value: e.target.value })}
                autoFocus
              />
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
              <button type="button" className="btn btn-secondary" onClick={() => setAddOptionModal(null)}>Hủy</button>
              <button type="button" className="btn btn-primary" onClick={handleAddOption}>Lưu tùy chọn</button>
            </div>
          </div>
        </div>
      )}

      {/* MODAL 5: REVIEW CATEGORY REQUEST */}
      {reviewRequestModal && (
        <div className="modal-backdrop" style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.7)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
          <div className="card-glass" style={{ width: '90%', maxWidth: '480px', padding: '2rem', borderRadius: '16px', border: '1px solid var(--border-color)' }}>
            <h3 style={{ margin: '0 0 1rem', fontWeight: 700, color: reviewRequestModal.isApproved ? '#22c55e' : '#ef4444' }}>
              {reviewRequestModal.isApproved ? 'Phê duyệt yêu cầu mở danh mục' : 'Từ chối yêu cầu mở danh mục'}
            </h3>
            <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '1.25rem' }}>
              Gian hàng: <strong>{reviewRequestModal.request.shopName}</strong> | Đề xuất: <strong>{reviewRequestModal.request.name}</strong>
            </p>

            {reviewRequestModal.isApproved && (
              <div style={{ marginBottom: '1rem' }}>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                  Gán vào danh mục thực tế (Resolved Category)
                </label>
                <select
                  className="form-control"
                  value={reviewRequestModal.resolvedCategoryId}
                  onChange={(e) => setReviewRequestModal({ ...reviewRequestModal, resolvedCategoryId: e.target.value })}
                >
                  <option value="">-- Chọn danh mục đã tồn tại nếu có --</option>
                  {allFlatCategories.map((c) => (
                    <option key={c.id} value={c.id}>
                      {'- '.repeat(c.depth)} {c.name}
                    </option>
                  ))}
                </select>
                <p style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '4px' }}>
                  Nếu đây là danh mục hoàn toàn mới, hãy vào tab Cây danh mục tạo trước rồi chọn ở đây.
                </p>
              </div>
            )}

            <div style={{ marginBottom: '1.25rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                Ghi chú gửi cho Gian hàng {reviewRequestModal.isApproved ? '(Tùy chọn)' : '(Bắt buộc lý do)'}
              </label>
              <textarea
                className="form-control"
                rows={3}
                placeholder={reviewRequestModal.isApproved ? 'Danh mục đã được khởi tạo và sẵn sàng đăng bán.' : 'Lý do từ chối...'}
                value={reviewRequestModal.adminNote}
                onChange={(e) => setReviewRequestModal({ ...reviewRequestModal, adminNote: e.target.value })}
              />
            </div>

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
              <button type="button" className="btn btn-secondary" onClick={() => setReviewRequestModal(null)}>Hủy</button>
              <button
                type="button"
                className={`btn ${reviewRequestModal.isApproved ? 'btn-primary' : 'btn-danger'}`}
                onClick={handleConfirmReviewRequest}
              >
                Xác nhận
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
