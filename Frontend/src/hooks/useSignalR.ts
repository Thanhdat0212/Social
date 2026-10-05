import { useEffect, useRef } from 'react';
import { signalrService } from '@/services/signalrService';

/**
 * Hook lắng nghe một sự kiện từ SignalR Hub và tự động hủy đăng ký khi component unmount
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function useSignalR<T = any>(
  eventName: string,
  handler: (...args: T[]) => void
) {
  const handlerRef = useRef(handler);
  handlerRef.current = handler;

  useEffect(() => {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const callback = (...args: any[]) => {
      handlerRef.current(...args);
    };

    signalrService.on(eventName, callback);
    return () => {
      signalrService.off(eventName, callback);
    };
  }, [eventName]);
}
