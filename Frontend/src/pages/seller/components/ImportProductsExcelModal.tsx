import React, { useState, useRef } from 'react';
import { marketplaceApi } from '@/api/marketplaceApi';
import type {
  ExcelProductImportPreviewResultDto,
} from '@/types/marketplace';
import {
  Button,
  Badge,
  Spinner,
  Alert,
  FileTextIcon,
  CheckCircleIcon,
} from '@/components/common';

interface ImportProductsExcelModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

export const ImportProductsExcelModal: React.FC<ImportProductsExcelModalProps> = ({
  isOpen,
  onClose,
  onSuccess,
}) => {
  const [loading, setLoading] = useState(false);
  const [executing, setExecuting] = useState(false);
  const [downloadingTemplate, setDownloadingTemplate] = useState(false);
  const [exportingErrors, setExportingErrors] = useState(false);
  const [preview, setPreview] = useState<ExcelProductImportPreviewResultDto | null>(null);
  const [skipInvalidProducts, setSkipInvalidProducts] = useState(true);
  const [viewMode, setViewMode] = useState<'grouped' | 'allRows'>('grouped');
  const [importResult, setImportResult] = useState<{
    successCount: number;
    failedCount: number;
    createdProductNames: string[];
    errors: string[];
  } | null>(null);
  const [errorMsg, setErrorMsg] = useState('');

  const fileInputRef = useRef<HTMLInputElement>(null);

  if (!isOpen) return null;

  const handleDownloadTemplate = async () => {
    setDownloadingTemplate(true);
    try {
      const blob = await marketplaceApi.downloadProductExcelTemplate();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'Mau_Nhap_San_Pham_Hang_Loat.xlsx';
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
      const res = await marketplaceApi.previewExcelProducts(selectedFile);
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
      const res = await marketplaceApi.executeExcelProductsImport({
        skipInvalidProducts,
        products: preview.products,
      });

      setImportResult(res);
      if (res.successCount > 0) {
        onSuccess();
      }
    } catch (err: any) {
      setErrorMsg(err.response?.data?.message || 'Có lỗi xảy ra khi tạo sản phẩm hàng loạt.');
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
      const blob = await marketplaceApi.exportExcelProductErrors(errorRows);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `San_Pham_Loi_${new Date().toISOString().slice(0, 10)}.xlsx`;
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
      style={{
        position: 'fixed',
        inset: 0,
        background: 'rgba(0, 0, 0, 0.75)',
        backdropFilter: 'blur(6px)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        zIndex: 1000,
        padding: '1.5rem',
      }}
    >
      <div
        style={{
          background: 'var(--bg-card)',
          border: '1px solid var(--border-color)',
          borderRadius: '16px',
          maxWidth: '920px',
          width: '100%',
          maxHeight: '90vh',
          display: 'flex',
          flexDirection: 'column',
          boxShadow: '0 25px 50px -12px rgba(0, 0, 0, 0.5)',
        }}
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
              Nhập Sản Phẩm Mới Bằng Excel
            </h3>
            <p style={{ margin: '0.2rem 0 0', fontSize: '0.8rem', color: 'var(--text-dim)' }}>
              Hỗ trợ tự động phân tích sản phẩm, gom nhóm biến thể, khớp danh mục thông minh và nạp kho số ban đầu
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

          {/* TRƯỜNG HỢP: ĐÃ TẠO SẢN PHẨM THÀNH CÔNG */}
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
                Đã Hoàn Tất Nhập Sản Phẩm Hàng Loạt!
              </h4>
              <p style={{ color: 'var(--text-dim)', fontSize: '0.9rem', maxWidth: '480px', margin: '0 auto 1.5rem' }}>
                Đã tạo thành công <strong style={{ color: '#10b981' }}>{importResult.successCount}</strong> sản phẩm mới vào gian hàng của bạn.
                {importResult.failedCount > 0 && (
                  <span style={{ color: '#ef4444', display: 'block', marginTop: '0.25rem' }}>
                    Có {importResult.failedCount} sản phẩm bị bỏ qua do lỗi.
                  </span>
                )}
              </p>

              {importResult.createdProductNames && importResult.createdProductNames.length > 0 && (
                <div
                  style={{
                    background: 'var(--bg-main)',
                    borderRadius: '12px',
                    padding: '1rem',
                    textAlign: 'left',
                    maxHeight: '180px',
                    overflowY: 'auto',
                    marginBottom: '1.5rem',
                    border: '1px solid var(--border-color)',
                  }}
                >
                  <p style={{ margin: '0 0 0.5rem', fontSize: '0.8rem', fontWeight: 700, color: 'var(--text-dim)' }}>
                    DANH SÁCH SẢN PHẨM VỪA TẠO:
                  </p>
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem' }}>
                    {importResult.createdProductNames.map((name, i) => (
                      <span
                        key={i}
                        style={{
                          background: 'rgba(59, 130, 246, 0.1)',
                          color: '#3b82f6',
                          border: '1px solid rgba(59, 130, 246, 0.3)',
                          padding: '0.2rem 0.6rem',
                          borderRadius: '6px',
                          fontSize: '0.8rem',
                          fontWeight: 600,
                        }}
                      >
                        {name}
                      </span>
                    ))}
                  </div>
                </div>
              )}

              <div style={{ display: 'flex', justifyContent: 'center', gap: '1rem' }}>
                <Button variant="outline" onClick={resetAll}>
                  Tiếp tục nhập thêm tệp khác
                </Button>
                <Button variant="primary" onClick={onClose}>
                  Đóng & Xem danh sách sản phẩm
                </Button>
              </div>
            </div>
          ) : !preview ? (
            /* TRƯỜNG HỢP: CHƯA CHỌN TỆP EXCEL */
            <div>
              {/* Box Hướng dẫn & Tải mẫu */}
              <div
                style={{
                  background: 'rgba(59, 130, 246, 0.05)',
                  border: '1px dashed rgba(59, 130, 246, 0.4)',
                  borderRadius: '12px',
                  padding: '1.25rem',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  marginBottom: '1.5rem',
                }}
              >
                <div>
                  <h4 style={{ margin: '0 0 0.25rem', fontSize: '0.95rem', fontWeight: 700, color: 'var(--text-main)' }}>
                    Chưa có tệp Excel mẫu chuẩn?
                  </h4>
                  <p style={{ margin: 0, fontSize: '0.8rem', color: 'var(--text-dim)' }}>
                    Tải mẫu Excel chuẩn có sẵn ví dụ điền dữ liệu, danh mục và hướng dẫn chi tiết.
                  </p>
                </div>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleDownloadTemplate}
                  disabled={downloadingTemplate}
                  style={{ whiteSpace: 'nowrap' }}
                >
                  {downloadingTemplate ? <Spinner size="sm" /> : <FileTextIcon size={16} />}
                  <span style={{ marginLeft: '0.5rem' }}>Tải file mẫu (.xlsx)</span>
                </Button>
              </div>

              {/* Khu vực Drag & Drop */}
              <div
                onClick={() => fileInputRef.current?.click()}
                style={{
                  border: '2px dashed var(--border-color)',
                  borderRadius: '16px',
                  padding: '3rem 2rem',
                  textAlign: 'center',
                  cursor: 'pointer',
                  transition: 'all 0.2s ease',
                  background: 'var(--bg-main)',
                }}
                onDragOver={(e) => {
                  e.preventDefault();
                  e.currentTarget.style.borderColor = 'var(--primary-color, #3b82f6)';
                  e.currentTarget.style.background = 'rgba(59, 130, 246, 0.04)';
                }}
                onDragLeave={(e) => {
                  e.preventDefault();
                  e.currentTarget.style.borderColor = 'var(--border-color)';
                  e.currentTarget.style.background = 'var(--bg-main)';
                }}
                onDrop={(e) => {
                  e.preventDefault();
                  e.currentTarget.style.borderColor = 'var(--border-color)';
                  e.currentTarget.style.background = 'var(--bg-main)';
                  if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
                    if (fileInputRef.current) {
                      fileInputRef.current.files = e.dataTransfer.files;
                      handleFileChange({ target: { files: e.dataTransfer.files } } as any);
                    }
                  }
                }}
              >
                <input
                  type="file"
                  ref={fileInputRef}
                  onChange={handleFileChange}
                  accept=".xlsx, .xls, .csv"
                  style={{ display: 'none' }}
                />

                {loading ? (
                  <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '0.75rem' }}>
                    <Spinner size="lg" />
                    <p style={{ margin: 0, fontWeight: 600, color: 'var(--text-main)' }}>
                      Đang phân tích cú pháp và kiểm thử dữ liệu file Excel...
                    </p>
                    <p style={{ margin: 0, fontSize: '0.8rem', color: 'var(--text-dim)' }}>
                      Kiểm tra SKU, đơn giá, khớp mờ danh mục và gom nhóm biến thể
                    </p>
                  </div>
                ) : (
                  <div>
                    <div
                      style={{
                        width: '56px',
                        height: '56px',
                        borderRadius: '50%',
                        background: 'rgba(59, 130, 246, 0.1)',
                        color: '#3b82f6',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        margin: '0 auto 1rem',
                      }}
                    >
                      <FileTextIcon size={28} />
                    </div>
                    <h4 style={{ margin: '0 0 0.25rem', fontSize: '1rem', fontWeight: 700, color: 'var(--text-main)' }}>
                      Kéo & Thả file Excel vào đây hoặc click để chọn
                    </h4>
                    <p style={{ margin: 0, fontSize: '0.8rem', color: 'var(--text-dim)' }}>
                      Hỗ trợ các định dạng .xlsx, .xls, .csv (Tối đa 500 dòng sản phẩm)
                    </p>
                  </div>
                )}
              </div>
            </div>
          ) : (
            /* TRƯỜNG HỢP: ĐÃ PHÂN TÍCH VÀ CÓ DỮ LIỆU PREVIEW */
            <div>
              {/* Thẻ tóm tắt thông số kiểm thử */}
              <div
                style={{
                  display: 'grid',
                  gridTemplateColumns: 'repeat(auto-fit, minmax(130px, 1fr))',
                  gap: '0.75rem',
                  marginBottom: '1rem',
                }}
              >
                <div style={{ background: 'var(--bg-main)', padding: '0.75rem 1rem', borderRadius: '10px', border: '1px solid var(--border-color)' }}>
                  <div style={{ fontSize: '0.75rem', color: 'var(--text-dim)' }}>Tổng số dòng</div>
                  <div style={{ fontSize: '1.25rem', fontWeight: 800, color: 'var(--text-main)' }}>{preview.totalRows}</div>
                </div>

                <div style={{ background: 'var(--bg-main)', padding: '0.75rem 1rem', borderRadius: '10px', border: '1px solid var(--border-color)' }}>
                  <div style={{ fontSize: '0.75rem', color: 'var(--text-dim)' }}>Số sản phẩm</div>
                  <div style={{ fontSize: '1.25rem', fontWeight: 800, color: '#3b82f6' }}>{preview.totalProductCount}</div>
                </div>

                <div style={{ background: 'rgba(16, 185, 129, 0.08)', padding: '0.75rem 1rem', borderRadius: '10px', border: '1px solid rgba(16, 185, 129, 0.3)' }}>
                  <div style={{ fontSize: '0.75rem', color: '#10b981' }}>Dòng hợp lệ</div>
                  <div style={{ fontSize: '1.25rem', fontWeight: 800, color: '#10b981' }}>{preview.validRows}</div>
                </div>

                <div style={{ background: preview.invalidRows > 0 ? 'rgba(239, 68, 68, 0.08)' : 'var(--bg-main)', padding: '0.75rem 1rem', borderRadius: '10px', border: `1px solid ${preview.invalidRows > 0 ? 'rgba(239, 68, 68, 0.3)' : 'var(--border-color)'}` }}>
                  <div style={{ fontSize: '0.75rem', color: preview.invalidRows > 0 ? '#ef4444' : 'var(--text-dim)' }}>Dòng lỗi</div>
                  <div style={{ fontSize: '1.25rem', fontWeight: 800, color: preview.invalidRows > 0 ? '#ef4444' : 'var(--text-dim)' }}>{preview.invalidRows}</div>
                </div>
              </div>

              {/* Thanh điều khiển hiển thị */}
              <div
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  marginBottom: '0.75rem',
                  flexWrap: 'wrap',
                  gap: '0.5rem',
                }}
              >
                <div style={{ display: 'flex', gap: '0.5rem' }}>
                  <button
                    type="button"
                    onClick={() => setViewMode('grouped')}
                    style={{
                      padding: '0.35rem 0.75rem',
                      borderRadius: '6px',
                      fontSize: '0.8rem',
                      fontWeight: 700,
                      border: '1px solid var(--border-color)',
                      background: viewMode === 'grouped' ? 'var(--primary-color, #3b82f6)' : 'var(--bg-main)',
                      color: viewMode === 'grouped' ? '#fff' : 'var(--text-main)',
                      cursor: 'pointer',
                    }}
                  >
                    Xem theo sản phẩm ({preview.totalProductCount})
                  </button>
                  <button
                    type="button"
                    onClick={() => setViewMode('allRows')}
                    style={{
                      padding: '0.35rem 0.75rem',
                      borderRadius: '6px',
                      fontSize: '0.8rem',
                      fontWeight: 700,
                      border: '1px solid var(--border-color)',
                      background: viewMode === 'allRows' ? 'var(--primary-color, #3b82f6)' : 'var(--bg-main)',
                      color: viewMode === 'allRows' ? '#fff' : 'var(--text-main)',
                      cursor: 'pointer',
                    }}
                  >
                    Xem chi tiết từng dòng ({preview.totalRows})
                  </button>
                </div>

                <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                  {preview.invalidRows > 0 && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={handleExportErrors}
                      disabled={exportingErrors}
                      style={{ color: '#ef4444', borderColor: '#ef4444' }}
                    >
                      {exportingErrors ? <Spinner size="sm" /> : null}
                      <span style={{ marginLeft: exportingErrors ? '0.35rem' : 0 }}>
                        Tải file {preview.invalidRows} dòng lỗi (.xlsx)
                      </span>
                    </Button>
                  )}
                  <Button variant="ghost" size="sm" onClick={resetAll}>
                    Chọn file khác
                  </Button>
                </div>
              </div>

              {/* Bảng Preview dữ liệu */}
              <div
                style={{
                  border: '1px solid var(--border-color)',
                  borderRadius: '10px',
                  overflow: 'hidden',
                  maxHeight: '340px',
                  overflowY: 'auto',
                  background: 'var(--bg-main)',
                }}
              >
                {viewMode === 'grouped' ? (
                  /* BẢNG THEO SẢN PHẨM GOM NHÓM */
                  <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.8rem' }}>
                    <thead>
                      <tr style={{ background: 'var(--bg-card)', borderBottom: '1px solid var(--border-color)', textAlign: 'left' }}>
                        <th style={{ padding: '0.6rem 0.75rem', fontWeight: 700 }}>Tên sản phẩm</th>
                        <th style={{ padding: '0.6rem 0.75rem', fontWeight: 700 }}>Danh mục</th>
                        <th style={{ padding: '0.6rem 0.75rem', fontWeight: 700 }}>Biến thể & Giá</th>
                        <th style={{ padding: '0.6rem 0.75rem', fontWeight: 700 }}>Kho mã</th>
                        <th style={{ padding: '0.6rem 0.75rem', fontWeight: 700 }}>Trạng thái</th>
                      </tr>
                    </thead>
                    <tbody>
                      {preview.products.map((prod, idx) => (
                        <tr
                          key={idx}
                          style={{
                            borderBottom: '1px solid var(--border-color)',
                            background: prod.canBeCreated ? 'transparent' : 'rgba(239, 68, 68, 0.04)',
                          }}
                        >
                          <td style={{ padding: '0.6rem 0.75rem', verticalAlign: 'top', fontWeight: 700, color: 'var(--text-main)' }}>
                            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                              {prod.imageUrl ? (
                                <img
                                  src={prod.imageUrl}
                                  alt=""
                                  style={{ width: '28px', height: '28px', borderRadius: '4px', objectFit: 'cover' }}
                                />
                              ) : null}
                              <span>{prod.productName}</span>
                            </div>
                            {prod.publishImmediate && (
                              <span style={{ fontSize: '0.7rem', color: '#10b981', fontWeight: 600 }}>• Đăng bán ngay</span>
                            )}
                          </td>
                          <td style={{ padding: '0.6rem 0.75rem', verticalAlign: 'top' }}>
                            {prod.matchedCategoryName ? (
                              <Badge variant={prod.matchedCategoryId ? 'info' : 'warning'}>
                                {prod.matchedCategoryName}
                              </Badge>
                            ) : (
                              <span style={{ color: 'var(--text-dim)' }}>Chưa chọn</span>
                            )}
                          </td>
                          <td style={{ padding: '0.6rem 0.75rem', verticalAlign: 'top' }}>
                            {prod.variants.map((v, vIdx) => (
                              <div key={vIdx} style={{ marginBottom: '0.2rem' }}>
                                <span style={{ fontWeight: 600 }}>{v.variantName}</span>:{' '}
                                <strong style={{ color: '#10b981' }}>{v.price.toLocaleString('vi-VN')} đ</strong>
                                {v.sku && <span style={{ color: 'var(--text-dim)', marginLeft: '0.35rem' }}>({v.sku})</span>}
                              </div>
                            ))}
                          </td>
                          <td style={{ padding: '0.6rem 0.75rem', verticalAlign: 'top' }}>
                            {prod.variants.reduce((acc, v) => acc + (v.initialCodes?.length || 0), 0) > 0 ? (
                              <span style={{ color: '#3b82f6', fontWeight: 600 }}>
                                {prod.variants.reduce((acc, v) => acc + (v.initialCodes?.length || 0), 0)} mã
                              </span>
                            ) : (
                              <span style={{ color: 'var(--text-dim)' }}>0 mã</span>
                            )}
                          </td>
                          <td style={{ padding: '0.6rem 0.75rem', verticalAlign: 'top' }}>
                            {prod.canBeCreated ? (
                              <Badge variant="success">Hợp lệ</Badge>
                            ) : (
                              <div>
                                <Badge variant="danger">Lỗi</Badge>
                                <div style={{ fontSize: '0.7rem', color: '#ef4444', marginTop: '0.25rem' }}>
                                  {prod.blockingErrors.join(', ')}
                                </div>
                              </div>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                ) : (
                  /* BẢNG THEO TỪNG DÒNG EXCEL */
                  <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.8rem' }}>
                    <thead>
                      <tr style={{ background: 'var(--bg-card)', borderBottom: '1px solid var(--border-color)', textAlign: 'left' }}>
                        <th style={{ padding: '0.6rem 0.75rem', fontWeight: 700, width: '50px' }}>Dòng</th>
                        <th style={{ padding: '0.6rem 0.75rem', fontWeight: 700 }}>Tên sản phẩm</th>
                        <th style={{ padding: '0.6rem 0.75rem', fontWeight: 700 }}>Tên gói / SKU</th>
                        <th style={{ padding: '0.6rem 0.75rem', fontWeight: 700 }}>Giá bán</th>
                        <th style={{ padding: '0.6rem 0.75rem', fontWeight: 700 }}>Danh mục</th>
                        <th style={{ padding: '0.6rem 0.75rem', fontWeight: 700 }}>Trạng thái / Lỗi</th>
                      </tr>
                    </thead>
                    <tbody>
                      {preview.allRows.map((row) => (
                        <tr
                          key={row.rowIndex}
                          style={{
                            borderBottom: '1px solid var(--border-color)',
                            background: row.isValid ? 'transparent' : 'rgba(239, 68, 68, 0.05)',
                          }}
                        >
                          <td style={{ padding: '0.6rem 0.75rem', color: 'var(--text-dim)', verticalAlign: 'top' }}>
                            #{row.rowIndex}
                          </td>
                          <td style={{ padding: '0.6rem 0.75rem', verticalAlign: 'top', fontWeight: 600, color: 'var(--text-main)' }}>
                            {row.productName || row.rawProductName || <span style={{ color: '#ef4444' }}>[Trống]</span>}
                          </td>
                          <td style={{ padding: '0.6rem 0.75rem', verticalAlign: 'top' }}>
                            <div>{row.variantName || 'Mặc định'}</div>
                            {row.sku ? (
                              <div style={{ fontSize: '0.7rem', color: 'var(--text-dim)' }}>{row.sku}</div>
                            ) : (
                              <div style={{ fontSize: '0.7rem', color: 'var(--text-dim)', fontStyle: 'italic' }}>Tự sinh SKU</div>
                            )}
                          </td>
                          <td style={{ padding: '0.6rem 0.75rem', verticalAlign: 'top', fontWeight: 700, color: row.price > 0 ? '#10b981' : '#ef4444' }}>
                            {row.price > 0 ? `${row.price.toLocaleString('vi-VN')} đ` : (row.rawPrice || '0')}
                          </td>
                          <td style={{ padding: '0.6rem 0.75rem', verticalAlign: 'top' }}>
                            <span style={{ fontSize: '0.75rem' }}>{row.matchedCategoryName || row.rawCategoryName || '-'}</span>
                          </td>
                          <td style={{ padding: '0.6rem 0.75rem', verticalAlign: 'top' }}>
                            {row.isValid ? (
                              <Badge variant="success">Hợp lệ</Badge>
                            ) : (
                              <div style={{ color: '#ef4444', fontSize: '0.75rem' }}>
                                <strong>Lỗi: </strong>{row.errorMessage}
                              </div>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </div>

              {/* Tùy chọn bỏ qua dòng lỗi */}
              {preview.invalidRows > 0 && (
                <div style={{ marginTop: '0.75rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                  <input
                    type="checkbox"
                    id="skipInvalidProducts"
                    checked={skipInvalidProducts}
                    onChange={(e) => setSkipInvalidProducts(e.target.checked)}
                    style={{ cursor: 'pointer', width: '16px', height: '16px' }}
                  />
                  <label htmlFor="skipInvalidProducts" style={{ fontSize: '0.85rem', color: 'var(--text-main)', cursor: 'pointer' }}>
                    Bỏ qua {preview.invalidRows} dòng bị lỗi và chỉ tạo <strong>{preview.readyToCreateProductCount}</strong> sản phẩm hợp lệ
                  </label>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Modal Footer */}
        {preview && !importResult && (
          <div
            style={{
              padding: '1rem 1.75rem',
              borderTop: '1px solid var(--border-color)',
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
            }}
          >
            <Button variant="ghost" onClick={onClose} disabled={executing}>
              Hủy bỏ
            </Button>
            <div style={{ display: 'flex', gap: '0.75rem' }}>
              <Button
                variant="primary"
                onClick={handleExecuteImport}
                disabled={executing || (preview.readyToCreateProductCount === 0 && skipInvalidProducts)}
              >
                {executing ? <Spinner size="sm" /> : null}
                <span style={{ marginLeft: executing ? '0.5rem' : 0 }}>
                  Xác nhận tạo {skipInvalidProducts ? preview.readyToCreateProductCount : preview.totalProductCount} sản phẩm
                </span>
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
