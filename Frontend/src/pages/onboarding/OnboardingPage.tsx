import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTitle } from '@/hooks/useTitle';
import { interestApi } from '@/api/interestApi';
import type { InterestDto } from '@/types/interest';
import { getApiErrorMessage } from '@/utils/error';
import { ROUTES } from '@/constants/routes';

export const OnboardingPage: React.FC = () => {
  useTitle('Khám phá sở thích của bạn');
  const navigate = useNavigate();

  const [interests, setInterests] = useState<InterestDto[]>([]);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [searchQuery, setSearchQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);

  const MIN_REQUIRED = 3;

  useEffect(() => {
    const fetchInterests = async () => {
      setLoading(true);
      setError(null);
      try {
        const data = await interestApi.getAllInterests();
        setInterests(data);

        // Nạp các sở thích đã chọn trước đó (nếu có)
        const initiallySelected = new Set(
          data.filter((item) => item.isSelected).map((item) => item.id)
        );
        setSelectedIds(initiallySelected);
      } catch (err) {
        setError(getApiErrorMessage(err));
      } finally {
        setLoading(false);
      }
    };

    fetchInterests();
  }, []);

  const toggleInterest = (id: string) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
    setError(null);
  };

  const handleSelectAllFiltered = () => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      filteredInterests.forEach((item) => next.add(item.id));
      return next;
    });
  };

  const handleClearSelection = () => {
    setSelectedIds(new Set());
  };

  const query = searchQuery.trim().toLowerCase();
  const filteredInterests = query
    ? interests.filter(
        (item) =>
          item.name.toLowerCase().includes(query) ||
          (item.description && item.description.toLowerCase().includes(query))
      )
    : interests;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (selectedIds.size < MIN_REQUIRED) {
      setError(`Vui lòng chọn tối thiểu ${MIN_REQUIRED} chủ đề để thuật toán hiểu rõ sở thích của bạn.`);
      return;
    }

    setSaving(true);
    setError(null);
    try {
      await interestApi.selectMyInterests({
        interestIds: Array.from(selectedIds),
      });
      setSuccessMsg('Đã lưu sở thích thành công! Đang chuyển hướng về Bảng tin...');
      setTimeout(() => {
        navigate(ROUTES.HOME);
      }, 1000);
    } catch (err) {
      setError(getApiErrorMessage(err));
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="container onboarding-page">
      <div className="onboarding-header">
        <div className="badge onboarding-badge">✨ Cá nhân hóa trải nghiệm Feed</div>
        <h1 className="onboarding-title">
          Bạn quan tâm đến <span className="gradient-text">chủ đề nào?</span>
        </h1>
        <p className="onboarding-subtitle">
          Chọn tối thiểu <strong>{MIN_REQUIRED} chủ đề</strong> bạn yêu thích. Thuật toán gợi ý của Social sẽ kết hợp
          sở thích của bạn cùng công nghệ AI để tạo nên Bảng tin hấp dẫn nhất.
        </p>

        {/* Thanh trạng thái chọn & Thanh tìm kiếm */}
        <div className="onboarding-controls">
          <div className="search-box">
            <span className="search-icon">🔍</span>
            <input
              type="text"
              placeholder="Tìm kiếm chủ đề (ví dụ: Công nghệ, Lập trình, Game...)"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="search-input"
            />
            {searchQuery && (
              <button
                type="button"
                onClick={() => setSearchQuery('')}
                className="search-clear-btn"
                title="Xóa tìm kiếm"
              >
                ✕
              </button>
            )}
          </div>

          <div className="selection-status-bar">
            <div className={`selection-counter ${selectedIds.size >= MIN_REQUIRED ? 'counter-valid' : ''}`}>
              Đã chọn: <strong>{selectedIds.size}</strong> / tối thiểu {MIN_REQUIRED}
            </div>

            <div className="selection-actions">
              {filteredInterests.length > 0 && searchQuery && (
                <button
                  type="button"
                  onClick={handleSelectAllFiltered}
                  className="btn btn-ghost btn-sm"
                >
                  Chọn tất cả kết quả
                </button>
              )}
              {selectedIds.size > 0 && (
                <button
                  type="button"
                  onClick={handleClearSelection}
                  className="btn btn-ghost btn-sm text-dim"
                >
                  Bỏ chọn tất cả
                </button>
              )}
            </div>
          </div>
        </div>
      </div>

      {error && <div className="alert alert-error mt-3">{error}</div>}
      {successMsg && <div className="alert alert-success mt-3">{successMsg}</div>}

      {/* Grid danh sách các sở thích */}
      {loading ? (
        <div className="onboarding-loading">
          <div className="spinner"></div>
          <p>Đang tải danh sách chủ đề...</p>
        </div>
      ) : (
        <form onSubmit={handleSubmit}>
          <div className="interests-grid">
            {filteredInterests.map((interest) => {
              const isSelected = selectedIds.has(interest.id);
              return (
                <div
                  key={interest.id}
                  onClick={() => toggleInterest(interest.id)}
                  className={`interest-card ${isSelected ? 'interest-card-selected' : ''}`}
                  role="button"
                  tabIndex={0}
                  onKeyDown={(e) => {
                    if (e.key === ' ' || e.key === 'Enter') {
                      e.preventDefault();
                      toggleInterest(interest.id);
                    }
                  }}
                >
                  <div className="interest-card-header">
                    <span className="interest-icon">{interest.icon || '🏷️'}</span>
                    <div className={`checkbox-indicator ${isSelected ? 'checked' : ''}`}>
                      {isSelected ? '✓' : ''}
                    </div>
                  </div>
                  <h3 className="interest-name">{interest.name}</h3>
                  {interest.description && (
                    <p className="interest-description">{interest.description}</p>
                  )}
                </div>
              );
            })}
          </div>

          {filteredInterests.length === 0 && (
            <div className="no-interests-found">
              <p>Không tìm thấy chủ đề nào khớp với từ khóa "{searchQuery}".</p>
            </div>
          )}

          {/* Sticky Bottom Bar */}
          <div className="onboarding-bottom-bar">
            <div className="bottom-bar-content">
              <div className="bottom-bar-info">
                {selectedIds.size < MIN_REQUIRED ? (
                  <span className="text-warning">
                    ⚠️ Chọn thêm ít nhất <strong>{MIN_REQUIRED - selectedIds.size}</strong> chủ đề nữa
                  </span>
                ) : (
                  <span className="text-success">
                    🎉 Tuyệt vời! Bạn đã chọn đủ {selectedIds.size} chủ đề
                  </span>
                )}
              </div>

              <div className="bottom-bar-buttons">
                <button
                  type="submit"
                  disabled={selectedIds.size < MIN_REQUIRED || saving}
                  className="btn btn-primary btn-lg"
                  id="submit-interests-btn"
                >
                  {saving ? 'Đang lưu cài đặt...' : 'Tiếp tục vào Bảng tin →'}
                </button>
              </div>
            </div>
          </div>
        </form>
      )}
    </div>
  );
};
