import React, { useState } from 'react';
import { marketplaceApi } from '@/api/marketplaceApi';
import { FormField, Input, Textarea, Button, Alert, StoreIcon } from '@/components/common';

const generateSlug = (str: string): string => {
  return str
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[đĐ]/g, 'd')
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9\s-]/g, '')
    .replace(/[\s-]+/g, '-')
    .replace(/^-+|-+$/g, '');
};

const SLUG_REGEX = /^[a-z0-9]+(-[a-z0-9]+)*$/;

interface RegisterShopFormProps {
  onSuccess: (shop: any) => void;
}

export const RegisterShopForm: React.FC<RegisterShopFormProps> = ({ onSuccess }) => {
  const [newShopName, setNewShopName] = useState('');
  const [newShopSlug, setNewShopSlug] = useState('');
  const [isSlugManuallyEdited, setIsSlugManuallyEdited] = useState(false);
  const [newShopEmail, setNewShopEmail] = useState('');
  const [newShopDesc, setNewShopDesc] = useState('');
  const [registering, setRegistering] = useState(false);
  const [registerError, setRegisterError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<{
    name?: string;
    slug?: string;
    email?: string;
  }>({});

  const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value;
    setNewShopName(val);
    if (!isSlugManuallyEdited) {
      setNewShopSlug(generateSlug(val));
    }
    if (fieldErrors.name) setFieldErrors((prev) => ({ ...prev, name: undefined }));
  };

  const handleSlugChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setIsSlugManuallyEdited(true);
    setNewShopSlug(e.target.value.toLowerCase());
    if (fieldErrors.slug) setFieldErrors((prev) => ({ ...prev, slug: undefined }));
  };

  const handleRegisterShop = async (e: React.FormEvent) => {
    e.preventDefault();
    setRegisterError(null);

    const errors: typeof fieldErrors = {};
    const cleanName = newShopName.trim();
    const cleanSlug = newShopSlug.trim();
    const cleanEmail = newShopEmail.trim();

    if (!cleanName) errors.name = 'Vui lòng nhập tên gian hàng.';
    else if (cleanName.length < 3) errors.name = 'Tên gian hàng tối thiểu 3 ký tự.';

    if (!cleanSlug) errors.slug = 'Vui lòng nhập đường dẫn tĩnh (slug).';
    else if (!SLUG_REGEX.test(cleanSlug)) errors.slug = 'Slug chỉ gồm chữ thường (a-z), số (0-9) và dấu gạch ngang (-).';

    if (!cleanEmail) errors.email = 'Vui lòng nhập email nhận thông báo.';

    if (Object.keys(errors).length > 0) {
      setFieldErrors(errors);
      return;
    }

    setRegistering(true);
    setFieldErrors({});

    try {
      const res = await marketplaceApi.createShop({
        name: cleanName,
        slug: cleanSlug,
        contactEmail: cleanEmail,
        description: newShopDesc.trim() || undefined,
      });
      onSuccess(res);
    } catch (err: any) {
      const data = err?.response?.data;
      const errorMsg = data?.detail || data?.message || 'Không thể tạo gian hàng. Vui lòng kiểm tra lại thông tin.';
      setRegisterError(errorMsg);
    } finally {
      setRegistering(false);
    }
  };

  return (
    <div
      style={{
        maxWidth: '560px',
        margin: '2rem auto 4rem',
        background: 'var(--bg-card)',
        border: '1px solid var(--border-color)',
        borderRadius: '20px',
        padding: '2.5rem 2rem',
        boxShadow: 'var(--shadow-lg)',
      }}
    >
      <div style={{ textAlign: 'center', marginBottom: '2rem' }}>
        <div
          style={{
            width: '56px',
            height: '56px',
            borderRadius: '16px',
            background: 'var(--gradient-brand)',
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: '#fff',
            marginBottom: '1rem',
          }}
        >
          <StoreIcon size={28} />
        </div>
        <h1 style={{ fontSize: '1.5rem', fontWeight: 800, color: 'var(--text-main)', marginBottom: '0.5rem', letterSpacing: '-0.02em' }}>
          Mở gian hàng kinh doanh số
        </h1>
        <p style={{ color: 'var(--text-muted)', fontSize: '0.88rem', lineHeight: '1.5', maxWidth: '460px', margin: '0 auto' }}>
          Bắt đầu kinh doanh bản quyền phần mềm, khóa học, key kích hoạt và tài nguyên số. Hệ thống tự động giao mã 24/7 và đối soát minh bạch.
        </p>
      </div>

      <form onSubmit={handleRegisterShop} noValidate>
        <div style={{ marginBottom: '1.5rem' }}>
          <div style={{ fontSize: '0.78rem', fontWeight: 700, color: 'var(--text-dim)', textTransform: 'uppercase', letterSpacing: '0.05em', marginBottom: '0.85rem' }}>
            Thông tin gian hàng
          </div>

          <FormField id="shop-name-input" label="Tên gian hàng" required error={fieldErrors.name} hint="Tên hiển thị công khai trên sàn Chợ số.">
            <Input
              id="shop-name-input"
              placeholder="VD: DevStore Bản Quyền Số"
              value={newShopName}
              onChange={handleNameChange}
              isError={!!fieldErrors.name}
              disabled={registering}
              required
            />
          </FormField>

          <FormField
            id="shop-slug-input"
            label="Đường dẫn định danh (Slug)"
            required
            error={fieldErrors.slug}
            hint={
              <span>
                Xem trước link gian hàng:{' '}
                <code style={{ color: 'var(--accent-primary)', background: 'rgba(14, 165, 233, 0.1)', padding: '2px 6px', borderRadius: '4px' }}>
                  /marketplace/shops/{newShopSlug || 'ten-gian-hang'}
                </code>
              </span>
            }
          >
            <Input
              id="shop-slug-input"
              placeholder="vd: devstore-ban-quyen-so"
              value={newShopSlug}
              onChange={handleSlugChange}
              isError={!!fieldErrors.slug}
              disabled={registering}
              required
            />
          </FormField>

          <FormField id="shop-desc-input" label="Mô tả gian hàng" hint="Giới thiệu uy tín, chính sách bảo hành, cam kết chất lượng.">
            <Textarea
              id="shop-desc-input"
              rows={3}
              placeholder="Cam kết bản quyền chính hãng 100%, bảo hành 1 đổi 1 suốt thời gian sử dụng..."
              value={newShopDesc}
              onChange={(e) => setNewShopDesc(e.target.value)}
              disabled={registering}
            />
          </FormField>
        </div>

        <div style={{ marginBottom: '2rem' }}>
          <div style={{ fontSize: '0.78rem', fontWeight: 700, color: 'var(--text-dim)', textTransform: 'uppercase', letterSpacing: '0.05em', marginBottom: '0.85rem' }}>
            Thông tin liên hệ
          </div>

          <FormField id="shop-email-input" label="Email nhận thông báo" required error={fieldErrors.email} hint="Hệ thống gửi thông báo duyệt đơn, khiếu nại và sao kê tài chính.">
            <Input
              id="shop-email-input"
              type="email"
              placeholder="seller@example.com"
              value={newShopEmail}
              onChange={(e) => {
                setNewShopEmail(e.target.value);
                if (fieldErrors.email) setFieldErrors((prev) => ({ ...prev, email: undefined }));
              }}
              isError={!!fieldErrors.email}
              disabled={registering}
              required
            />
          </FormField>
        </div>

        {registerError && (
          <div style={{ marginBottom: '1.5rem' }}>
            <Alert type="error" message={registerError} />
          </div>
        )}

        <Button type="submit" variant="primary" size="lg" block loading={registering} disabled={registering}>
          Kích hoạt gian hàng ngay
        </Button>
      </form>
    </div>
  );
};
