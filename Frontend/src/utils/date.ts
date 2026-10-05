/**
 * Chuẩn hóa định dạng thời gian tương đối cho mạng xã hội
 */
export const formatRelativeTime = (utcDateString?: string | null): string => {
  if (!utcDateString) return '';
  const date = new Date(utcDateString);
  const now = new Date();
  const diffInSeconds = Math.max(0, Math.floor((now.getTime() - date.getTime()) / 1000));

  if (diffInSeconds < 45) return 'Vừa xong';
  if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)} phút`;
  if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)} giờ`;
  if (diffInSeconds < 2592000) return `${Math.floor(diffInSeconds / 86400)} ngày`;

  return date.toLocaleDateString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });
};
