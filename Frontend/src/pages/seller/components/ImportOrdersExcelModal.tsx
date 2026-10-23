import React, { useState, useRef, useEffect } from 'react';
import { marketplaceApi } from '@/api/marketplaceApi';
import type {
  ExcelOrderImportPreviewResultDto,
  ExcelOrderGroupPreviewDto,
  ExcelOrderPreviewRowDto,
} from '@/types/marketplace';
import {
  Button,
  Badge,
  Spinner,
  Alert,
  FileTextIcon,
  CheckCircleIcon,
} from '@/components/common';

interface ImportOrdersExcelModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

export const ImportOrdersExcelModal: React.FC<ImportOrdersExcelModalProps> = ({
  isOpen,
  onClose,
  onSuccess,
}) => {
  const [loading, setLoading] = useState(false);
  const [executing, setExecuting] = useState(false);
  const [downloadingTemplate, setDownloadingTemplate] = useState(false);
  const [exportingErrors, setExportingErrors] = useState(false);
  const [preview, setPreview] = useState<ExcelOrderImportPreviewResultDto | null>(null);
  const [skipInvalidOrders, setSkipInvalidOrders] = useState(true);
  const [importResult, setImportResult] = useState<{
    successCount: number;
    failedCount: number;
    createdOrderNos: string[];
    errors: string[];
  } | null>(null);
  const [errorMsg, setErrorMsg] = useState('');

  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (!isOpen) return;
    document.body.classList.add('modal-open');
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !loading && !executing) onClose();
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.body.classList.remove('modal-open');
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [isOpen, onClose, loading, executing]);

  if (!isOpen) return null;

  const handleDownloadTemplate = async () => {
    setDownloadingTemplate(true);
    try {
      const blob = await marketplaceApi.downloadExcelTemplate();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'Mau_Nhap_Don_Hang_Hang_Loat.xlsx';
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (err: any) {
      alert('Không thể tải file mẫu. Vui lòng thử lại sau.');
    } finally {
      setDownloadingTemplate(false);
    }
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0];
    if (!selectedFile) return;

    setErrorMsg('');
    setImportResult(null);
    setLoading(true);

    try {
      const res = await marketplaceApi.previewExcelOrders(selectedFile);
      setPreview(res);
    } catch (err: any) {
      setErrorMsg(err.response?.data?.message || 'Có lỗi khi đọc và phân tích file Excel.');
      setPreview(null);
    } finally {
      setLoading(false);
    }
  };

  const handleExecuteImport = async () => {
    if (!preview) return;

    setExecuting(true);
    setErrorMsg('');

    try {
      const res = await marketplaceApi.executeExcelImport({
        skipInvalidOrders,
        orders: preview.orders,
      });

      setImportResult(res);
      if (res.successCount > 0) {
        onSuccess();
      }
    } catch (err: any) {
      setErrorMsg(err.response?.data?.message || 'Có lỗi xảy ra khi tạo đơn hàng.');
    } finally {
      setExecuting(false);
    }
  };

  const handleExportErrors = async () => {
    if (!preview) return;
    const errorRows = preview.allRows.filter((r) => !r.isValid);
    if (errorRows.length === 0) return;

    setExportingErrors(true);
    try {
      const blob = await marketplaceApi.exportExcelErrors(errorRows);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `Don_Hang_Loi_${new Date().toISOString().slice(0, 10)}.xlsx`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (err: any) {
      alert('Lỗi khi xuất file lỗi vi sai.');
    } finally {
      setExportingErrors(false);
    }
  };

  const resetAll = () => {
    setPreview(null);
    setImportResult(null);
    setErrorMsg('');
    if (fileInputRef.current) fileInputRef.current.value = '';
  };

  return (
    <div
      className="modal-overlay"
      onClick={() => {
        if (!loading && !executing) onClose();
      }}
      role="dialog"
      aria-modal="true"
    >
      <div
        className="modal-content"
        style={{
          maxWidth: '850px',
          width: '100%',
          maxHeight: '90vh',
          display: 'flex',
          flexDirection: 'column',
          borderRadius: '16px',
        }}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Modal Header */}
        <div
          style={{
            padding: '1.25rem 1.75rem',
            borderBottom: '1px solid var(--border-color)',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
          }}
        >
          <div>
            <h3 style={{ margin: 0, fontSize: '1.2rem', fontWeight: 800, color: 'var(--text-main)' }}>
              Nhập Đơn Hàng Bằng Excel / CSV
            </h3>
            <p style={{ margin: '0.2rem 0 0', fontSize: '0.8rem', color: 'var(--text-dim)' }}>
              Hỗ trợ tự động khớp cột thông minh, gom nhóm đơn đa mặt hàng và kiểm tra kho số thực tế
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            style={{
              background: 'none',
              border: 'none',
              fontSize: '1.5rem',
              color: 'var(--text-dim)',
              cursor: 'pointer',
              lineHeight: 1,
            }}
          >
            ×
          </button>
        </div>

        {/* Modal Body */}
        <div style={{ padding: '1.5rem 1.75rem', overflowY: 'auto', flex: 1 }}>
          {errorMsg && <Alert type="error" message={errorMsg} className="mb-4" />}

          {/* TRƯỜNG HỢP: ĐÃ TẠO ĐƠN THÀNH CÔNG */}
          {importResult ? (
            <div style={{ textAlign: 'center', padding: '2rem 1rem' }}>
              <div
                style={{
                  width: '64px',
                  height: '64px',
                  borderRadius: '50%',
                  background: 'rgba(16, 185, 129, 0.15)',
                  color: '#10b981',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  margin: '0 auto 1rem',
                }}
              >
                <CheckCircleIcon size={36} />
              </div>
              <h4 style={{ fontSize: '1.25rem', fontWeight: 800, color: 'var(--text-main)', marginBottom: '0.5rem' }}>
                Đã tạo thành công {importResult.successCount} đơn hàng!
              </h4>
              <p style={{ color: 'var(--text-dim)', fontSize: '0.88rem', maxWidth: '480px', margin: '0 auto 1.5rem' }}>
                Các mã kho số tương ứng đã được tự động trừ tồn kho và chuyển sang trạng thái <strong>Đã giao (Delivered)</strong> an toàn.
              </p>

              {importResult.createdOrderNos.length > 0 && (
                <div
                  style={{
                    background: 'var(--bg-card-subtle, rgba(255,255,255,0.03))',
                    border: '1px solid var(--border-color)',
                    borderRadius: '12px',
                    padding: '1rem',
                    maxHeight: '160px',
                    overflowY: 'auto',
                    textAlign: 'left',
                    fontFamily: 'monospace',
                    fontSize: '0.85rem',
                    marginBottom: '1.5rem',
                  }}
                >
                  <div style={{ fontWeight: 700, color: 'var(--accent-primary)', marginBottom: '0.5rem' }}>
                    Danh sách mã đơn hàng mới:
                  </div>
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.4rem' }}>
                    {importResult.createdOrderNos.map((no) => (
                      <span
                        key={no}
                        style={{
                          background: 'rgba(56, 189, 248, 0.1)',
                          border: '1px solid rgba(56, 189, 248, 0.25)',
                          padding: '2px 8px',
                          borderRadius: '6px',
                          color: '#38bdf8',
                        }}
                      >
                        #{no}
                      </span>
                    ))}
                  </div>
                </div>
              )}

              <div style={{ display: 'flex', justifyContent: 'center', gap: '0.75rem' }}>
                <Button variant="secondary" onClick={resetAll}>
                  Nhập thêm file khác
                </Button>
                <Button variant="primary" onClick={onClose}>
                  Đóng & Xem danh sách đơn
                </Button>
              </div>
            </div>
          ) : !preview ? (
            /* BƯỚC 1: CHƯA UPLOAD HOẶC ĐANG CHỌN FILE */
            <div>
              <div
                style={{
                  border: '2px dashed var(--border-color)',
                  borderRadius: '16px',
                  padding: '2.5rem 1.5rem',
                  textAlign: 'center',
                  background: 'var(--bg-card-subtle, rgba(255,255,255,0.02))',
                  cursor: 'pointer',
                  transition: 'border-color 0.2s',
                }}
                onClick={() => fileInputRef.current?.click()}
              >
                <input
                  ref={fileInputRef}
                  type="file"
                  accept=".xlsx, .xls, .csv"
                  style={{ display: 'none' }}
                  onChange={handleFileChange}
                />
                <FileTextIcon size={44} style={{ color: 'var(--accent-primary)', marginBottom: '0.75rem' }} />
                <div style={{ fontWeight: 700, fontSize: '1rem', color: 'var(--text-main)', marginBottom: '0.25rem' }}>
                  {loading ? 'Đang đọc và đối soát file...' : 'Bấm để chọn hoặc kéo thả file Excel vào đây'}
                </div>
                <div style={{ fontSize: '0.82rem', color: 'var(--text-dim)' }}>
                  Hỗ trợ định dạng Microsoft Excel (.xlsx, .xls) hoặc tệp phân tách (.csv)
                </div>

                {loading && (
                  <div style={{ marginTop: '1.25rem' }}>
                    <Spinner size="sm" />
                    <span style={{ marginLeft: '8px', fontSize: '0.85rem', color: 'var(--accent-primary)' }}>
                      Đang chạy thuật toán kiểm tra kho số...
                    </span>
                  </div>
                )}
              </div>

              {/* Hướng dẫn cột và nút tải file mẫu */}
              <div
                style={{
                  marginTop: '1.5rem',
                  background: 'rgba(255, 255, 255, 0.02)',
                  border: '1px solid var(--border-color)',
                  borderRadius: '12px',
                  padding: '1.25rem',
                }}
              >
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.75rem', flexWrap: 'wrap', gap: '0.5rem' }}>
                  <span style={{ fontWeight: 700, fontSize: '0.88rem', color: 'var(--text-main)' }}>
                    Cấu trúc các cột trong tệp tin Excel:
                  </span>
                  <Button
                    type="button"
                    variant="secondary"
                    size="xs"
                    loading={downloadingTemplate}
                    onClick={handleDownloadTemplate}
                  >
                    📥 Tải file Excel mẫu chuẩn (.xlsx)
                  </Button>
                </div>

                <div style={{ fontSize: '0.82rem', color: 'var(--text-muted)', lineHeight: 1.6 }}>
                  • <strong>Mã đơn (hoặc SĐT/Email)</strong>: Dùng để tự động gom nhiều dòng thành 1 đơn.<br />
                  • <strong>Mã SKU (hoặc Tên sản phẩm)</strong>: Tự động đối soát với danh mục sản phẩm của Shop.<br />
                  • <strong>Số lượng</strong>: Số lượng mã kho cần bán (hệ thống sẽ báo đỏ nếu kho không đủ).<br />
                  • <strong>Đơn giá</strong>: Đơn giá thực bán (để trống sẽ tự lấy giá niêm yết của biến thể).<br />
                  • <strong>Email / Số điện thoại / Tên khách</strong>: Thông tin người nhận đơn.
                </div>
              </div>
            </div>
          ) : (
            /* BƯỚC 2: BẢNG XEM TRƯỚC (PREVIEW) & KIỂM TRA TỒN KHO */
            <div>
              {/* Thống kê nhanh Preview */}
              <div
                style={{
                  display: 'grid',
                  gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))',
                  gap: '0.75rem',
                  marginBottom: '1.25rem',
                }}
              >
                <div style={{ padding: '0.85rem 1rem', background: 'rgba(56, 189, 248, 0.1)', border: '1px solid rgba(56, 189, 248, 0.25)', borderRadius: '12px' }}>
                  <div style={{ fontSize: '0.75rem', color: '#38bdf8', fontWeight: 700 }}>TỔNG SỐ ĐƠN GỘP</div>
                  <div style={{ fontSize: '1.35rem', fontWeight: 800, color: 'var(--text-main)', marginTop: '2px' }}>
                    {preview.totalOrderCount} đơn ({preview.totalRows} dòng)
                  </div>
                </div>

                <div style={{ padding: '0.85rem 1rem', background: 'rgba(16, 185, 129, 0.1)', border: '1px solid rgba(16, 185, 129, 0.25)', borderRadius: '12px' }}>
                  <div style={{ fontSize: '0.75rem', color: '#10b981', fontWeight: 700 }}>HỢP LỆ (ĐỦ KHO SỐ)</div>
                  <div style={{ fontSize: '1.35rem', fontWeight: 800, color: '#10b981', marginTop: '2px' }}>
                    {preview.readyToCreateOrderCount} đơn
                  </div>
                </div>

                <div style={{ padding: '0.85rem 1rem', background: 'rgba(239, 68, 68, 0.1)', border: '1px solid rgba(239, 68, 68, 0.25)', borderRadius: '12px' }}>
                  <div style={{ fontSize: '0.75rem', color: '#ef4444', fontWeight: 700 }}>ĐƠN BỊ LỖI / THIẾU KHO</div>
                  <div style={{ fontSize: '1.35rem', fontWeight: 800, color: '#ef4444', marginTop: '2px' }}>
                    {preview.totalOrderCount - preview.readyToCreateOrderCount} đơn
                  </div>
                </div>
              </div>

              {/* Bảng danh sách đơn hàng đã gom nhóm */}
              <div
                style={{
                  border: '1px solid var(--border-color)',
                  borderRadius: '12px',
                  overflow: 'hidden',
                  maxHeight: '360px',
                  overflowY: 'auto',
                  marginBottom: '1.25rem',
                }}
              >
                <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.82rem' }}>
                  <thead>
                    <tr style={{ background: 'var(--bg-card-subtle, rgba(255,255,255,0.03))', borderBottom: '1px solid var(--border-color)', textAlign: 'left', color: 'var(--text-dim)' }}>
                      <th style={{ padding: '0.65rem 0.75rem' }}>Mã đơn / Khách hàng</th>
                      <th style={{ padding: '0.65rem 0.75rem' }}>Mặt hàng & Số lượng</th>
                      <th style={{ padding: '0.65rem 0.75rem', textAlign: 'right' }}>Tổng tiền</th>
                      <th style={{ padding: '0.65rem 0.75rem', textAlign: 'center' }}>Kiểm tra kho</th>
                    </tr>
                  </thead>
                  <tbody>
                    {preview.orders.map((ord: ExcelOrderGroupPreviewDto, idx: number) => (
                      <tr
                        key={idx}
                        style={{
                          borderBottom: '1px solid var(--border-color)',
                          background: ord.canBeCreated ? 'transparent' : 'rgba(239, 68, 68, 0.04)',
                        }}
                      >
                        <td style={{ padding: '0.65rem 0.75rem' }}>
                          <div style={{ fontWeight: 700, color: ord.canBeCreated ? 'var(--text-main)' : '#ef4444' }}>
                            {ord.orderNo ? `#${ord.orderNo}` : `Đơn #${idx + 1}`}
                          </div>
                          <div style={{ fontSize: '0.75rem', color: 'var(--text-dim)' }}>
                            {ord.customerName || ord.customerEmail || ord.customerPhone || 'Khách vãng lai'}
                          </div>
                        </td>

                        <td style={{ padding: '0.65rem 0.75rem' }}>
                          {ord.items.map((it: ExcelOrderPreviewRowDto, itemIdx: number) => (
                            <div key={itemIdx} style={{ marginBottom: '2px' }}>
                              <span style={{ fontWeight: 600, color: it.isValid ? 'var(--text-main)' : '#ef4444' }}>
                                {it.matchedProductName || it.rawSkuOrProduct}
                              </span>
                              <span style={{ color: 'var(--text-dim)', fontSize: '0.75rem' }}>
                                {' '}({it.matchedVariantLabel || 'Chưa nhận diện'}) x{it.quantity}
                              </span>
                              {!it.isValid && (
                                <div style={{ color: '#ef4444', fontSize: '0.72rem' }}>
                                  ⚠️ {it.errorMessage}
                                </div>
                              )}
                            </div>
                          ))}
                        </td>

                        <td style={{ padding: '0.65rem 0.75rem', textAlign: 'right', fontWeight: 700, color: 'var(--text-main)' }}>
                          {ord.totalAmount.toLocaleString('vi-VN')} đ
                        </td>

                        <td style={{ padding: '0.65rem 0.75rem', textAlign: 'center' }}>
                          {ord.canBeCreated ? (
                            <Badge variant="success" size="sm">Đủ kho sẵn sàng</Badge>
                          ) : (
                            <Badge variant="danger" size="sm">Thiếu kho / Lỗi</Badge>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {/* Tùy chọn xử lý lỗi vi sai */}
              <div
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  flexWrap: 'wrap',
                  gap: '0.75rem',
                  padding: '0.75rem 1rem',
                  background: 'var(--bg-card-subtle, rgba(255,255,255,0.02))',
                  borderRadius: '10px',
                  border: '1px solid var(--border-color)',
                }}
              >
                <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '0.85rem', color: 'var(--text-main)', cursor: 'pointer' }}>
                  <input
                    type="checkbox"
                    checked={skipInvalidOrders}
                    onChange={(e) => setSkipInvalidOrders(e.target.checked)}
                  />
                  <span>Chỉ tạo các đơn hợp lệ (Bỏ qua {preview.totalOrderCount - preview.readyToCreateOrderCount} đơn lỗi)</span>
                </label>

                {preview.invalidRows > 0 && (
                  <Button
                    type="button"
                    variant="danger"
                    size="xs"
                    loading={exportingErrors}
                    onClick={handleExportErrors}
                  >
                    📄 Xuất file {preview.invalidRows} dòng lỗi (.xlsx)
                  </Button>
                )}
              </div>
            </div>
          )}
        </div>

        {/* Modal Footer */}
        <div
          style={{
            padding: '1.25rem 1.75rem',
            borderTop: '1px solid var(--border-color)',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
          }}
        >
          {preview && !importResult ? (
            <Button variant="ghost" size="sm" onClick={resetAll} disabled={executing}>
              Chọn file khác
            </Button>
          ) : (
            <div />
          )}

          <div style={{ display: 'flex', gap: '0.75rem' }}>
            <Button variant="ghost" size="sm" onClick={onClose} disabled={executing}>
              {importResult ? 'Đóng' : 'Hủy bỏ'}
            </Button>

            {preview && !importResult && (
              <Button
                variant="primary"
                size="sm"
                loading={executing}
                disabled={executing || (preview.readyToCreateOrderCount === 0 && skipInvalidOrders)}
                onClick={handleExecuteImport}
              >
                Xác nhận tạo {preview.readyToCreateOrderCount} đơn hàng
              </Button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
