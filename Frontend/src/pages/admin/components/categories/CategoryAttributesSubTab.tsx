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
          style={{ display: 'inline-flex', alignItems: 'center', gap: '6px' }}
        >
          + Tạo thuộc tính mới
        </button>
      </div>

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

      {/* MODAL: CREATE ATTRIBUTE DEFINITION */}
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

      {/* MODAL: ADD OPTION TO ENUM ATTRIBUTE */}
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
    </div>
  );
};
