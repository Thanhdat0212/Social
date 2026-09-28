import { useEffect } from 'react';
import { ENV } from '@/config';

/**
 * Custom hook đồng bộ tiêu đề trang (document.title)
 */
export function useTitle(title: string) {
  useEffect(() => {
    const prevTitle = document.title;
    document.title = title ? `${title} | ${ENV.APP_NAME}` : ENV.APP_NAME;

    return () => {
      document.title = prevTitle;
    };
  }, [title]);
}
