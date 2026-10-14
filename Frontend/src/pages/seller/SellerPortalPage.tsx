import React, { useCallback, useEffect, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { marketplaceApi } from '@/api/marketplaceApi';
import type {
  ShopPerformanceDto,
  ShopBalanceDto,
  ShopBankAccountDto,
  PayoutDto,
  ComplaintDto,
  Category,
  SellerOrderDto,
  DigitalCodeItemDto,
  LedgerStatementItemDto,
} from '@/types/marketplace';
import {
  PageContainer,
  FormField,
  Input,
  Textarea,
  Select,
  Button,
  Badge,
  Alert,
  Spinner,
  StoreIcon,
  SparklesIcon,
  ClockIcon,
  PackageIcon,
  ScaleIcon,
  EditIcon,
} from '@/components/common';
import { EditProductModal } from './components/EditProductModal';

// Hàm chuẩn hóa chuỗi tiếng Việt thành slug (kebab-case)
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

type SellerTab = 'overview' | 'finance' | 'products' | 'orders' | 'complaints';

const getTabFromPath = (pathname: string): SellerTab => {
  if (pathname.includes('/seller/finance')) return 'finance';
  if (pathname.includes('/seller/products') || pathname.includes('/seller/inventory')) return 'products';
  if (pathname.includes('/seller/orders')) return 'orders';
  if (pathname.includes('/seller/complaints')) return 'complaints';
  return 'overview';
};

export const SellerPortalPage: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const activeTab = getTabFromPath(location.pathname);

  const handleTabChange = (tab: SellerTab) => {
    navigate(`/seller/${tab}`);
  };
  const [shop, setShop] = useState<any | null>(null);
  const [loading, setLoading] = useState(true);
  const [hasShop, setHasShop] = useState(false);

  // Form đăng ký Shop mới
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

  // Cài đặt gian hàng & đóng/mở (F1.5)
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [settingsForm, setSettingsForm] = useState({
    name: '',
    contactEmail: '',
    description: '',
  });
  const [settingsSaving, setSettingsSaving] = useState(false);
  const [isResubmitOpen, setIsResubmitOpen] = useState(false);
  const [resubmitNotes, setResubmitNotes] = useState('');
  const [resubmitLoading, setResubmitLoading] = useState(false);

  // Tab 1: Performance
  const [performance, setPerformance] = useState<ShopPerformanceDto | null>(null);

  // Tab 2: Finance & Payout
  const [balance, setBalance] = useState<ShopBalanceDto | null>(null);
  const [bankAccount, setBankAccount] = useState<ShopBankAccountDto | null>(null);
  const [payouts, setPayouts] = useState<PayoutDto[]>([]);
  const [statementItems, setStatementItems] = useState<LedgerStatementItemDto[]>([]);
  const [statementPage, setStatementPage] = useState(1);
  const [statementTotalCount, setStatementTotalCount] = useState(0);
  const [statementFromDate, setStatementFromDate] = useState('');
  const [statementToDate, setStatementToDate] = useState('');
  const [statementLoading, setStatementLoading] = useState(false);
  const [payoutAmount, setPayoutAmount] = useState<number>(100000);
  const [payoutLoading, setPayoutLoading] = useState(false);
  const [bankForm, setBankForm] = useState({
    bankCode: 'MB',
    bankName: 'MBBank (Quân Đội)',
    accountNumber: '',
    accountHolderName: '',
  });
  const [bankSaving, setBankSaving] = useState(false);

  // Tab 3: Products & Inventory
  const [products, setProducts] = useState<any[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [selectedVariantId, setSelectedVariantId] = useState<string | null>(null);
  const [bulkCodesText, setBulkCodesText] = useState('');
  const [bulkImportLoading, setBulkImportLoading] = useState(false);
  const [bulkImportSuccess, setBulkImportSuccess] = useState('');

  // Chỉnh sửa sản phẩm
  const [editingProductId, setEditingProductId] = useState<string | null>(null);

  // F1.1: Tạo sản phẩm mới
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [createStep, setCreateStep] = useState<1 | 2 | 3 | 4>(1);
  const [productForm, setProductForm] = useState({
    name: '',
    categoryId: '',
    description: '',
    variantName: 'Gói Tiêu Chuẩn',
    sku: '',
    price: 50000,
    compareAtPrice: 100000,
    imageUrl: '',
  });
  const [isCreatingProduct, setIsCreatingProduct] = useState(false);
  const [createProductError, setCreateProductError] = useState<string | null>(null);
  const [isUploadingImage, setIsUploadingImage] = useState(false);
  const [imageUploadProgress, setImageUploadProgress] = useState<string | null>(null);
  const [imageInputMode, setImageInputMode] = useState<'upload' | 'url'>('upload');
  const fileInputRef = React.useRef<HTMLInputElement | null>(null);

  // F1.3: Xem kho mã số chi tiết
  const [inspectVariant, setInspectVariant] = useState<{ id: string; name: string } | null>(null);
  const [variantCodes, setVariantCodes] = useState<DigitalCodeItemDto[]>([]);
  const [codesLoading, setCodesLoading] = useState(false);

  // Tab 4: Đơn hàng của Shop (F1.2)
  const [shopOrders, setShopOrders] = useState<SellerOrderDto[]>([]);
  const [ordersLoading, setOrdersLoading] = useState(false);
  const [ordersFilterStatus, setOrdersFilterStatus] = useState<string>('all');
  const [ordersPage, setOrdersPage] = useState(1);
  const [ordersTotalCount, setOrdersTotalCount] = useState(0);

  // Tab 5: Complaints
  const [complaints, setComplaints] = useState<ComplaintDto[]>([]);
  const [respondingId, setRespondingId] = useState<string | null>(null);
  const [responseNote, setResponseNote] = useState('');
  const [actionLoading, setActionLoading] = useState(false);

  // Helper lấy danh sách category lá
  const getLeafCategories = (tree: Category[]): Category[] => {
    const leaves: Category[] = [];
    const traverse = (nodes: Category[]) => {
      for (const node of nodes) {
        if (node.children && node.children.length > 0) {
          traverse(node.children);
        } else {
          leaves.push(node);
        }
      }
    };
    traverse(tree);
    return leaves;
  };

  // Nạp dữ liệu các tab
  const loadTabData = useCallback(async (tab: string) => {
    try {
      if (tab === 'overview') {
        const [perf, bal, ordersRes] = await Promise.all([
          marketplaceApi.getShopPerformance().catch(() => null),
          marketplaceApi.getShopBalance().catch(() => null),
          marketplaceApi.getShopOrders(undefined, 1, 10).catch(() => null),
        ]);
        if (perf) setPerformance(perf);
        if (bal) setBalance(bal);
        if (ordersRes) {
          setShopOrders(ordersRes.items || ordersRes || []);
          setOrdersTotalCount(ordersRes.totalCount || (ordersRes.items || ordersRes || []).length);
        }
      } else if (tab === 'finance') {
        const [bal, bank, list, stmt] = await Promise.all([
          marketplaceApi.getShopBalance().catch(() => null),
          marketplaceApi.getShopBankAccount().catch(() => null),
          marketplaceApi.getShopPayouts().catch(() => ({ items: [] })),
          marketplaceApi.getShopStatement().catch(() => ({ items: [] })),
        ]);
        if (bal) setBalance(bal);
        if (bank) {
          setBankAccount(bank);
          setBankForm({
            bankCode: bank.bankCode || 'MB',
            bankName: bank.bankName || 'MBBank',
            accountNumber: bank.accountNumberMasked || '',
            accountHolderName: bank.accountHolderName || '',
          });
        }
        if (list) setPayouts(list.items || []);
        if (stmt) {
          setStatementItems(stmt.items || []);
          setStatementTotalCount(stmt.totalCount || 0);
          setStatementPage(stmt.page || 1);
        }
      } else if (tab === 'products') {
        const [prodRes, catRes] = await Promise.all([
          marketplaceApi.getShopProducts(),
          marketplaceApi.getCategoriesTree().catch(() => []),
        ]);
        setProducts(prodRes.items || prodRes || []);
        setCategories(catRes || []);
      } else if (tab === 'orders') {
        setOrdersLoading(true);
        try {
          const statusParam = ordersFilterStatus !== 'all' ? ordersFilterStatus : undefined;
          const res = await marketplaceApi.getShopOrders(statusParam, ordersPage, 15);
          setShopOrders(res.items || res || []);
          setOrdersTotalCount(res.totalCount || (res.items || res || []).length);
        } finally {
          setOrdersLoading(false);
        }
      } else if (tab === 'complaints') {
        const res = await marketplaceApi.getShopComplaints();
        setComplaints(res.items || res || []);
      }
    } catch (err) {
      console.error('Error loading tab data:', err);
    }
  }, []);

  const handleFilterStatement = async (page = 1) => {
    setStatementLoading(true);
    try {
      const res = await marketplaceApi.getShopStatement(
        page,
        20,
        statementFromDate || undefined,
        statementToDate || undefined
      );
      setStatementItems(res?.items || []);
      setStatementPage(res?.page || 1);
      setStatementTotalCount(res?.totalCount || 0);
    } catch (err) {
      console.error('Error filtering statement:', err);
    } finally {
      setStatementLoading(false);
    }
  };

  const handleResetStatementFilter = () => {
    setStatementFromDate('');
    setStatementToDate('');
    setStatementLoading(true);
    marketplaceApi
      .getShopStatement(1, 20)
      .then((res) => {
        setStatementItems(res?.items || []);
        setStatementPage(res?.page || 1);
        setStatementTotalCount(res?.totalCount || 0);
      })
      .finally(() => setStatementLoading(false));
  };

  const handleOrderStatusFilterChange = async (newStatus: string) => {
    setOrdersFilterStatus(newStatus);
    setOrdersLoading(true);
    try {
      const statusParam = newStatus !== 'all' ? newStatus : undefined;
      const res = await marketplaceApi.getShopOrders(statusParam, 1, 15);
      setShopOrders(res.items || res || []);
      setOrdersTotalCount(res.totalCount || (res.items || res || []).length);
      setOrdersPage(1);
    } catch (err) {
      console.error('Error filtering orders:', err);
    } finally {
      setOrdersLoading(false);
    }
  };

  const handleOrderPageChange = async (newPage: number) => {
    setOrdersLoading(true);
    try {
      const statusParam = ordersFilterStatus !== 'all' ? ordersFilterStatus : undefined;
      const res = await marketplaceApi.getShopOrders(statusParam, newPage, 15);
      setShopOrders(res.items || res || []);
      setOrdersTotalCount(res.totalCount || (res.items || res || []).length);
      setOrdersPage(newPage);
    } catch (err) {
      console.error('Error paginating orders:', err);
    } finally {
      setOrdersLoading(false);
    }
  };

  // Khởi tạo & nạp dữ liệu Shop
  const fetchShopData = useCallback(async () => {
    setLoading(true);
    try {
      const shopData = await marketplaceApi.getMyShop();
      if (shopData && shopData.id) {
        setShop(shopData);
        setHasShop(true);
        setSettingsForm({
          name: shopData.name || '',
          contactEmail: shopData.contactEmail || '',
          description: shopData.description || '',
        });
      } else {
        setHasShop(false);
      }
    } catch (err: any) {
      if (err?.response?.status === 404) {
        setHasShop(false);
      }
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchShopData();
  }, [fetchShopData]);

  useEffect(() => {
    if (hasShop) {
      loadTabData(activeTab);
    }
  }, [activeTab, hasShop, loadTabData]);

  // Handle thay đổi tên shop -> tự sinh slug nếu chưa sửa thủ công
  const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value;
    setNewShopName(val);
    if (fieldErrors.name) {
      setFieldErrors((prev) => ({ ...prev, name: undefined }));
    }
    if (!isSlugManuallyEdited) {
      const autoSlug = generateSlug(val);
      setNewShopSlug(autoSlug);
      if (fieldErrors.slug) {
        setFieldErrors((prev) => ({ ...prev, slug: undefined }));
      }
    }
  };

  // Handle thay đổi slug thủ công
  const handleSlugChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setIsSlugManuallyEdited(true);
    const rawVal = e.target.value.toLowerCase().replace(/\s+/g, '-');
    setNewShopSlug(rawVal);
    if (fieldErrors.slug) {
      setFieldErrors((prev) => ({ ...prev, slug: undefined }));
    }
  };

  // 1. Xử lý Đăng ký Shop mới
  const handleRegisterShop = async (e: React.FormEvent) => {
    e.preventDefault();
    setRegisterError(null);

    const errors: { name?: string; slug?: string; email?: string } = {};
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
      setShop(res);
      setHasShop(true);
      handleTabChange('overview');
    } catch (err: any) {
      const data = err?.response?.data;
      const errorMsg = data?.detail || data?.message || 'Không thể tạo gian hàng. Vui lòng kiểm tra lại thông tin.';
      setRegisterError(errorMsg);
    } finally {
      setRegistering(false);
    }
  };

  // 2. Cập nhật Tài khoản Ngân hàng
  const handleSaveBank = async (e: React.FormEvent) => {
    e.preventDefault();
    setBankSaving(true);
    try {
      const res = await marketplaceApi.setShopBankAccount(bankForm);
      setBankAccount(res);
      alert('Đã cập nhật tài khoản ngân hàng nhận tiền thành công!');
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi khi cập nhật tài khoản ngân hàng.');
    } finally {
      setBankSaving(false);
    }
  };

  // 3. Yêu cầu Rút tiền
  const handleRequestPayout = async () => {
    if (!bankAccount) {
      alert('Vui lòng thiết lập tài khoản ngân hàng trước khi rút tiền.');
      return;
    }
    if (payoutAmount < 100000) {
      alert('Hạn mức rút tiền tối thiểu là 100,000 đ.');
      return;
    }
    if ((balance?.availableBalance || 0) < payoutAmount) {
      alert('Số dư khả dụng không đủ.');
      return;
    }

    setPayoutLoading(true);
    try {
      await marketplaceApi.requestPayout(payoutAmount);
      alert('Lệnh rút tiền đã được gửi tới hệ thống đối soát.');
      loadTabData('finance');
    } catch (err: any) {
      alert(err.response?.data?.message || 'Không thể tạo lệnh rút tiền.');
    } finally {
      setPayoutLoading(false);
    }
  };

  // 4. Nạp Mã Kho Số Hàng Loạt
  const handleBulkImport = async () => {
    if (!selectedVariantId || !bulkCodesText.trim()) return;
    const lines = bulkCodesText.split('\n').map((l) => l.trim()).filter((l) => l.length > 0);
    if (lines.length === 0) return;

    setBulkImportLoading(true);
    setBulkImportSuccess('');
    try {
      const res = await marketplaceApi.addDigitalCodesBulk(selectedVariantId, lines);
      setBulkImportSuccess(`Đã nạp thành công ${res.importedCount || lines.length} mã vào kho số an toàn!`);
      setBulkCodesText('');
      loadTabData('products');
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi khi nạp mã kho số.');
    } finally {
      setBulkImportLoading(false);
    }
  };

  // 5. Phản hồi Khiếu nại
  const handleRespondComplaint = async (complaintId: string, acceptRefund: boolean) => {
    if (!responseNote.trim()) {
      alert('Vui lòng nhập ghi chú phản hồi.');
      return;
    }
    setActionLoading(true);
    try {
      await marketplaceApi.respondComplaint(complaintId, acceptRefund, responseNote.trim());
      alert(acceptRefund ? 'Đã chấp thuận hoàn tiền cho người mua.' : 'Đã từ chối khiếu nại. Quản trị viên sàn sẽ can thiệp.');
      setRespondingId(null);
      setResponseNote('');
      loadTabData('complaints');
    } catch (err: any) {
      alert(err.response?.data?.message || 'Không thể phản hồi khiếu nại.');
    } finally {
      setActionLoading(false);
    }
  };

  // Tải ảnh từ máy tính lên Cloudinary
  const handleImageFileUpload = async (file: File) => {
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      setCreateProductError('Vui lòng chọn tệp hình ảnh hợp lệ (JPG, PNG, WebP, GIF).');
      return;
    }

    if (file.size > 10 * 1024 * 1024) {
      setCreateProductError('Kích thước tệp ảnh không được vượt quá 10MB.');
      return;
    }

    setIsUploadingImage(true);
    setCreateProductError(null);
    setImageUploadProgress('Đang tải ảnh lên Cloudinary...');

    try {
      const res = await marketplaceApi.uploadShopProductImageFile(file);
      setProductForm((prev) => ({ ...prev, imageUrl: res.url }));
    } catch (err: any) {
      const msg = err.response?.data?.detail || err.response?.data?.message || err.message || 'Không thể tải ảnh lên Cloudinary.';
      setCreateProductError(`Lỗi tải ảnh lên Cloudinary: ${msg}`);
    } finally {
      setIsUploadingImage(false);
      setImageUploadProgress(null);
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  const handleImageDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      handleImageFileUpload(e.dataTransfer.files[0]);
    }
  };

  // F1.1: Tạo sản phẩm mới
  const handleCreateProduct = async (publishImmediate = false) => {
    if (!productForm.name.trim()) {
      setCreateProductError('Vui lòng nhập tên sản phẩm.');
      return;
    }
    if (!productForm.categoryId) {
      setCreateProductError('Vui lòng chọn danh mục cho sản phẩm.');
      return;
    }
    if (productForm.price <= 0) {
      setCreateProductError('Giá sản phẩm phải lớn hơn 0 đ.');
      return;
    }
    if (productForm.compareAtPrice && Number(productForm.compareAtPrice) < Number(productForm.price)) {
      setCreateProductError('Giá gốc so sánh (gạch ngang) không được nhỏ hơn giá bán.');
      return;
    }
    if (publishImmediate && !productForm.imageUrl.trim()) {
      setCreateProductError('Sản phẩm cần có ít nhất một hình ảnh bìa trước khi đăng bán ngay.');
      return;
    }

    setIsCreatingProduct(true);
    setCreateProductError(null);
    try {
      const priceVal = Number(productForm.price);
      const comparePriceVal = productForm.compareAtPrice ? Number(productForm.compareAtPrice) : undefined;
      const validComparePrice = comparePriceVal && comparePriceVal >= priceVal ? comparePriceVal : undefined;

      const created = await marketplaceApi.createShopProduct({
        name: productForm.name.trim(),
        categoryId: productForm.categoryId || undefined,
        description: productForm.description.trim() || undefined,
        defaultVariant: {
          price: priceVal,
          compareAtPrice: validComparePrice,
          sku: productForm.sku.trim() || undefined,
          deliveryType: 1, // 1: CodePool
          trackInventory: true,
        },
      });

      if (created?.id && productForm.variantName?.trim()) {
        const vName = productForm.variantName.trim();
        const defaultVar = created.variants?.[0];
        try {
          await marketplaceApi.updateShopProductVariants(created.id, {
            options: [{ name: 'Gói sản phẩm', position: 0, values: [vName] }],
            variants: [
              {
                id: defaultVar?.id,
                sku: productForm.sku.trim() || undefined,
                price: priceVal,
                compareAtPrice: validComparePrice,
                deliveryType: 1,
                trackInventory: true,
                optionValues: [vName],
              },
            ],
          });
        } catch {
          // Non-blocking fallback
        }
      }

      if (productForm.imageUrl.trim() && created?.id) {
        await marketplaceApi.addShopProductImage(created.id, {
          url: productForm.imageUrl.trim(),
          position: 0,
          isCover: true,
        });
      }

      if (publishImmediate && created?.id) {
        await marketplaceApi.publishShopProduct(created.id);
        alert('Đã tạo và đăng bán sản phẩm thành công lên Chợ số!');
      } else {
        alert('Đã lưu bản nháp sản phẩm thành công!');
      }

      setIsCreateModalOpen(false);
      setCreateStep(1);
      setProductForm({
        name: '',
        categoryId: '',
        description: '',
        variantName: 'Gói Tiêu Chuẩn',
        sku: '',
        price: 50000,
        compareAtPrice: 100000,
        imageUrl: '',
      });
      loadTabData('products');
    } catch (err: any) {
      const serverMsg =
        err.response?.data?.detail ||
        err.response?.data?.message ||
        (err.response?.data?.errors ? Object.values(err.response.data.errors).flat().join(', ') : null);
      setCreateProductError(serverMsg || 'Không thể tạo sản phẩm. Vui lòng kiểm tra lại.');
    } finally {
      setIsCreatingProduct(false);
    }

  };

  // F1.1: Chuyển đổi trạng thái sản phẩm
  const handlePublishProduct = async (id: string) => {
    try {
      await marketplaceApi.publishShopProduct(id);
      alert('Đã đăng bán sản phẩm lên Chợ số thành công!');
      loadTabData('products');
    } catch (err: any) {
      alert(err.response?.data?.message || 'Không thể đăng bán sản phẩm. Kiểm tra xem sản phẩm đã có ảnh chưa.');
    }
  };

  const handleHideProduct = async (id: string) => {
    try {
      await marketplaceApi.hideShopProduct(id);
      alert('Đã tạm ẩn sản phẩm khỏi sàn.');
      loadTabData('products');
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi khi ẩn sản phẩm.');
    }
  };

  const handleUnhideProduct = async (id: string) => {
    try {
      await marketplaceApi.unhideShopProduct(id);
      alert('Đã mở hiển thị lại sản phẩm trên sàn.');
      loadTabData('products');
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi khi bỏ ẩn sản phẩm.');
    }
  };

  const handleArchiveProduct = async (id: string) => {
    if (!window.confirm('Bạn có chắc muốn lưu trữ sản phẩm này? Sản phẩm sẽ ngừng kinh doanh.')) return;
    try {
      await marketplaceApi.archiveShopProduct(id);
      alert('Đã lưu trữ sản phẩm.');
      loadTabData('products');
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi khi lưu trữ sản phẩm.');
    }
  };

  // F1.3: Xem kho mã số chi tiết & hủy mã lỗi
  const handleOpenInspectCodes = async (variant: { id: string; name: string }) => {
    setInspectVariant(variant);
    setCodesLoading(true);
    try {
      const res = await marketplaceApi.getVariantCodes(variant.id);
      setVariantCodes(res.items || res || []);
    } catch (err) {
      console.error('Lỗi nạp mã kho số:', err);
    } finally {
      setCodesLoading(false);
    }
  };

  const handleInvalidateCode = async (codeId: string) => {
    if (!inspectVariant) return;
    if (!window.confirm('Xác nhận đánh dấu mã này là BỊ LỖI (Invalid) để chặn xuất kho cho khách?')) return;
    try {
      await marketplaceApi.invalidateCode(inspectVariant.id, codeId);
      alert('Đã vô hiệu hóa mã số thành công.');
      const res = await marketplaceApi.getVariantCodes(inspectVariant.id);
      setVariantCodes(res.items || res || []);
      loadTabData('products');
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi khi vô hiệu hóa mã.');
    }
  };

  // F1.5: Đóng / Mở shop
  const handleToggleOperatingStatus = async () => {
    const isCurrentlyClosed = shop?.operatingStatus === 'Closed' || shop?.operatingStatus === 1;
    const actionText = isCurrentlyClosed ? 'mở cửa hoạt động' : 'tạm đóng cửa hàng';
    if (!window.confirm(`Bạn có chắc chắn muốn ${actionText}?`)) return;
    try {
      if (isCurrentlyClosed) {
        await marketplaceApi.openMyShop();
      } else {
        await marketplaceApi.closeMyShop();
      }
      alert(`Đã ${actionText} thành công!`);
      await fetchShopData();
    } catch (err: any) {
      alert(err.response?.data?.message || `Không thể ${actionText}.`);
    }
  };

  // F1.5: Cập nhật thông tin Shop
  const handleSaveShopSettings = async (e: React.FormEvent) => {
    e.preventDefault();
    setSettingsSaving(true);
    try {
      await marketplaceApi.updateMyShop(settingsForm);
      alert('Đã cập nhật thông tin gian hàng!');
      setIsSettingsOpen(false);
      await fetchShopData();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi khi cập nhật gian hàng.');
    } finally {
      setSettingsSaving(false);
    }
  };

  // F1.5: Nộp lại hồ sơ duyệt
  const handleResubmit = async () => {
    setResubmitLoading(true);
    try {
      await marketplaceApi.resubmitMyShop({ notes: resubmitNotes.trim() });
      alert('Đã nộp lại hồ sơ xét duyệt tới Quản trị viên!');
      setIsResubmitOpen(false);
      await fetchShopData();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Không thể nộp lại hồ sơ xét duyệt.');
    } finally {
      setResubmitLoading(false);
    }
  };

  if (loading) {
    return (
      <PageContainer variant="narrow" style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', padding: '6rem 1rem' }}>
        <Spinner size="lg" />
        <p style={{ color: 'var(--text-muted)', marginTop: '1rem', fontSize: '0.9rem' }}>Đang nạp thông tin kênh người bán...</p>
      </PageContainer>
    );
  }

  // Nếu người dùng chưa mở Shop -> Giao diện Đăng ký mở shop chuẩn hóa
  if (!hasShop) {
    return (
      <PageContainer variant="narrow" style={{ padding: '2rem 1rem 4rem' }}>
        <div
          className="app-card"
          style={{
            background: 'var(--bg-card)',
            border: '1px solid var(--border-color)',
            borderRadius: '18px',
            padding: '2.5rem 2rem',
            boxShadow: '0 12px 32px rgba(0, 0, 0, 0.35)',
          }}
        >
          <div style={{ textAlign: 'center', marginBottom: '2rem' }}>
            <div
              style={{
                width: '60px',
                height: '60px',
                borderRadius: '16px',
                background: 'rgba(14, 165, 233, 0.12)',
                border: '1px solid rgba(14, 165, 233, 0.25)',
                color: 'var(--accent-primary)',
                display: 'inline-flex',
                alignItems: 'center',
                justifyContent: 'center',
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
      </PageContainer>
    );
  }

  const leafCategories = getLeafCategories(categories);
  const isShopClosed = shop?.operatingStatus === 'Closed' || shop?.operatingStatus === 1;

  // Giao diện Quản trị Gian hàng của người bán
  return (
    <PageContainer variant="wide" style={{ padding: '2rem 1rem 4rem' }}>
      {/* Banner Cảnh Báo nếu Shop bị Từ Chối (M1.8) */}
      {shop?.approvalStatus === 'Rejected' && (
        <div style={{ marginBottom: '1.5rem', background: 'rgba(239, 68, 68, 0.12)', border: '1px solid var(--danger)', borderRadius: '12px', padding: '1rem 1.25rem', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <div>
            <div style={{ fontWeight: 800, color: 'var(--danger)', fontSize: '0.95rem' }}>Gian hàng bị Quản trị viên từ chối phê duyệt</div>
            <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)', marginTop: '0.25rem' }}>
              Lý do: <em>{shop?.rejectReasonNote || 'Chưa cung cấp thông tin hợp lệ hoặc vi phạm chính sách.'}</em>
            </div>
          </div>
          <Button variant="danger" size="sm" onClick={() => setIsResubmitOpen(true)}>
            Nộp lại hồ sơ xét duyệt
          </Button>
        </div>
      )}

      {/* Header Dashboard */}
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: '1.5rem',
          flexWrap: 'wrap',
          gap: '1rem',
          background: 'var(--bg-card)',
          border: '1px solid var(--border-color)',
          borderRadius: '16px',
          padding: '1.25rem 1.5rem',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <div
            style={{
              width: '48px',
              height: '48px',
              borderRadius: '12px',
              background: 'var(--gradient-brand)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: '#fff',
              fontWeight: 800,
              fontSize: '1.25rem',
            }}
          >
            {shop?.name?.charAt(0)?.toUpperCase() || 'S'}
          </div>
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.65rem' }}>
              <h1 style={{ fontSize: '1.25rem', fontWeight: 800, color: 'var(--text-main)', margin: 0 }}>
                {shop?.name}
              </h1>
              <Badge variant={isShopClosed ? 'warning' : 'success'} size="sm">
                {isShopClosed ? 'Tạm đóng cửa' : 'Đang mở bán'}
              </Badge>
              {shop?.approvalStatus === 'Pending' && <Badge variant="neutral" size="sm">Chờ duyệt</Badge>}
            </div>
            <p style={{ margin: '0.25rem 0 0', fontSize: '0.82rem', color: 'var(--text-muted)' }}>
              Đường dẫn:{' '}
              <Link
                to={`/marketplace/shops/${shop?.slug}`}
                target="_blank"
                style={{ color: 'var(--accent-primary)', fontWeight: 700, textDecoration: 'underline' }}
                title="Xem trang gian hàng công khai"
              >
                /marketplace/shops/{shop?.slug}
              </Link>{' '}
              • Email: {shop?.contactEmail}
            </p>
          </div>
        </div>

        {/* Cụm điều khiển trạng thái Shop */}
        <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', flexWrap: 'wrap' }}>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={handleToggleOperatingStatus}
          >
            {isShopClosed ? 'Mở cửa bán hàng' : 'Tạm nghỉ bán'}
          </Button>
          <Button
            type="button"
            variant="secondary"
            size="sm"
            onClick={() => setIsSettingsOpen(true)}
          >
            Cài đặt shop
          </Button>
        </div>
      </div>

      {/* Tab Navigation */}
      <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap', marginBottom: '1.5rem' }}>
        <Button
          type="button"
          variant={activeTab === 'overview' ? 'primary' : 'secondary'}
          size="sm"
          icon={<SparklesIcon size={15} />}
          onClick={() => handleTabChange('overview')}
        >
          Tổng quan
        </Button>
        <Button
          type="button"
          variant={activeTab === 'finance' ? 'primary' : 'secondary'}
          size="sm"
          icon={<ClockIcon size={15} />}
          onClick={() => handleTabChange('finance')}
        >
          Ví & Rút tiền
        </Button>
        <Button
          type="button"
          variant={activeTab === 'products' ? 'primary' : 'secondary'}
          size="sm"
          icon={<PackageIcon size={15} />}
          onClick={() => handleTabChange('products')}
        >
          Sản phẩm & Kho mã ({products.length})
        </Button>
        <Button
          type="button"
          variant={activeTab === 'orders' ? 'primary' : 'secondary'}
          size="sm"
          icon={<StoreIcon size={15} />}
          onClick={() => handleTabChange('orders')}
        >
          Đơn hàng bán ({ordersTotalCount || shopOrders.length})
        </Button>
        <Button
          type="button"
          variant={activeTab === 'complaints' ? 'primary' : 'secondary'}
          size="sm"
          icon={<ScaleIcon size={15} />}
          onClick={() => handleTabChange('complaints')}
        >
          Khiếu nại ({complaints.length})
        </Button>
      </div>

      {/* TAB 1: HIỆU SUẤT & TỔNG QUAN */}
      {activeTab === 'overview' && (
        <div>
          {/* Hàng 4 thẻ chỉ số tài chính & kinh doanh */}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(230px, 1fr))', gap: '1rem', marginBottom: '1.5rem' }}>
            <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '14px', padding: '1.25rem' }}>
              <div style={{ fontSize: '0.78rem', color: 'var(--text-dim)', textTransform: 'uppercase', fontWeight: 600 }}>Tổng doanh thu tích lũy</div>
              <div style={{ fontSize: '1.55rem', fontWeight: 800, color: 'var(--text-main)', marginTop: '0.35rem' }}>
                {(balance?.lifetimeGrossRevenue || 0).toLocaleString('vi-VN')} đ
              </div>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '0.25rem' }}>
                Hoa hồng sàn đã trả: {(balance?.lifetimeCommissionPaid || 0).toLocaleString('vi-VN')} đ
              </div>
            </div>

            <div style={{ background: 'var(--bg-card)', border: '1px solid rgba(245, 158, 11, 0.25)', borderRadius: '14px', padding: '1.25rem', backgroundClip: 'padding-box' }}>
              <div style={{ fontSize: '0.78rem', color: '#f59e0b', textTransform: 'uppercase', fontWeight: 600 }}>Tiền tạm giữ (Bảo hành 48h)</div>
              <div style={{ fontSize: '1.55rem', fontWeight: 800, color: '#f59e0b', marginTop: '0.35rem' }}>
                {(balance?.pendingBalance || 0).toLocaleString('vi-VN')} đ
              </div>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '0.25rem' }}>
                Tự động chuyển vào số dư khả dụng sau 48h
              </div>
            </div>

            <div style={{ background: 'var(--bg-card)', border: '1px solid rgba(16, 185, 129, 0.25)', borderRadius: '14px', padding: '1.25rem' }}>
              <div style={{ fontSize: '0.78rem', color: '#10b981', textTransform: 'uppercase', fontWeight: 600 }}>Số dư khả dụng (Rút tiền)</div>
              <div style={{ fontSize: '1.55rem', fontWeight: 800, color: '#10b981', marginTop: '0.35rem' }}>
                {(balance?.availableBalance || 0).toLocaleString('vi-VN')} đ
              </div>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '0.25rem' }}>
                Đang chờ chuyển khoản: {(balance?.payoutPendingBalance || 0).toLocaleString('vi-VN')} đ
              </div>
            </div>

            <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '14px', padding: '1.25rem' }}>
              <div style={{ fontSize: '0.78rem', color: 'var(--text-dim)', textTransform: 'uppercase', fontWeight: 600 }}>Đơn hàng đã bán</div>
              <div style={{ fontSize: '1.55rem', fontWeight: 800, color: 'var(--accent-primary)', marginTop: '0.35rem' }}>
                {ordersTotalCount || performance?.totalOrders || shopOrders.length} đơn
              </div>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '0.25rem' }}>
                {performance?.completedOrders || 0} đơn đã hoàn tất • Tỷ lệ lỗi: {performance?.defectRate || 0}%
              </div>
            </div>
          </div>

          {/* Khối Đơn hàng mới bán gần đây trên màn hình Tổng quan */}
          <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '16px', padding: '1.5rem', marginBottom: '1.5rem' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.2rem', flexWrap: 'wrap', gap: '0.5rem' }}>
              <div>
                <h3 style={{ fontSize: '1.1rem', fontWeight: 700, color: 'var(--text-main)', margin: 0 }}>
                  Đơn hàng mới bán gần đây
                </h3>
                <p style={{ margin: '0.2rem 0 0', fontSize: '0.8rem', color: 'var(--text-dim)' }}>
                  Theo dõi các giao dịch bán hàng số vừa phát sinh của gian hàng
                </p>
              </div>
              <Button
                variant="secondary"
                size="sm"
                onClick={() => handleTabChange('orders')}
                style={{ fontSize: '0.8rem' }}
              >
                Xem tất cả đơn hàng →
              </Button>
            </div>

            {shopOrders.length === 0 ? (
              <div style={{ textAlign: 'center', padding: '2.5rem 1rem', color: 'var(--text-muted)' }}>
                <StoreIcon size={36} style={{ color: 'var(--text-dim)', marginBottom: '0.5rem' }} />
                <div style={{ fontSize: '0.9rem', fontWeight: 600, color: 'var(--text-main)' }}>Chưa có đơn hàng nào phát sinh</div>
                <div style={{ fontSize: '0.8rem', color: 'var(--text-dim)', marginTop: '0.25rem' }}>
                  Khi người mua thanh toán đơn hàng số của bạn, thông tin chi tiết và tiền bán sẽ hiển thị ngay tại đây.
                </div>
              </div>
            ) : (
              <div style={{ overflowX: 'auto' }}>
                <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.84rem' }}>
                  <thead>
                    <tr style={{ borderBottom: '1px solid var(--border-color)', textAlign: 'left', color: 'var(--text-dim)' }}>
                      <th style={{ padding: '0.65rem 0.5rem' }}>Mã đơn</th>
                      <th style={{ padding: '0.65rem 0.5rem' }}>Thời gian</th>
                      <th style={{ padding: '0.65rem 0.5rem' }}>Sản phẩm</th>
                      <th style={{ padding: '0.65rem 0.5rem' }}>Tổng thu</th>
                      <th style={{ padding: '0.65rem 0.5rem' }}>Thực nhận về ví</th>
                      <th style={{ padding: '0.65rem 0.5rem' }}>Trạng thái</th>
                    </tr>
                  </thead>
                  <tbody>
                    {shopOrders.slice(0, 5).map((ord) => (
                      <tr key={ord.id} style={{ borderBottom: '1px solid var(--border-color)' }}>
                        <td style={{ padding: '0.65rem 0.5rem', fontWeight: 700, color: 'var(--accent-primary)' }}>
                          #{ord.orderNo || ord.id.slice(0, 8)}
                        </td>
                        <td style={{ padding: '0.65rem 0.5rem', whiteSpace: 'nowrap', color: 'var(--text-muted)' }}>
                          {new Date(ord.createdAtUtc).toLocaleString('vi-VN')}
                        </td>
                        <td style={{ padding: '0.65rem 0.5rem' }}>
                          {ord.items?.map((it) => (
                            <div key={it.id} style={{ fontSize: '0.82rem' }}>
                              <strong style={{ color: 'var(--text-main)' }}>{it.productName}</strong>
                              <span style={{ color: 'var(--text-dim)' }}> ({it.variantLabel}) x{it.quantity}</span>
                            </div>
                          ))}
                        </td>
                        <td style={{ padding: '0.65rem 0.5rem', fontWeight: 600 }}>
                          {ord.total.toLocaleString('vi-VN')} đ
                        </td>
                        <td style={{ padding: '0.65rem 0.5rem', fontWeight: 800, color: '#10b981' }}>
                          +{ord.shopNet.toLocaleString('vi-VN')} đ
                        </td>
                        <td style={{ padding: '0.65rem 0.5rem' }}>
                          <Badge
                            variant={ord.status === 'Completed' || ord.status === 'Delivered' ? 'success' : ord.status === 'Cancelled' ? 'danger' : 'neutral'}
                            size="sm"
                          >
                            {ord.status === 'Delivered' ? 'Đã giao mã' : ord.status === 'Completed' ? 'Hoàn tất' : ord.status}
                          </Badge>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      )}

      {/* TAB 2: VÍ & RÚT TIỀN + SAO KÊ SỔ CÁI (F1.4) */}
      {activeTab === 'finance' && (
        <div>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))', gap: '1.5rem', marginBottom: '1.5rem' }}>
            {/* Cột Trái: Số Dư & Rút Tiền */}
            <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '16px', padding: '1.5rem' }}>
              <h2 style={{ fontSize: '1.1rem', fontWeight: 700, color: 'var(--text-main)', marginBottom: '1rem' }}>
                Số dư gian hàng
              </h2>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem', marginBottom: '1.5rem' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', padding: '0.75rem 1rem', background: 'var(--bg-surface)', borderRadius: '10px' }}>
                  <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Khả dụng (Có thể rút):</span>
                  <span style={{ fontSize: '1.1rem', fontWeight: 800, color: '#10b981' }}>
                    {(balance?.availableBalance || 0).toLocaleString('vi-VN')} đ
                  </span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', padding: '0.75rem 1rem', background: 'var(--bg-surface)', borderRadius: '10px' }}>
                  <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Đang giữ bảo hành (48h):</span>
                  <span style={{ fontSize: '1rem', fontWeight: 700, color: '#f59e0b' }}>
                    {(balance?.pendingBalance || 0).toLocaleString('vi-VN')} đ
                  </span>
                </div>
              </div>

              <h3 style={{ fontSize: '0.95rem', fontWeight: 700, color: 'var(--text-main)', marginBottom: '0.75rem' }}>
                Tạo lệnh rút tiền (Payout)
              </h3>
              <div style={{ display: 'flex', gap: '0.75rem' }}>
                <Input
                  type="number"
                  step="10000"
                  min="100000"
                  value={payoutAmount}
                  onChange={(e) => setPayoutAmount(Number(e.target.value))}
                />
                <Button
                  type="button"
                  variant="primary"
                  disabled={payoutLoading || (balance?.availableBalance || 0) < 100000 || !bankAccount}
                  loading={payoutLoading}
                  onClick={handleRequestPayout}
                >
                  Yêu cầu rút
                </Button>
              </div>
            </div>

            {/* Cột Phải: Ngân Hàng Nhận Tiền */}
            <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '16px', padding: '1.5rem' }}>
              <h2 style={{ fontSize: '1.1rem', fontWeight: 700, color: 'var(--text-main)', marginBottom: '1rem' }}>
                Tài khoản ngân hàng nhận tiền
              </h2>
              <form onSubmit={handleSaveBank}>
                <FormField label="Số tài khoản" required>
                  <Input
                    value={bankForm.accountNumber}
                    onChange={(e) => setBankForm({ ...bankForm, accountNumber: e.target.value })}
                    placeholder="Nhập số tài khoản"
                    required
                  />
                </FormField>

                <FormField label="Tên chủ tài khoản (In hoa không dấu)" required>
                  <Input
                    value={bankForm.accountHolderName}
                    onChange={(e) => setBankForm({ ...bankForm, accountHolderName: e.target.value.toUpperCase() })}
                    placeholder="NGUYEN VAN A"
                    required
                  />
                </FormField>

                <Button type="submit" variant="secondary" block loading={bankSaving} disabled={bankSaving}>
                  Lưu thông tin ngân hàng
                </Button>
              </form>
            </div>
          </div>

          {/* Lịch sử yêu cầu rút tiền */}
          <div style={{ marginBottom: '1.5rem', background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '16px', padding: '1.5rem' }}>
            <h3 style={{ fontSize: '1rem', fontWeight: 700, color: 'var(--text-main)', marginBottom: '1rem' }}>
              Lịch sử yêu cầu rút tiền ({payouts.length})
            </h3>
            {payouts.length === 0 ? (
              <p style={{ color: 'var(--text-dim)', fontSize: '0.85rem', margin: 0 }}>Chưa có lệnh rút tiền nào.</p>
            ) : (
              <div style={{ overflowX: 'auto' }}>
                <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.85rem' }}>
                  <thead>
                    <tr style={{ borderBottom: '1px solid var(--border-color)', textAlign: 'left', color: 'var(--text-dim)' }}>
                      <th style={{ padding: '0.6rem 0.5rem' }}>Ngày tạo</th>
                      <th style={{ padding: '0.6rem 0.5rem' }}>Số tiền</th>
                      <th style={{ padding: '0.6rem 0.5rem' }}>Ngân hàng</th>
                      <th style={{ padding: '0.6rem 0.5rem' }}>Trạng thái</th>
                    </tr>
                  </thead>
                  <tbody>
                    {payouts.map((p) => (
                      <tr key={p.id} style={{ borderBottom: '1px solid var(--border-color)' }}>
                        <td style={{ padding: '0.6rem 0.5rem' }}>{new Date(p.createdAtUtc).toLocaleDateString('vi-VN')}</td>
                        <td style={{ padding: '0.6rem 0.5rem', fontWeight: 700, color: 'var(--text-main)' }}>{p.amount.toLocaleString('vi-VN')} đ</td>
                        <td style={{ padding: '0.6rem 0.5rem', color: 'var(--text-muted)' }}>{p.bankName} ({p.accountNumberMasked})</td>
                        <td style={{ padding: '0.6rem 0.5rem' }}>
                          <Badge variant={p.status === 'Succeeded' ? 'success' : p.status === 'Requested' ? 'warning' : 'neutral'} size="sm">
                            {p.status}
                          </Badge>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          {/* F1.4: BẢNG SAO KÊ SỔ CÁI TÀI CHÍNH KÉP */}
          <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '16px', padding: '1.5rem' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem', flexWrap: 'wrap', gap: '0.75rem' }}>
              <div>
                <h3 style={{ fontSize: '1.05rem', fontWeight: 700, color: 'var(--text-main)', margin: 0 }}>
                  Sao kê biến động Sổ cái tài chính kép (Tổng {statementTotalCount || statementItems.length} bút toán)
                </h3>
                <p style={{ margin: '0.25rem 0 0', fontSize: '0.8rem', color: 'var(--text-dim)' }}>
                  Ghi nhận minh bạch theo nguyên tắc hạch toán đối ứng (Nợ / Có). Số dư được cập nhật tức thì.
                </p>
              </div>

              {/* Bộ lọc ngày */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', flexWrap: 'wrap' }}>
                <input
                  type="date"
                  value={statementFromDate}
                  onChange={(e) => setStatementFromDate(e.target.value)}
                  style={{
                    background: 'var(--bg-surface)',
                    border: '1px solid var(--border-color)',
                    color: 'var(--text-main)',
                    borderRadius: '8px',
                    padding: '0.35rem 0.65rem',
                    fontSize: '0.8rem',
                  }}
                  title="Từ ngày"
                />
                <span style={{ fontSize: '0.8rem', color: 'var(--text-dim)' }}>đến</span>
                <input
                  type="date"
                  value={statementToDate}
                  onChange={(e) => setStatementToDate(e.target.value)}
                  style={{
                    background: 'var(--bg-surface)',
                    border: '1px solid var(--border-color)',
                    color: 'var(--text-main)',
                    borderRadius: '8px',
                    padding: '0.35rem 0.65rem',
                    fontSize: '0.8rem',
                  }}
                  title="Đến ngày"
                />
                <Button
                  variant="secondary"
                  size="sm"
                  disabled={statementLoading}
                  onClick={() => handleFilterStatement(1)}
                  style={{ padding: '0.35rem 0.75rem', fontSize: '0.8rem' }}
                >
                  {statementLoading ? 'Đang lọc...' : 'Lọc sao kê'}
                </Button>
                {(statementFromDate || statementToDate) && (
                  <Button
                    variant="ghost"
                    size="sm"
                    disabled={statementLoading}
                    onClick={handleResetStatementFilter}
                    style={{ padding: '0.35rem 0.6rem', fontSize: '0.8rem' }}
                  >
                    Đặt lại
                  </Button>
                )}
              </div>
            </div>

            {statementLoading ? (
              <div style={{ textAlign: 'center', padding: '2.5rem' }}>
                <Spinner size="md" />
                <p style={{ marginTop: '0.75rem', fontSize: '0.85rem', color: 'var(--text-dim)' }}>Đang tải dữ liệu sao kê...</p>
              </div>
            ) : statementItems.length === 0 ? (
              <p style={{ color: 'var(--text-dim)', fontSize: '0.85rem', margin: 0, padding: '1rem 0' }}>
                Không tìm thấy biến động sổ cái nào trong khoảng thời gian đã chọn.
              </p>
            ) : (
              <div style={{ overflowX: 'auto' }}>
                <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.83rem' }}>
                  <thead>
                    <tr style={{ borderBottom: '1px solid var(--border-color)', textAlign: 'left', color: 'var(--text-dim)' }}>
                      <th style={{ padding: '0.65rem 0.5rem' }}>Thời gian</th>
                      <th style={{ padding: '0.65rem 0.5rem' }}>Tài khoản đối ứng</th>
                      <th style={{ padding: '0.65rem 0.5rem' }}>Nợ (Debit -)</th>
                      <th style={{ padding: '0.65rem 0.5rem' }}>Có (Credit +)</th>
                      <th style={{ padding: '0.65rem 0.5rem' }}>Loại & Tham chiếu</th>
                      <th style={{ padding: '0.65rem 0.5rem' }}>Diễn giải nghiệp vụ</th>
                    </tr>
                  </thead>
                  <tbody>
                    {statementItems.map((st) => (
                      <tr key={st.entryId} style={{ borderBottom: '1px solid var(--border-color)' }}>
                        <td style={{ padding: '0.65rem 0.5rem', whiteSpace: 'nowrap', color: 'var(--text-muted)' }}>
                          {st.createdAt ? new Date(st.createdAt).toLocaleString('vi-VN') : '-'}
                        </td>
                        <td style={{ padding: '0.65rem 0.5rem', fontWeight: 600 }}>
                          <span style={{ color: 'var(--text-main)' }}>{st.accountName || st.accountCode}</span>
                          {st.accountCode && (
                            <span style={{ display: 'block', fontSize: '0.72rem', color: 'var(--text-dim)', fontFamily: 'monospace' }}>
                              [{st.accountCode}]
                            </span>
                          )}
                        </td>
                        <td style={{ padding: '0.65rem 0.5rem', fontWeight: 700, color: st.debit > 0 ? '#ef4444' : 'var(--text-dim)' }}>
                          {st.debit > 0 ? `-${st.debit.toLocaleString('vi-VN')} đ` : '-'}
                        </td>
                        <td style={{ padding: '0.65rem 0.5rem', fontWeight: 700, color: st.credit > 0 ? '#10b981' : 'var(--text-dim)' }}>
                          {st.credit > 0 ? `+${st.credit.toLocaleString('vi-VN')} đ` : '-'}
                        </td>
                        <td style={{ padding: '0.65rem 0.5rem' }}>
                          <Badge variant={st.credit > 0 ? 'success' : 'neutral'} size="sm">
                            {st.transactionType}
                          </Badge>
                          {st.refType && (
                            <span style={{ display: 'block', fontSize: '0.72rem', color: 'var(--text-dim)', marginTop: '0.2rem' }}>
                              {st.refType}
                            </span>
                          )}
                        </td>
                        <td style={{ padding: '0.65rem 0.5rem', color: 'var(--text-muted)', maxWidth: '280px' }}>
                          {st.memo}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>

                {/* Phân trang sao kê */}
                {statementTotalCount > 20 && (
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '1rem', paddingTop: '0.75rem', borderTop: '1px solid var(--border-color)' }}>
                    <span style={{ fontSize: '0.8rem', color: 'var(--text-dim)' }}>
                      Trang {statementPage} / {Math.ceil(statementTotalCount / 20)}
                    </span>
                    <div style={{ display: 'flex', gap: '0.45rem' }}>
                      <Button
                        variant="secondary"
                        size="xs"
                        disabled={statementPage <= 1 || statementLoading}
                        onClick={() => handleFilterStatement(statementPage - 1)}
                      >
                        Trang trước
                      </Button>
                      <Button
                        variant="secondary"
                        size="xs"
                        disabled={statementPage >= Math.ceil(statementTotalCount / 20) || statementLoading}
                        onClick={() => handleFilterStatement(statementPage + 1)}
                      >
                        Trang sau
                      </Button>
                    </div>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      )}

      {/* TAB 3: SẢN PHẨM & KHO MÃ SỐ (F1.1 + F1.3) */}
      {activeTab === 'products' && (
        <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '16px', padding: '1.5rem' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem', flexWrap: 'wrap', gap: '0.75rem' }}>
            <div>
              <h2 style={{ fontSize: '1.15rem', fontWeight: 700, color: 'var(--text-main)', margin: 0 }}>
                Quản lý sản phẩm & Kho số tự động
              </h2>
              <p style={{ margin: '0.25rem 0 0', fontSize: '0.82rem', color: 'var(--text-muted)' }}>
                Tạo sản phẩm, nạp mã số tự động và thiết lập trạng thái kinh doanh.
              </p>
            </div>
            <Button
              type="button"
              variant="primary"
              size="sm"
              onClick={() => setIsCreateModalOpen(true)}
            >
              + Tạo sản phẩm mới
            </Button>
          </div>

          {products.length === 0 ? (
            <div style={{ textAlign: 'center', padding: '3.5rem 1rem', color: 'var(--text-muted)' }}>
              <PackageIcon size={40} style={{ color: 'var(--text-dim)', marginBottom: '0.75rem' }} />
              <div>Chưa có sản phẩm nào được tạo trong gian hàng.</div>
              <Button
                variant="primary"
                size="sm"
                style={{ marginTop: '1rem' }}
                onClick={() => setIsCreateModalOpen(true)}
              >
                Tạo sản phẩm đầu tiên ngay
              </Button>
            </div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              {products.map((p) => (
                <div key={p.id} style={{ padding: '1.25rem', background: 'var(--bg-surface)', borderRadius: '12px', border: '1px solid var(--border-color)' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: '0.75rem' }}>
                    <div>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                        <h3 style={{ fontSize: '1.05rem', fontWeight: 800, color: 'var(--text-main)', margin: 0 }}>{p.name}</h3>
                        <Badge
                          variant={p.status === 'Listed' ? 'success' : p.status === 'Draft' ? 'neutral' : p.status === 'Hidden' ? 'warning' : 'danger'}
                          size="sm"
                        >
                          {p.status === 'Listed' ? 'Đang bán' : p.status === 'Draft' ? 'Bản nháp' : p.status === 'Hidden' ? 'Tạm ẩn' : p.status}
                        </Badge>
                      </div>
                      <p style={{ fontSize: '0.8rem', color: 'var(--text-dim)', margin: '0.25rem 0 0' }}>
                        Slug: <code>{p.slug}</code> • Danh mục: <strong>{p.categoryName || 'Sản phẩm số'}</strong>
                      </p>
                    </div>

                    {/* Cụm nút chuyển trạng thái & chỉnh sửa sản phẩm */}
                    <div style={{ display: 'flex', gap: '0.4rem', flexWrap: 'wrap' }}>
                      <Button
                        type="button"
                        variant="secondary"
                        size="xs"
                        icon={<EditIcon size={13} />}
                        onClick={() => setEditingProductId(p.id)}
                      >
                        Chỉnh sửa
                      </Button>
                      {p.status === 'Draft' && (
                        <Button type="button" variant="primary" size="xs" onClick={() => handlePublishProduct(p.id)}>
                          Đăng bán
                        </Button>
                      )}
                      {p.status === 'Listed' && (
                        <Button type="button" variant="secondary" size="xs" onClick={() => handleHideProduct(p.id)}>
                          Tạm ẩn
                        </Button>
                      )}
                      {p.status === 'Hidden' && (
                        <Button type="button" variant="primary" size="xs" onClick={() => handleUnhideProduct(p.id)}>
                          Bỏ ẩn
                        </Button>
                      )}
                      {p.status !== 'Archived' && (
                        <Button type="button" variant="ghost" size="xs" onClick={() => handleArchiveProduct(p.id)}>
                          Lưu trữ
                        </Button>
                      )}
                    </div>
                  </div>

                  {/* Danh sách biến thể */}
                  <div style={{ marginTop: '1rem', display: 'flex', flexWrap: 'wrap', gap: '0.65rem' }}>
                    {p.variants?.map((v: any) => (
                      <div
                        key={v.id}
                        style={{
                          display: 'flex',
                          alignItems: 'center',
                          gap: '0.5rem',
                          background: 'var(--bg-card)',
                          padding: '0.5rem 0.85rem',
                          borderRadius: '8px',
                          border: '1px solid var(--border-color)',
                          fontSize: '0.85rem',
                        }}
                      >
                        <span style={{ fontWeight: 700, color: 'var(--text-main)' }}>{v.name || 'Mặc định'}</span>
                        <span style={{ color: 'var(--accent-primary)', fontWeight: 700 }}>
                          {v.price ? Number(v.price).toLocaleString('vi-VN') + ' đ' : ''}
                        </span>
                        <Badge variant={v.availableStock > 0 ? 'success' : 'danger'} size="sm">
                          Kho: {v.availableStock || 0}
                        </Badge>
                        <Button
                          type="button"
                          variant="ghost"
                          size="xs"
                          onClick={() => handleOpenInspectCodes({ id: v.id, name: `${p.name} - ${v.name || ''}` })}
                        >
                          Xem kho mã
                        </Button>
                        <Button
                          type="button"
                          variant="secondary"
                          size="xs"
                          onClick={() => setSelectedVariantId(v.id)}
                        >
                          + Nạp kho
                        </Button>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}

          {/* F1.1: MODAL TẠO SẢN PHẨM MỚI 4 BƯỚC */}
          {isCreateModalOpen && (
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
                padding: '1rem',
              }}
            >
              <div
                style={{
                  background: 'var(--bg-card)',
                  border: '1px solid var(--border-color)',
                  borderRadius: '16px',
                  maxWidth: '580px',
                  width: '100%',
                  padding: '1.75rem',
                  maxHeight: '90vh',
                  overflowY: 'auto',
                }}
              >
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
                  <h3 style={{ fontSize: '1.2rem', fontWeight: 800, color: 'var(--text-main)', margin: 0 }}>
                    Tạo sản phẩm số mới ({createStep}/4)
                  </h3>
                  <Badge variant="primary" size="sm">
                    {createStep === 1 ? '1. Thông tin' : createStep === 2 ? '2. Biến thể & Giá' : createStep === 3 ? '3. Hình ảnh' : '4. Xác nhận'}
                  </Badge>
                </div>

                {createProductError && (
                  <div style={{ marginBottom: '1rem' }}>
                    <Alert type="error" message={createProductError} />
                  </div>
                )}

                {/* BƯỚC 1: THÔNG TIN CƠ BẢN */}
                {createStep === 1 && (
                  <div>
                    <FormField label="Tên sản phẩm" required hint="Tên gói hoặc phần mềm bản quyền">
                      <Input
                        placeholder="VD: Bản quyền Windows 11 Pro Vĩnh Viễn"
                        value={productForm.name}
                        onChange={(e) => setProductForm({ ...productForm, name: e.target.value })}
                        required
                      />
                    </FormField>

                    <FormField label="Danh mục sản phẩm" required hint="Chọn danh mục tương ứng">
                      <Select
                        value={productForm.categoryId}
                        onChange={(e) => setProductForm({ ...productForm, categoryId: e.target.value })}
                        required
                      >
                        <option value="">-- Chọn danh mục --</option>
                        {leafCategories.map((c) => (
                          <option key={c.id} value={c.id}>
                            {c.name}
                          </option>
                        ))}
                      </Select>
                    </FormField>

                    <FormField label="Mô tả sản phẩm" hint="Giới thiệu tính năng, cách kích hoạt, chính sách bảo hành">
                      <Textarea
                        rows={4}
                        placeholder="Mô tả chi tiết sản phẩm, cam kết đổi trả..."
                        value={productForm.description}
                        onChange={(e) => setProductForm({ ...productForm, description: e.target.value })}
                      />
                    </FormField>

                    <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.25rem' }}>
                      <Button variant="ghost" size="sm" onClick={() => setIsCreateModalOpen(false)}>Hủy</Button>
                      <Button variant="primary" size="sm" onClick={() => setCreateStep(2)}>Tiếp theo: Biến thể & Giá</Button>
                    </div>
                  </div>
                )}

                {/* BƯỚC 2: BIẾN THỂ & ĐỊNH GIÁ */}
                {createStep === 2 && (
                  <div>
                    <FormField label="Tên biến thể mặc định" required hint="Ví dụ: Gói 1 Năm, Bản Quyền Vĩnh Viễn">
                      <Input
                        value={productForm.variantName}
                        onChange={(e) => setProductForm({ ...productForm, variantName: e.target.value })}
                      />
                    </FormField>

                    <div style={{ gridTemplateColumns: '1fr 1fr', display: 'grid', gap: '1rem' }}>
                      <FormField label="Giá bán (VND)" required>
                        <Input
                          type="number"
                          step="1000"
                          min="1000"
                          value={productForm.price}
                          onChange={(e) => setProductForm({ ...productForm, price: Number(e.target.value) })}
                        />
                      </FormField>

                      <FormField label="Giá gốc gạch ngang (VND)">
                        <Input
                          type="number"
                          step="1000"
                          min="1000"
                          value={productForm.compareAtPrice}
                          onChange={(e) => setProductForm({ ...productForm, compareAtPrice: Number(e.target.value) })}
                        />
                      </FormField>
                    </div>

                    <FormField label="Mã SKU (tùy chọn)" hint="Để trống để hệ thống tự sinh SKU">
                      <Input
                        placeholder="VD: WIN11-PRO-OEM"
                        value={productForm.sku}
                        onChange={(e) => setProductForm({ ...productForm, sku: e.target.value })}
                      />
                    </FormField>

                    <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: '1.25rem' }}>
                      <Button variant="secondary" size="sm" onClick={() => setCreateStep(1)}>Quay lại</Button>
                      <Button

                        variant="primary"
                        size="sm"
                        onClick={() => {
                          if (productForm.price <= 0) {
                            setCreateProductError('Giá sản phẩm phải lớn hơn 0 đ.');
                            return;
                          }
                          if (productForm.compareAtPrice && Number(productForm.compareAtPrice) < Number(productForm.price)) {
                            setCreateProductError('Giá gốc so sánh không được nhỏ hơn giá bán.');
                            return;
                          }
                          setCreateProductError(null);
                          setCreateStep(3);
                        }}
                      >
                        Tiếp theo: Hình ảnh
                      </Button>
                    </div>
                  </div>
                )}

                {/* BƯỚC 3: HÌNH ẢNH SẢN PHẨM */}
                {createStep === 3 && (
                  <div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
                      <div>
                        <h4 style={{ margin: 0, fontSize: '0.98rem', fontWeight: 600 }}>Ảnh bìa sản phẩm</h4>
                        <p style={{ margin: '0.2rem 0 0', fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
                          Tải ảnh từ máy tính để lưu trữ tự động trên Cloudinary CDN
                        </p>
                      </div>
                      <div style={{ display: 'flex', gap: '0.35rem', background: 'var(--bg-card)', padding: '3px', borderRadius: '8px', border: '1px solid var(--border-color)' }}>
                        <button
                          type="button"
                          onClick={() => setImageInputMode('upload')}
                          style={{
                            padding: '0.3rem 0.65rem',
                            fontSize: '0.78rem',
                            borderRadius: '6px',
                            border: 'none',
                            background: imageInputMode === 'upload' ? 'var(--accent-primary, #1877f2)' : 'transparent',
                            color: imageInputMode === 'upload' ? '#fff' : 'var(--text-secondary)',
                            cursor: 'pointer',
                            fontWeight: 500,
                            transition: 'all 0.15s ease'
                          }}
                        >
                          Tải từ máy tính
                        </button>
                        <button
                          type="button"
                          onClick={() => setImageInputMode('url')}
                          style={{
                            padding: '0.3rem 0.65rem',
                            fontSize: '0.78rem',
                            borderRadius: '6px',
                            border: 'none',
                            background: imageInputMode === 'url' ? 'var(--accent-primary, #1877f2)' : 'transparent',
                            color: imageInputMode === 'url' ? '#fff' : 'var(--text-secondary)',
                            cursor: 'pointer',
                            fontWeight: 500,
                            transition: 'all 0.15s ease'
                          }}
                        >
                          Nhập URL
                        </button>
                      </div>
                    </div>

                    {imageInputMode === 'upload' ? (
                      <div>
                        <input
                          type="file"
                          ref={fileInputRef}
                          style={{ display: 'none' }}
                          accept="image/png,image/jpeg,image/jpg,image/webp,image/gif"
                          onChange={(e) => {
                            if (e.target.files && e.target.files.length > 0) {
                              handleImageFileUpload(e.target.files[0]);
                            }
                          }}
                        />

                        {!productForm.imageUrl ? (
                          <div
                            onClick={() => !isUploadingImage && fileInputRef.current?.click()}
                            onDragOver={(e) => e.preventDefault()}
                            onDrop={handleImageDrop}
                            style={{
                              border: '2px dashed var(--border-color)',
                              borderRadius: '12px',
                              padding: '2.5rem 1.5rem',
                              textAlign: 'center',
                              cursor: isUploadingImage ? 'not-allowed' : 'pointer',
                              background: 'var(--bg-surface)',
                              transition: 'all 0.2s ease',
                            }}
                          >
                            {isUploadingImage ? (
                              <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '0.75rem' }}>
                                <Spinner size="md" />
                                <div style={{ fontSize: '0.9rem', color: 'var(--accent-primary)', fontWeight: 500 }}>
                                  {imageUploadProgress || 'Đang tải ảnh lên Cloudinary...'}
                                </div>
                                <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>
                                  Hệ thống đang lưu trữ và nén ảnh trên Cloudinary
                                </div>
                              </div>
                            ) : (
                              <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '0.5rem' }}>
                                <div
                                  style={{
                                    width: '52px',
                                    height: '52px',
                                    borderRadius: '50%',
                                    background: 'rgba(14, 165, 233, 0.12)',
                                    display: 'flex',
                                    alignItems: 'center',
                                    justifyContent: 'center',
                                    color: 'var(--accent-primary)',
                                    marginBottom: '0.25rem'
                                  }}
                                >
                                  <svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                                    <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                                    <polyline points="17 8 12 3 7 8" />
                                    <line x1="12" y1="3" x2="12" y2="15" />
                                  </svg>
                                </div>
                                <div style={{ fontSize: '0.95rem', fontWeight: 600, color: 'var(--text-primary)' }}>
                                  Nhấp để chọn ảnh từ máy tính hoặc kéo thả vào đây
                                </div>
                                <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
                                  Hỗ trợ JPG, PNG, WebP (Tối đa 10MB) • Tự động lưu lên Cloudinary
                                </div>
                              </div>
                            )}
                          </div>
                        ) : (
                          <div
                            style={{
                              border: '1px solid var(--border-color)',
                              borderRadius: '12px',
                              padding: '1.25rem',
                              background: 'var(--bg-surface)',
                            }}
                          >
                            <div style={{ display: 'flex', gap: '1.25rem', alignItems: 'center' }}>
                              <img
                                src={productForm.imageUrl}
                                alt="Preview"
                                style={{
                                  width: '110px',
                                  height: '110px',
                                  borderRadius: '10px',
                                  objectFit: 'cover',
                                  border: '1px solid var(--border-color)',
                                  flexShrink: 0,
                                  boxShadow: '0 4px 12px rgba(0,0,0,0.1)'
                                }}
                              />
                              <div style={{ flex: 1, minWidth: 0 }}>
                                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.4rem' }}>
                                  <Badge variant="success" size="sm">✓ Đã lưu trữ trên Cloudinary</Badge>
                                </div>
                                <div
                                  style={{
                                    fontSize: '0.78rem',
                                    color: 'var(--text-secondary)',
                                    whiteSpace: 'nowrap',
                                    overflow: 'hidden',
                                    textOverflow: 'ellipsis',
                                    marginBottom: '0.85rem',
                                    fontFamily: 'monospace',
                                    background: 'var(--bg-card)',
                                    padding: '0.35rem 0.6rem',
                                    borderRadius: '6px',
                                    border: '1px solid var(--border-color)'
                                  }}
                                  title={productForm.imageUrl}
                                >
                                  {productForm.imageUrl}
                                </div>
                                <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
                                  <Button
                                    variant="secondary"
                                    size="sm"
                                    disabled={isUploadingImage}
                                    onClick={() => fileInputRef.current?.click()}
                                  >
                                    Tải ảnh khác từ máy
                                  </Button>
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    disabled={isUploadingImage}
                                    onClick={() => setProductForm({ ...productForm, imageUrl: '' })}
                                  >
                                    Xóa ảnh
                                  </Button>
                                </div>
                              </div>
                            </div>
                          </div>
                        )}
                      </div>
                    ) : (
                      <div>
                        <FormField label="URL Hình ảnh bìa sản phẩm" required hint="Dán đường dẫn ảnh thumbnail trực tiếp">
                          <Input
                            placeholder="https://..."
                            value={productForm.imageUrl}
                            onChange={(e) => setProductForm({ ...productForm, imageUrl: e.target.value })}
                          />
                        </FormField>
                        {productForm.imageUrl && (
                          <div style={{ marginTop: '0.75rem', textAlign: 'center' }}>
                            <img
                              src={productForm.imageUrl}
                              alt="Preview"
                              style={{ maxWidth: '100%', maxHeight: '180px', borderRadius: '10px', objectFit: 'cover', border: '1px solid var(--border-color)' }}
                            />
                          </div>
                        )}
                      </div>
                    )}

                    <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: '1.5rem' }}>
                      <Button variant="secondary" size="sm" onClick={() => setCreateStep(2)} disabled={isUploadingImage}>
                        Quay lại
                      </Button>
                      <Button
                        variant="primary"
                        size="sm"
                        disabled={isUploadingImage}
                        loading={isUploadingImage}
                        onClick={() => {
                          if (!productForm.imageUrl.trim()) {
                            setCreateProductError('Vui lòng tải ảnh từ máy tính hoặc cung cấp link ảnh trước khi tiếp tục.');
                            return;
                          }
                          setCreateProductError(null);
                          setCreateStep(4);
                        }}
                      >
                        Tiếp theo: Xác nhận
                      </Button>
                    </div>
                  </div>
                )}

                {/* BƯỚC 4: XÁC NHẬN & XUẤT BẢN */}
                {createStep === 4 && (
                  <div>
                    <div style={{ background: 'var(--bg-surface)', padding: '1rem', borderRadius: '10px', border: '1px solid var(--border-color)', marginBottom: '1.25rem', fontSize: '0.88rem' }}>
                      <div style={{ marginBottom: '0.4rem' }}><strong>Sản phẩm:</strong> {productForm.name}</div>
                      <div style={{ marginBottom: '0.4rem' }}><strong>Danh mục:</strong> {leafCategories.find((c) => c.id === productForm.categoryId)?.name || 'Chưa chọn'}</div>
                      <div style={{ marginBottom: '0.4rem' }}><strong>Biến thể:</strong> {productForm.variantName}</div>
                      <div style={{ marginBottom: '0.4rem' }}><strong>Giá bán:</strong> {productForm.price.toLocaleString('vi-VN')} đ</div>
                    </div>

                    <div style={{ display: 'flex', justifyContent: 'space-between', gap: '0.5rem', flexWrap: 'wrap' }}>
                      <Button variant="secondary" size="sm" onClick={() => setCreateStep(3)} disabled={isCreatingProduct}>Quay lại</Button>
                      <div style={{ display: 'flex', gap: '0.5rem' }}>
                        <Button
                          variant="ghost"
                          size="sm"
                          loading={isCreatingProduct}
                          disabled={isCreatingProduct}
                          onClick={() => handleCreateProduct(false)}
                        >
                          Lưu bản nháp
                        </Button>
                        <Button
                          variant="primary"
                          size="sm"
                          loading={isCreatingProduct}
                          disabled={isCreatingProduct}
                          onClick={() => handleCreateProduct(true)}
                        >
                          Đăng bán ngay
                        </Button>
                      </div>
                    </div>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* F1.3: MODAL XEM CHI TIẾT KHO MÃ & HỦY MÃ LỖI */}
          {inspectVariant && (
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
                padding: '1rem',
              }}
            >
              <div
                style={{
                  background: 'var(--bg-card)',
                  border: '1px solid var(--border-color)',
                  borderRadius: '16px',
                  maxWidth: '650px',
                  width: '100%',
                  padding: '1.75rem',
                  maxHeight: '85vh',
                  overflowY: 'auto',
                }}
              >
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
                  <div>
                    <h3 style={{ fontSize: '1.15rem', fontWeight: 800, color: 'var(--text-main)', margin: 0 }}>
                      Kho mã số bảo mật
                    </h3>
                    <p style={{ margin: '0.2rem 0 0', fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                      Biến thể: <strong>{inspectVariant.name}</strong>
                    </p>
                  </div>
                  <Button variant="ghost" size="xs" onClick={() => setInspectVariant(null)}>Đóng</Button>
                </div>

                {codesLoading ? (
                  <div style={{ textAlign: 'center', padding: '2rem' }}><Spinner size="md" /></div>
                ) : variantCodes.length === 0 ? (
                  <div style={{ textAlign: 'center', padding: '2rem', color: 'var(--text-muted)' }}>
                    Chưa có mã nào trong kho của biến thể này. Bấm <strong>+ Nạp kho</strong> để thêm mã.
                  </div>
                ) : (
                  <div style={{ overflowX: 'auto' }}>
                    <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.85rem' }}>
                      <thead>
                        <tr style={{ borderBottom: '1px solid var(--border-color)', textAlign: 'left', color: 'var(--text-dim)' }}>
                          <th style={{ padding: '0.6rem 0.5rem' }}>Mã đã che</th>
                          <th style={{ padding: '0.6rem 0.5rem' }}>Trạng thái</th>
                          <th style={{ padding: '0.6rem 0.5rem' }}>Ngày nạp</th>
                          <th style={{ padding: '0.6rem 0.5rem', textAlign: 'right' }}>Thao tác</th>
                        </tr>
                      </thead>
                      <tbody>
                        {variantCodes.map((code) => (
                          <tr key={code.id} style={{ borderBottom: '1px solid var(--border-color)' }}>
                            <td style={{ padding: '0.6rem 0.5rem', fontFamily: 'monospace', fontWeight: 700 }}>
                              {code.maskedValue}
                            </td>
                            <td style={{ padding: '0.6rem 0.5rem' }}>
                              <Badge
                                variant={code.status === 'Available' ? 'success' : code.status === 'Delivered' ? 'primary' : code.status === 'Reserved' ? 'warning' : 'danger'}
                                size="sm"
                              >
                                {code.status}
                              </Badge>
                            </td>
                            <td style={{ padding: '0.6rem 0.5rem', color: 'var(--text-dim)', fontSize: '0.78rem' }}>
                              {new Date(code.createdAtUtc).toLocaleDateString('vi-VN')}
                            </td>
                            <td style={{ padding: '0.6rem 0.5rem', textAlign: 'right' }}>
                              {code.status === 'Available' && (
                                <Button
                                  type="button"
                                  variant="danger"
                                  size="xs"
                                  onClick={() => handleInvalidateCode(code.id)}
                                >
                                  Báo lỗi (Hủy)
                                </Button>
                              )}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Modal Nạp Kho Số Bulk */}
          {selectedVariantId && (
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
                padding: '1rem',
              }}
            >
              <div
                style={{
                  background: 'var(--bg-card)',
                  border: '1px solid var(--border-color)',
                  borderRadius: '16px',
                  maxWidth: '520px',
                  width: '100%',
                  padding: '1.75rem',
                }}
              >
                <h3 style={{ fontSize: '1.15rem', fontWeight: 800, color: 'var(--text-main)', marginBottom: '0.5rem' }}>
                  Nạp mã kho số hàng loạt
                </h3>
                <p style={{ fontSize: '0.82rem', color: 'var(--text-muted)', marginBottom: '1rem', lineHeight: '1.5' }}>
                  Nhập danh sách mã key hoặc tài khoản. Mỗi mã trên một dòng riêng biệt. Hệ thống mã hóa AES-256-GCM tự động.
                </p>

                {bulkImportSuccess && (
                  <Alert type="success" message={bulkImportSuccess} className="mb-3" />
                )}

                <Textarea
                  rows={6}
                  placeholder={`KEY-XXXX-1111\nKEY-YYYY-2222\nuser@example.com|password123`}
                  value={bulkCodesText}
                  onChange={(e) => setBulkCodesText(e.target.value)}
                  style={{ fontFamily: 'monospace', fontSize: '0.85rem' }}
                />

                <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.25rem' }}>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={() => setSelectedVariantId(null)}
                    disabled={bulkImportLoading}
                  >
                    Đóng
                  </Button>
                  <Button
                    type="button"
                    variant="primary"
                    size="sm"
                    onClick={handleBulkImport}
                    loading={bulkImportLoading}
                    disabled={bulkImportLoading || !bulkCodesText.trim()}
                  >
                    Xác nhận nạp kho
                  </Button>
                </div>
              </div>
            </div>
          )}

          {/* MODAL CHỈNH SỬA SẢN PHẨM */}
          {editingProductId && (
            <EditProductModal
              productId={editingProductId}
              categories={categories}
              isOpen={Boolean(editingProductId)}
              onClose={() => setEditingProductId(null)}
              onOpenAddStock={(vId) => {
                setEditingProductId(null);
                setSelectedVariantId(vId);
              }}
              onProductUpdated={() => {
                loadTabData('products');
              }}
            />
          )}
        </div>
      )}

      {/* TAB 4: ĐƠN HÀNG CỦA SHOP (F1.2) */}
      {activeTab === 'orders' && (
        <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '16px', padding: '1.5rem' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem', flexWrap: 'wrap', gap: '0.75rem' }}>
            <div>
              <h2 style={{ fontSize: '1.15rem', fontWeight: 700, color: 'var(--text-main)', margin: 0 }}>
                Quản lý đơn hàng bán của Gian hàng ({ordersTotalCount || shopOrders.length} đơn)
              </h2>
              <p style={{ margin: '0.2rem 0 0', fontSize: '0.8rem', color: 'var(--text-dim)' }}>
                Chi tiết các giao dịch bán hàng, doanh thu thực nhận và trạng thái giao mã số tự động
              </p>
            </div>

            {/* Bộ lọc trạng thái đơn hàng */}
            <div style={{ display: 'flex', gap: '0.4rem', flexWrap: 'wrap' }}>
              {[
                { key: 'all', label: 'Tất cả' },
                { key: 'Delivered', label: 'Đã giao mã' },
                { key: 'Completed', label: 'Hoàn tất' },
                { key: 'Cancelled', label: 'Đã hủy' },
              ].map((st) => (
                <Button
                  key={st.key}
                  type="button"
                  variant={ordersFilterStatus === st.key ? 'primary' : 'secondary'}
                  size="xs"
                  onClick={() => handleOrderStatusFilterChange(st.key)}
                >
                  {st.label}
                </Button>
              ))}
            </div>
          </div>

          {ordersLoading ? (
            <div style={{ textAlign: 'center', padding: '3rem' }}>
              <Spinner size="md" />
              <p style={{ marginTop: '0.5rem', fontSize: '0.85rem', color: 'var(--text-dim)' }}>Đang tải danh sách đơn hàng...</p>
            </div>
          ) : shopOrders.length === 0 ? (
            <div style={{ textAlign: 'center', padding: '3.5rem 1rem', color: 'var(--text-muted)' }}>
              <StoreIcon size={40} style={{ color: 'var(--text-dim)', marginBottom: '0.75rem' }} />
              <div style={{ fontWeight: 600, color: 'var(--text-main)' }}>Chưa có đơn hàng nào phù hợp</div>
              <div style={{ fontSize: '0.82rem', color: 'var(--text-dim)', marginTop: '0.25rem' }}>
                Khi khách hàng đặt mua các sản phẩm từ gian hàng của bạn, đơn hàng sẽ hiển thị tại đây.
              </div>
            </div>
          ) : (
            <div style={{ overflowX: 'auto' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.85rem' }}>
                <thead>
                  <tr style={{ borderBottom: '1px solid var(--border-color)', textAlign: 'left', color: 'var(--text-dim)' }}>
                    <th style={{ padding: '0.65rem 0.5rem' }}>Mã đơn</th>
                    <th style={{ padding: '0.65rem 0.5rem' }}>Thời gian</th>
                    <th style={{ padding: '0.65rem 0.5rem' }}>Mặt hàng & Biến thể</th>
                    <th style={{ padding: '0.65rem 0.5rem' }}>Tổng thu</th>
                    <th style={{ padding: '0.65rem 0.5rem' }}>Phí sàn</th>
                    <th style={{ padding: '0.65rem 0.5rem' }}>Thực nhận về ví</th>
                    <th style={{ padding: '0.65rem 0.5rem' }}>Trạng thái</th>
                  </tr>
                </thead>
                <tbody>
                  {shopOrders.map((ord) => (
                    <tr key={ord.id} style={{ borderBottom: '1px solid var(--border-color)' }}>
                      <td style={{ padding: '0.65rem 0.5rem', fontWeight: 700, color: 'var(--accent-primary)' }}>
                        #{ord.orderNo || ord.id.slice(0, 8)}
                      </td>
                      <td style={{ padding: '0.65rem 0.5rem', whiteSpace: 'nowrap', color: 'var(--text-muted)' }}>
                        {new Date(ord.createdAtUtc).toLocaleString('vi-VN')}
                      </td>
                      <td style={{ padding: '0.65rem 0.5rem' }}>
                        {ord.items?.map((it) => (
                          <div key={it.id} style={{ fontSize: '0.82rem', marginBottom: '0.2rem' }}>
                            <strong style={{ color: 'var(--text-main)' }}>{it.productName}</strong>
                            <span style={{ color: 'var(--text-dim)' }}> ({it.variantLabel}) x{it.quantity}</span>
                          </div>
                        ))}
                      </td>
                      <td style={{ padding: '0.65rem 0.5rem', fontWeight: 600 }}>
                        {ord.total.toLocaleString('vi-VN')} đ
                      </td>
                      <td style={{ padding: '0.65rem 0.5rem', color: '#f43f5e', fontSize: '0.82rem' }}>
                        -{ord.commissionTotal.toLocaleString('vi-VN')} đ
                      </td>
                      <td style={{ padding: '0.65rem 0.5rem', fontWeight: 800, color: '#10b981' }}>
                        +{ord.shopNet.toLocaleString('vi-VN')} đ
                      </td>
                      <td style={{ padding: '0.65rem 0.5rem' }}>
                        <Badge
                          variant={ord.status === 'Completed' || ord.status === 'Delivered' ? 'success' : ord.status === 'Cancelled' ? 'danger' : 'neutral'}
                          size="sm"
                        >
                          {ord.status === 'Delivered' ? 'Đã giao mã (Chờ 48h)' : ord.status === 'Completed' ? 'Hoàn tất' : ord.status}
                        </Badge>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>

              {/* Phân trang đơn hàng */}
              {ordersTotalCount > 15 && (
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '1.25rem', paddingTop: '0.75rem', borderTop: '1px solid var(--border-color)' }}>
                  <span style={{ fontSize: '0.8rem', color: 'var(--text-dim)' }}>
                    Trang {ordersPage} / {Math.ceil(ordersTotalCount / 15)} (Tổng {ordersTotalCount} đơn)
                  </span>
                  <div style={{ display: 'flex', gap: '0.45rem' }}>
                    <Button
                      variant="secondary"
                      size="xs"
                      disabled={ordersPage <= 1 || ordersLoading}
                      onClick={() => handleOrderPageChange(ordersPage - 1)}
                    >
                      Trang trước
                    </Button>
                    <Button
                      variant="secondary"
                      size="xs"
                      disabled={ordersPage >= Math.ceil(ordersTotalCount / 15) || ordersLoading}
                      onClick={() => handleOrderPageChange(ordersPage + 1)}
                    >
                      Trang sau
                    </Button>
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
      )}

      {/* TAB 5: KHIẾU NẠI */}
      {activeTab === 'complaints' && (
        <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '16px', padding: '1.5rem' }}>
          <h2 style={{ fontSize: '1.1rem', fontWeight: 700, color: 'var(--text-main)', marginBottom: '1rem' }}>
            Khiếu nại từ khách hàng (Thời hạn phản hồi 24h)
          </h2>
          {complaints.length === 0 ? (
            <div style={{ textAlign: 'center', padding: '3rem 1rem', color: 'var(--text-muted)' }}>
              Hiện tại không có khiếu nại nào từ khách hàng.
            </div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              {complaints.map((c) => (
                <div key={c.id} style={{ padding: '1.25rem', background: 'var(--bg-surface)', borderRadius: '12px', border: '1px solid var(--border-color)' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '0.75rem' }}>
                    <div>
                      <div style={{ fontWeight: 700, fontSize: '0.95rem', color: 'var(--text-main)' }}>
                        Lý do: {c.reason} • Đơn: #{c.shopOrderId?.slice(0, 8)}
                      </div>
                      <div style={{ fontSize: '0.78rem', color: 'var(--text-dim)', marginTop: '0.2rem' }}>
                        Gửi lúc: {new Date(c.createdAtUtc).toLocaleString('vi-VN')}
                      </div>
                    </div>
                    <Badge variant={c.status === 'Resolved' || c.status === 'Refunded' ? 'success' : 'warning'} size="sm">
                      {c.status}
                    </Badge>
                  </div>

                  <p style={{ fontSize: '0.88rem', margin: '0.5rem 0', background: 'var(--bg-card)', padding: '0.75rem', borderRadius: '8px', color: 'var(--text-main)' }}>
                    <strong>Mô tả của khách:</strong> {c.description}
                  </p>

                  {c.status === 'Open' && (
                    <div style={{ marginTop: '0.85rem', paddingTop: '0.85rem', borderTop: '1px solid var(--border-color)' }}>
                      {respondingId === c.id ? (
                        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
                          <Textarea
                            rows={3}
                            placeholder="Nhập ghi chú giải thích hoặc đồng ý hoàn tiền..."
                            value={responseNote}
                            onChange={(e) => setResponseNote(e.target.value)}
                          />
                          <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'flex-end' }}>
                            <Button
                              type="button"
                              variant="ghost"
                              size="sm"
                              onClick={() => setRespondingId(null)}
                              disabled={actionLoading}
                            >
                              Hủy
                            </Button>
                            <Button
                              type="button"
                              variant="danger"
                              size="sm"
                              onClick={() => handleRespondComplaint(c.id, false)}
                              disabled={actionLoading}
                            >
                              Từ chối khiếu nại
                            </Button>
                            <Button
                              type="button"
                              variant="primary"
                              size="sm"
                              onClick={() => handleRespondComplaint(c.id, true)}
                              disabled={actionLoading}
                            >
                              Chấp thuận hoàn tiền
                            </Button>
                          </div>
                        </div>
                      ) : (
                        <Button
                          type="button"
                          variant="secondary"
                          size="sm"
                          onClick={() => {
                            setRespondingId(c.id);
                            setResponseNote('');
                          }}
                        >
                          Xử lý khiếu nại này
                        </Button>
                      )}
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* F1.5: MODAL CÀI ĐẶT GIAN HÀNG */}
      {isSettingsOpen && (
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
            padding: '1rem',
          }}
        >
          <div
            style={{
              background: 'var(--bg-card)',
              border: '1px solid var(--border-color)',
              borderRadius: '16px',
              maxWidth: '500px',
              width: '100%',
              padding: '1.75rem',
            }}
          >
            <h3 style={{ fontSize: '1.2rem', fontWeight: 800, color: 'var(--text-main)', marginBottom: '1rem' }}>
              Cài đặt thông tin gian hàng
            </h3>
            <form onSubmit={handleSaveShopSettings}>
              <FormField label="Tên gian hàng" required>
                <Input
                  value={settingsForm.name}
                  onChange={(e) => setSettingsForm({ ...settingsForm, name: e.target.value })}
                  required
                />
              </FormField>

              <FormField label="Email nhận thông báo" required>
                <Input
                  type="email"
                  value={settingsForm.contactEmail}
                  onChange={(e) => setSettingsForm({ ...settingsForm, contactEmail: e.target.value })}
                  required
                />
              </FormField>

              <FormField label="Mô tả gian hàng">
                <Textarea
                  rows={3}
                  value={settingsForm.description}
                  onChange={(e) => setSettingsForm({ ...settingsForm, description: e.target.value })}
                />
              </FormField>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.25rem' }}>
                <Button variant="ghost" size="sm" onClick={() => setIsSettingsOpen(false)} disabled={settingsSaving}>
                  Hủy
                </Button>
                <Button variant="primary" size="sm" type="submit" loading={settingsSaving} disabled={settingsSaving}>
                  Lưu thay đổi
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* F1.5: MODAL NỘP LẠI HỒ SƠ XÉT DUYỆT */}
      {isResubmitOpen && (
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
            padding: '1rem',
          }}
        >
          <div
            style={{
              background: 'var(--bg-card)',
              border: '1px solid var(--border-color)',
              borderRadius: '16px',
              maxWidth: '500px',
              width: '100%',
              padding: '1.75rem',
            }}
          >
            <h3 style={{ fontSize: '1.2rem', fontWeight: 800, color: 'var(--text-main)', marginBottom: '0.5rem' }}>
              Nộp lại hồ sơ xét duyệt gian hàng
            </h3>
            <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)', marginBottom: '1rem' }}>
              Giải trình việc khắc phục các thiếu sót mà Quản trị viên đã nêu trong lần xét duyệt trước.
            </p>

            <FormField label="Ghi chú giải trình" required>
              <Textarea
                rows={4}
                placeholder="Tôi đã cập nhật chính sách bảo hành rõ ràng và cam kết tuân thủ..."
                value={resubmitNotes}
                onChange={(e) => setResubmitNotes(e.target.value)}
              />
            </FormField>

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.25rem' }}>
              <Button variant="ghost" size="sm" onClick={() => setIsResubmitOpen(false)} disabled={resubmitLoading}>
                Đóng
              </Button>
              <Button variant="danger" size="sm" onClick={handleResubmit} loading={resubmitLoading} disabled={resubmitLoading || !resubmitNotes.trim()}>
                Xác nhận nộp lại
              </Button>
            </div>
          </div>
        </div>
      )}
    </PageContainer>
  );
};

export default SellerPortalPage;
