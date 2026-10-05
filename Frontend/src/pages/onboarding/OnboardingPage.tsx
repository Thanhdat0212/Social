import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTitle } from '@/hooks/useTitle';
import { interestApi } from '@/api/interestApi';
import type { InterestDto } from '@/types/interest';
import { getApiErrorMessage } from '@/utils/error';
import { ROUTES } from '@/constants/routes';
import {
  Button,
  Alert,
  SearchIcon,
  CheckIcon,
  CloseIcon,
  SparklesIcon,
} from '@/components/common';

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

  const MIN_REQUIRED = 1;

  useEffect(() => {
    const fetchInterests = async () => {
      setLoading(true);
      setError(null);
      try {
        const [data, status] = await Promise.all([
          interestApi.getAllInterests(),
          interestApi.getOnboardingStatus().catch(() => null),
        ]);

        if (status && status.isOnboarded) {
          navigate(ROUTES.HOME, { replace: true });
          return;
        }

        setInterests(data);

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
  }, [navigate]);

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
      setError('Vui lòng chọn ít nhất 1 chủ đề để tiếp tục.');
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
    <div className="container onboarding-page-container">
      <div className="onboarding-header-section">
        <span className="onboarding-pill-tag">
          <SparklesIcon size={14} />
          <span>Cá nhân hóa Bảng tin</span>
        </span>
        <h1 className="onboarding-main-title">
          Bạn quan tâm đến <span className="accent-text">chủ đề nào?</span>
        </h1>
        <p className="onboarding-subtext">
          Chọn các chủ đề bạn yêu thích để hệ thống gợi ý bài viết phù hợp nhất cho trải nghiệm đọc của bạn.
        </p>

        {/* Thanh tìm kiếm & bộ lọc */}
        <div className="onboarding-filter-bar">
          <div className="search-field-wrapper">
            <span className="search-field-icon">
              <SearchIcon size={17} />
            </span>
            <input
              type="text"
              placeholder="Tìm kiếm chủ đề (ví dụ: Công nghệ, Lập trình, AI, Game...)"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="search-field-input"
            />
            {searchQuery && (
              <button
                type="button"
                onClick={() => setSearchQuery('')}
                className="search-field-clear"
                title="Xóa tìm kiếm"
                aria-label="Xóa tìm kiếm"
              >
                <CloseIcon size={14} />
              </button>
            )}
          </div>

          <div className="onboarding-actions-row">
            <span className={`selection-status-badge ${selectedIds.size >= MIN_REQUIRED ? 'is-valid' : ''}`}>
              Đã chọn <strong>{selectedIds.size}</strong> chủ đề
            </span>

            <div className="selection-quick-tools">
              {filteredInterests.length > 0 && searchQuery && (
                <Button
                  type="button"
                  variant="ghost"
                  size="xs"
                  onClick={handleSelectAllFiltered}
                >
                  Chọn tất cả kết quả
                </Button>
              )}
              {selectedIds.size > 0 && (
                <Button
                  type="button"
                  variant="ghost"
                  size="xs"
                  onClick={handleClearSelection}
                  className="text-dim"
                >
                  Bỏ chọn tất cả
                </Button>
              )}
            </div>
          </div>
        </div>
      </div>

      {error && <Alert type="error" message={error} className="mb-4" />}
      {successMsg && <Alert type="success" message={successMsg} className="mb-4" />}

      {/* Grid danh sách sở thích */}
      {loading ? (
        <div className="onboarding-loading-state">
          <span className="spinner-inline" />
          <p>Đang tải danh sách chủ đề...</p>
        </div>
      ) : (
        <form onSubmit={handleSubmit} className="onboarding-form-wrap">
          <div className="interests-grid-stream">
            {filteredInterests.map((interest) => {
              const isSelected = selectedIds.has(interest.id);
              return (
                <div
                  key={interest.id}
                  onClick={() => toggleInterest(interest.id)}
                  className={`interest-tile ${isSelected ? 'is-selected' : ''}`}
                  role="checkbox"
                  aria-checked={isSelected}
                  tabIndex={0}
                  onKeyDown={(e) => {
                    if (e.key === ' ' || e.key === 'Enter') {
                      e.preventDefault();
                      toggleInterest(interest.id);
                    }
                  }}
                >
                  <div className="tile-top-row">
                    <span className="tile-icon">{interest.icon || '🏷️'}</span>
                    <div className={`tile-check ${isSelected ? 'checked' : ''}`}>
                      {isSelected && <CheckIcon size={12} />}
                    </div>
                  </div>
                  <h3 className="tile-title">{interest.name}</h3>
                  {interest.description && (
                    <p className="tile-description">{interest.description}</p>
                  )}
                </div>
              );
            })}
          </div>

          {filteredInterests.length === 0 && (
            <div className="onboarding-empty-search">
              <p>Không tìm thấy chủ đề nào khớp với từ khóa "{searchQuery}".</p>
            </div>
          )}

          {/* Sticky Bottom Action Bar */}
          <div className="onboarding-bottom-dock">
            <div className="dock-content-wrapper">
              <div className="dock-info">
                {selectedIds.size === 0 ? (
                  <span className="dock-hint-warning">
                    Vui lòng chọn ít nhất 1 chủ đề bạn quan tâm
                  </span>
                ) : (
                  <span className="dock-hint-success">
                    ✓ Đã chọn {selectedIds.size} chủ đề
                  </span>
                )}
              </div>

              <div className="dock-actions">
                <Button
                  type="submit"
                  variant="primary"
                  size="md"
                  disabled={selectedIds.size < MIN_REQUIRED || saving}
                  loading={saving}
                  id="submit-interests-btn"
                >
                  {saving
                    ? 'Đang lưu...'
                    : selectedIds.size >= MIN_REQUIRED
                    ? `Tiếp tục vào Bảng tin (${selectedIds.size}) →`
                    : 'Tiếp tục vào Bảng tin →'}
                </Button>
              </div>
            </div>
          </div>
        </form>
      )}
    </div>
  );
};
