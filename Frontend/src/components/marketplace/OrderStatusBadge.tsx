import React from 'react';
import { Badge } from '@/components/common/Badge';

interface OrderStatusBadgeProps {
  status: string;
}

export const OrderStatusBadge: React.FC<OrderStatusBadgeProps> = ({ status }) => {
  switch (status) {
    case 'Delivered':
      return <Badge variant="success" dot>Đã giao mã</Badge>;
    case 'Completed':
      return <Badge variant="primary">Đã hoàn tất</Badge>;
    case 'Paid':
      return <Badge variant="warning">Đang xuất kho</Badge>;
    case 'Pending':
      return <Badge variant="neutral">Chờ thanh toán</Badge>;
    case 'Cancelled':
      return <Badge variant="danger">Đã hủy</Badge>;
    case 'Refunded':
      return <Badge variant="warning">Đã hoàn tiền</Badge>;
    default:
      return <Badge variant="neutral">{status}</Badge>;
  }
};
