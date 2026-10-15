import React, { useState, useEffect, useMemo } from 'react';
import { adminApi } from '@/api/adminApi';
import type {
  AdminCategoryNode,
  AdminAttributeDefinition,
  AdminCategoryRequestItem,
} from '@/types/admin';
import { CategoryTreeSubTab } from './categories/CategoryTreeSubTab';
import { CategoryAttributesSubTab } from './categories/CategoryAttributesSubTab';
import { CategoryRequestsSubTab } from './categories/CategoryRequestsSubTab';

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
      loadAttributes(); // Cần attributes cho modal gán thuộc tính
    } else if (subTab === 'attributes') {
      loadAttributes();
    } else if (subTab === 'requests') {
      loadRequests();
      loadCategoryTree(); // Cần danh mục cho modal gán
    }
  }, [subTab, requestFilter]);

  // Flatten category tree for select dropdowns
  const allFlatCategories = useMemo(() => {
    const flattenWithDepth = (nodes: AdminCategoryNode[], depth: number = 0): { id: string; name: string; depth: number }[] => {
      let result: { id: string; name: string; depth: number }[] = [];
      for (const node of nodes) {
        result.push({ id: node.id, name: node.name, depth });
        if (node.children && node.children.length > 0) {
          result = result.concat(flattenWithDepth(node.children, depth + 1));
        }
      }
      return result;
    };
    return flattenWithDepth(categoryTree);
  }, [categoryTree]);

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
      </div>

      {loading && (
        <div style={{ padding: '2rem', textAlign: 'center' }}>
          <div className="spinner" style={{ margin: '0 auto 0.5rem' }} />
          <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>Đang nạp dữ liệu danh mục & thuộc tính...</p>
        </div>
      )}

      {/* SUB-VIEW 1: CATEGORY TREE */}
      {subTab === 'tree' && (
        <CategoryTreeSubTab
          categoryTree={categoryTree}
          allFlatCategories={allFlatCategories}
          attributes={attributes}
          loadCategoryTree={loadCategoryTree}
          showFlash={showFlash}
        />
      )}

      {/* SUB-VIEW 2: ATTRIBUTE DEFINITIONS */}
      {subTab === 'attributes' && (
        <CategoryAttributesSubTab
          attributes={attributes}
          loadAttributes={loadAttributes}
          showFlash={showFlash}
        />
      )}

      {/* SUB-VIEW 3: CATEGORY REQUESTS */}
      {subTab === 'requests' && (
        <CategoryRequestsSubTab
          requests={requests}
          allFlatCategories={allFlatCategories}
          loadRequests={loadRequests}
          showFlash={showFlash}
          requestFilter={requestFilter}
          setRequestFilter={setRequestFilter}
        />
      )}
    </div>
  );
};
