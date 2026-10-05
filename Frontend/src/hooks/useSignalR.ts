import { useEffect } from 'react';
import { signalrService } from '@/services/signalrService';

/**
 * Hook lắng nghe một sự kiện từ SignalR Hub và tự động hủy đăng ký khi component unmount
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function useSignalR<T = any>(
  eventName: string,
  handler: (...args: T[]) => void
) {
  useEffect(() => {
    signalrService.on(eventName, handler);
    return () => {
      signalrService.off(eventName, handler);
    };
  }, [eventName, handler]);
}
