import React, { useEffect, useState } from 'react';
import { marketplaceApi } from '@/api/marketplaceApi';
import type {
  ShopBalanceDto,
  ShopBankAccountDto,
  PayoutDto,
  LedgerStatementItemDto,
} from '@/types/marketplace';
import { FormField, Input, Select, Button, Badge, Spinner } from '@/components/common';

interface FinanceTabProps {
  balance: ShopBalanceDto | null;
  onRefreshBalance: () => void;
}

export const FinanceTab: React.FC<FinanceTabProps> = ({ balance, onRefreshBalance }) => {
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

  const loadFinanceData = async () => {
    try {
      const [bank, list, stmt] = await Promise.all([
        marketplaceApi.getShopBankAccount().catch(() => null),
        marketplaceApi.getShopPayouts().catch(() => ({ items: [] })),
        marketplaceApi.getShopStatement().catch(() => ({ items: [] })),
      ]);
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
    } catch (err) {
      console.error('Error loading finance tab data:', err);
    }
  };

  useEffect(() => {
    loadFinanceData();
  }, []);

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
      onRefreshBalance();
      loadFinanceData();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Không thể tạo lệnh rút tiền.');
    } finally {
      setPayoutLoading(false);
    }
  };

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

  return (
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
              <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Tạm giữ (Bảo hành 48h):</span>
              <span style={{ fontSize: '1rem', fontWeight: 700, color: '#f59e0b' }}>
                {(balance?.pendingBalance || 0).toLocaleString('vi-VN')} đ
              </span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between', padding: '0.75rem 1rem', background: 'var(--bg-surface)', borderRadius: '10px' }}>
              <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Đang chờ xử lý rút:</span>
              <span style={{ fontSize: '1rem', fontWeight: 700, color: 'var(--text-dim)' }}>
                {(balance?.payoutPendingBalance || 0).toLocaleString('vi-VN')} đ
              </span>
            </div>
          </div>

          <h3 style={{ fontSize: '0.95rem', fontWeight: 700, color: 'var(--text-main)', marginBottom: '0.75rem' }}>
            Tạo lệnh rút tiền (Payout)
          </h3>
          <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'flex-end' }}>
            <div style={{ flex: 1 }}>
              <FormField label="Số tiền muốn rút (VNĐ, tối thiểu 100,000 đ)">
                <Input
                  type="number"
                  min={100000}
                  step={10000}
                  value={payoutAmount}
                  onChange={(e) => setPayoutAmount(Number(e.target.value))}
                />
              </FormField>
            </div>
            <Button
              type="button"
              variant="primary"
              loading={payoutLoading}
              disabled={payoutLoading || (balance?.availableBalance || 0) < 100000 || !bankAccount}
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
                placeholder="Nhập số tài khoản ngân hàng"
                required
              />
            </FormField>

            <FormField label="Chủ tài khoản (Không dấu)" required>
              <Input
                value={bankForm.accountHolderName}
                onChange={(e) => setBankForm({ ...bankForm, accountHolderName: e.target.value.toUpperCase() })}
                placeholder="VD: NGUYEN VAN A"
                required
              />
            </FormField>

            <FormField label="Ngân hàng thụ hưởng" required>
              <Select
                value={bankForm.bankCode}
                onChange={(e) => {
                  const code = e.target.value;
                  const name = e.target.options[e.target.selectedIndex].text;
                  setBankForm({ ...bankForm, bankCode: code, bankName: name });
                }}
              >
                <option value="MB">MBBank (Ngân hàng Quân Đội)</option>
                <option value="VCB">Vietcombank</option>
                <option value="TCB">Techcombank</option>
                <option value="ACB">ACB (Á Châu)</option>
                <option value="VPB">VPBank</option>
                <option value="CTG">VietinBank</option>
                <option value="BIDV">BIDV</option>
                <option value="TPB">TPBank</option>
              </Select>
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
            <span style={{ color: 'var(--text-dim)', fontSize: '0.8rem' }}>đến</span>
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
                    <td style={{ padding: '0.65rem 0.5rem', color: 'var(--text-main)', maxWidth: '280px' }}>
                      {st.memo}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            {/* Phân trang sao kê */}
            {statementTotalCount > 20 && (
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '1rem', paddingTop: '1rem', borderTop: '1px solid var(--border-color)', fontSize: '0.85rem' }}>
                <span style={{ color: 'var(--text-dim)' }}>
                  Trang {statementPage} / {Math.ceil(statementTotalCount / 20)}
                </span>
                <div style={{ display: 'flex', gap: '0.5rem' }}>
                  <Button
                    variant="ghost"
                    size="sm"
                    disabled={statementPage <= 1 || statementLoading}
                    onClick={() => handleFilterStatement(statementPage - 1)}
                  >
                    ← Trước
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    disabled={statementPage >= Math.ceil(statementTotalCount / 20) || statementLoading}
                    onClick={() => handleFilterStatement(statementPage + 1)}
                  >
                    Sau →
                  </Button>
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};
