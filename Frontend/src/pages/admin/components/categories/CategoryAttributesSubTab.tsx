import React, { useState } from 'react';
import { adminApi } from '@/api/adminApi';
import type { AdminAttributeDefinition } from '@/types/admin';

interface CategoryAttributesSubTabProps {
  attributes: AdminAttributeDefinition[];
  loadAttributes: () => Promise<void>;
  showFlash: (text: string, type?: 'success' | 'error') => void;
}

export const CategoryAttributesSubTab: React.FC<CategoryAttributesSubTabProps> = ({
  attributes,
  loadAttributes,
  showFlash,
}) => {
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

  const [addOptionModal, setAddOptionModal] = useState<{
    isOpen: boolean;
    attributeId: string;
    attributeName: string;
    value: string;
  } | null>(null);

  const handleCreateAttribute = async () => {
    if (!attributeForm.code.trim() || !attributeForm.name.trim()) {
      alert('Vui lòng nhập mã và tên thuộc tính.');
      return;
    }
    try {
      const initialOptions =
        attributeForm.dataType === 1 && attributeForm.initialOptions.trim()
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

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: '1rem' }}>
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
          style={{ display: 'inline-flex', alignItems: 'center', gap: '6px', fontWeight: 700 }}
        >
          + Tạo thuộc tính mới
        </button>
      </div>

      <div className="admin-table-card">
        <div style={{ overflowX: 'auto' }}>
          <table className="admin-table">
            <thead>
              <tr>
                <th>Mã thuộc tính</th>
                <th>Tên hiển thị</th>
                <th>Kiểu dữ liệu</th>
                <th>Đơn vị / Giới hạn</th>
                <th>Các tùy chọn (Options)</th>
                <th style={{ textAlign: 'right' }}>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {attributes.length === 0 ? (
                <tr>
                  <td colSpan={6} style={{ textAlign: 'center', padding: '3.5rem', color: '#64748b', fontWeight: 500 }}>
                    Chưa có định nghĩa thuộc tính nào. Bấm &quot;Tạo thuộc tính mới&quot; để thiết lập.
                  </td>
                </tr>
              ) : (
                attributes.map((attr) => (
                  <tr key={attr.id}>
                    <td style={{ padding: '0.95rem 1.25rem', fontFamily: 'monospace', fontWeight: 700, color: '#0284c7' }}>
                      {attr.code}
                    </td>
                    <td style={{ padding: '0.95rem 1.25rem', fontWeight: 700, color: '#0f172a' }}>
                      {attr.name}
                    </td>
                    <td style={{ padding: '0.95rem 1.25rem' }}>
                      <span className="admin-pill-badge admin-pill-blue">
                        {attr.dataType}
                      </span>
                    </td>
                    <td style={{ padding: '0.95rem 1.25rem', color: '#475569', fontWeight: 500 }}>
                      {attr.unit ? `Đơn vị: ${attr.unit}` : ''}
                      {attr.min != null || attr.max != null ? ` [${attr.min ?? '-∞'} .. ${attr.max ?? '+∞'}]` : ''}
                      {!attr.unit && attr.min == null && attr.max == null ? '-' : ''}
                    </td>
                    <td style={{ padding: '0.95rem 1.25rem' }}>
                      {attr.dataType === 'Enum' ? (
                        <div style={{ display: 'flex', flexWrap: 'wrap', gap: '6px', alignItems: 'center' }}>
                          {attr.options && attr.options.length > 0 ? (
                            attr.options.map((opt) => (
                              <span
                                key={opt.id}
                                className="admin-pill-badge admin-pill-purple"
                                style={{
                                  display: 'inline-flex',
                                  alignItems: 'center',
                                  gap: '4px',
                                  padding: '2px 8px',
                                  fontSize: '0.78rem',
                                }}
                              >
                                {opt.value}
                                <button
                                  type="button"
                                  onClick={() => handleDeleteOption(opt.id, opt.value)}
                                  style={{
                                    background: 'none',
                                    border: 'none',
                                    color: '#b91c1c',
                                    cursor: 'pointer',
                                    padding: '0 2px',
                                    fontSize: '0.85rem',
                                    fontWeight: 700,
                                    lineHeight: 1,
                                  }}
                                  title="Xóa tùy chọn này"
                                >
                                  ×
                                </button>
                              </span>
                            ))
                          ) : (
                            <span style={{ fontSize: '0.82rem', color: '#64748b' }}>Chưa có options</span>
                          )}
                          <button
                            type="button"
                            className="btn btn-secondary btn-sm"
                            onClick={() => setAddOptionModal({ isOpen: true, attributeId: attr.id, attributeName: attr.name, value: '' })}
                            style={{ padding: '2px 8px', fontSize: '0.75rem', fontWeight: 600 }}
                          >
                            + Thêm
                          </button>
                        </div>
                      ) : (
                        <span style={{ fontSize: '0.85rem', color: '#64748b', fontWeight: 500 }}>Nhập tự do ({attr.dataType})</span>
                      )}
                    </td>
                    <td style={{ padding: '0.95rem 1.25rem', textAlign: 'right' }}>
                      <span style={{ fontSize: '0.78rem', color: '#64748b', fontWeight: 600 }}>Hệ thống</span>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* MODAL: CREATE ATTRIBUTE DEFINITION */}
      {createAttributeModal && (
        <div className="modal-backdrop">
          <div className="admin-modal-card" style={{ width: '90%', maxWidth: '500px', padding: '2rem', borderRadius: '16px' }}>
            <h3 style={{ margin: '0 0 1.25rem', fontWeight: 800, color: '#0f172a' }}>Tạo thuộc tính mới</h3>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '5px', color: '#334155', fontWeight: 600 }}>
                  Mã thuộc tính (Code) *
                </label>
                <input
                  type="text"
                  className="form-control"
                  placeholder="Ví dụ: region, duration, subscription_tier"
                  value={attributeForm.code}
                  onChange={(e) => setAttributeForm({ ...attributeForm, code: e.target.value })}
                  style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 500 }}
                />
              </div>

              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '5px', color: '#334155', fontWeight: 600 }}>
                  Tên hiển thị (Name) *
                </label>
                <input
                  type="text"
                  className="form-control"
                  placeholder="Ví dụ: Khu vực / Quốc gia, Thời hạn gói"
                  value={attributeForm.name}
                  onChange={(e) => setAttributeForm({ ...attributeForm, name: e.target.value })}
                  style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 500 }}
                />
              </div>

              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '5px', color: '#334155', fontWeight: 600 }}>
                  Kiểu dữ liệu (Data Type)
                </label>
                <select
                  className="form-control"
                  value={attributeForm.dataType}
                  onChange={(e) => setAttributeForm({ ...attributeForm, dataType: Number(e.target.value) })}
                  style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 500 }}
                >
                  <option value={1}>Enum (Danh sách chọn trước)</option>
                  <option value={2}>Number (Con số)</option>
                  <option value={3}>Text (Chuỗi văn bản)</option>
                  <option value={4}>Boolean (Đúng / Sai)</option>
                </select>
              </div>

              {attributeForm.dataType === 1 && (
                <div>
                  <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '5px', color: '#334155', fontWeight: 600 }}>
                    Các tùy chọn ban đầu (phân tách bằng dấu phẩy)
                  </label>
                  <input
                    type="text"
                    className="form-control"
                    placeholder="Ví dụ: Việt Nam, Thổ Nhĩ Kỳ, Toàn cầu, Mỹ"
                    value={attributeForm.initialOptions}
                    onChange={(e) => setAttributeForm({ ...attributeForm, initialOptions: e.target.value })}
                    style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 500 }}
                  />
                </div>
              )}

              {attributeForm.dataType === 2 && (
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '0.5rem' }}>
                  <div>
                    <label style={{ display: 'block', fontSize: '0.78rem', marginBottom: '3px', color: '#334155', fontWeight: 600 }}>
                      Đơn vị tính
                    </label>
                    <input
                      type="text"
                      className="form-control"
                      placeholder="tháng, GB"
                      value={attributeForm.unit}
                      onChange={(e) => setAttributeForm({ ...attributeForm, unit: e.target.value })}
                      style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 500 }}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', fontSize: '0.78rem', marginBottom: '3px', color: '#334155', fontWeight: 600 }}>
                      Tối thiểu
                    </label>
                    <input
                      type="number"
                      className="form-control"
                      value={attributeForm.min}
                      onChange={(e) => setAttributeForm({ ...attributeForm, min: e.target.value })}
                      style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 500 }}
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', fontSize: '0.78rem', marginBottom: '3px', color: '#334155', fontWeight: 600 }}>
                      Tối đa
                    </label>
                    <input
                      type="number"
                      className="form-control"
                      value={attributeForm.max}
                      onChange={(e) => setAttributeForm({ ...attributeForm, max: e.target.value })}
                      style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 500 }}
                    />
                  </div>
                </div>
              )}
            </div>

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.5rem' }}>
              <button type="button" className="btn btn-secondary" onClick={() => setCreateAttributeModal(false)} style={{ fontWeight: 600 }}>
                Hủy
              </button>
              <button type="button" className="btn btn-primary" onClick={handleCreateAttribute} style={{ fontWeight: 700 }}>
                Tạo thuộc tính
              </button>
            </div>
          </div>
        </div>
      )}

      {/* MODAL: ADD OPTION TO ENUM ATTRIBUTE */}
      {addOptionModal && (
        <div className="modal-backdrop">
          <div className="admin-modal-card" style={{ width: '90%', maxWidth: '400px', padding: '1.75rem', borderRadius: '16px' }}>
            <h4 style={{ margin: '0 0 1rem', fontWeight: 800, color: '#0f172a' }}>
              Thêm tùy chọn cho: {addOptionModal.attributeName}
            </h4>
            <div style={{ marginBottom: '1.25rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '5px', color: '#334155', fontWeight: 600 }}>
                Giá trị tùy chọn *
              </label>
              <input
                type="text"
                className="form-control"
                placeholder="Ví dụ: 1 Tháng, 12 Tháng, Ultra HD..."
                value={addOptionModal.value}
                onChange={(e) => setAddOptionModal({ ...addOptionModal, value: e.target.value })}
                autoFocus
                style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 500 }}
              />
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
              <button type="button" className="btn btn-secondary" onClick={() => setAddOptionModal(null)} style={{ fontWeight: 600 }}>Hủy</button>
              <button type="button" className="btn btn-primary" onClick={handleAddOption} style={{ fontWeight: 700 }}>Lưu tùy chọn</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
