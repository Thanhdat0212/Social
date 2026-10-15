import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { ENV } from '@/config';
import { useAuthStore } from '@/store';

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type EventHandler = (...args: any[]) => void;

class SignalRService {
  private connection: HubConnection | null = null;
  private isStarting = false;
  private lastConnectedToken: string | null = null;
  private listeners: Map<string, Set<EventHandler>> = new Map();

  private createConnection(): HubConnection {
    const conn = new HubConnectionBuilder()
      .withUrl(ENV.HUB_URL, {
        accessTokenFactory: () => {
          const token = useAuthStore.getState().accessToken;
          return token || '';
        },
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(ENV.IS_DEV ? LogLevel.Information : LogLevel.Error)
      .build();

    // Đăng ký lại toàn bộ sự kiện đã lắng nghe lên kết nối mới
    this.listeners.forEach((handlers, eventName) => {
      handlers.forEach((handler) => {
        conn.on(eventName, handler);
      });
    });

    conn.onreconnecting((error) => {
      if (ENV.IS_DEV) {
        console.warn('[SignalR] Đang thử kết nối lại do gián đoạn:', error?.message);
      }
    });

    conn.onreconnected((connectionId) => {
      if (ENV.IS_DEV) {
        console.log('[SignalR] Đã kết nối lại thành công, ConnectionId:', connectionId);
      }
    });

    conn.onclose((error) => {
      if (ENV.IS_DEV && error) {
        console.error('[SignalR] Kết nối đã đóng với lỗi:', error.message);
      }
    });

    return conn;
  }

  public async start(): Promise<void> {
    const currentToken = useAuthStore.getState().accessToken || null;

    // Nếu token thay đổi (ví dụ: đăng nhập, đổi tài khoản, hoặc đăng xuất),
    // ngắt kết nối hiện tại để thiết lập kết nối mới với quyền và Group chính xác
    if (this.connection && this.connection.state === HubConnectionState.Connected) {
      if (this.lastConnectedToken === currentToken) {
        return;
      }
      try {
        await this.connection.stop();
      } catch {
        // bỏ qua lỗi khi ngắt kết nối cũ
      }
      this.connection = null;
    }

    if (this.isStarting) {
      return;
    }

    if (!this.connection) {
      this.connection = this.createConnection();
    }

    try {
      this.isStarting = true;
      await this.connection.start();
      this.lastConnectedToken = currentToken;
      if (ENV.IS_DEV) {
        console.log('[SignalR] Đã kết nối thành công tới SocialHub. Authenticated:', !!currentToken);
      }
    } catch (err) {
      if (ENV.IS_DEV) {
        console.warn('[SignalR] Không thể khởi tạo kết nối tới SocialHub:', err);
      }
    } finally {
      this.isStarting = false;
    }
  }

  public async stop(): Promise<void> {
    if (this.connection) {
      try {
        await this.connection.stop();
        if (ENV.IS_DEV) {
          console.log('[SignalR] Đã ngắt kết nối SocialHub.');
        }
      } catch (err) {
        console.error('[SignalR] Lỗi khi ngắt kết nối:', err);
      } finally {
        this.connection = null;
        this.lastConnectedToken = null;
      }
    }
  }

  public on<T = unknown>(eventName: string, newMethod: (...args: T[]) => void): void {
    const handler = newMethod as EventHandler;
    if (!this.listeners.has(eventName)) {
      this.listeners.set(eventName, new Set());
    }
    this.listeners.get(eventName)!.add(handler);

    if (this.connection) {
      this.connection.on(eventName, handler);
    }
  }

  public off<T = unknown>(eventName: string, method?: (...args: T[]) => void): void {
    if (method) {
      const handler = method as EventHandler;
      this.listeners.get(eventName)?.delete(handler);
      if (this.connection) {
        this.connection.off(eventName, handler);
      }
    } else {
      this.listeners.delete(eventName);
      if (this.connection) {
        this.connection.off(eventName);
      }
    }
  }

  public async joinPostGroup(postId: string): Promise<void> {
    if (this.connection && this.connection.state === HubConnectionState.Connected) {
      try {
        await this.connection.invoke('JoinPostGroup', postId);
      } catch (err) {
        console.error(`[SignalR] Lỗi khi tham gia group bài viết post_${postId}:`, err);
      }
    }
  }

  public async leavePostGroup(postId: string): Promise<void> {
    if (this.connection && this.connection.state === HubConnectionState.Connected) {
      try {
        await this.connection.invoke('LeavePostGroup', postId);
      } catch (err) {
        console.error(`[SignalR] Lỗi khi rời group bài viết post_${postId}:`, err);
      }
    }
  }

  public async joinCheckoutGroup(checkoutId: string): Promise<void> {
    if (this.connection && this.connection.state === HubConnectionState.Connected) {
      try {
        await this.connection.invoke('JoinCheckoutGroup', checkoutId);
      } catch (err) {
        console.error(`[SignalR] Lỗi khi tham gia group checkout_${checkoutId}:`, err);
      }
    }
  }

  public async leaveCheckoutGroup(checkoutId: string): Promise<void> {
    if (this.connection && this.connection.state === HubConnectionState.Connected) {
      try {
        await this.connection.invoke('LeaveCheckoutGroup', checkoutId);
      } catch (err) {
        console.error(`[SignalR] Lỗi khi rời group checkout_${checkoutId}:`, err);
      }
    }
  }

  public isConnected(): boolean {
    return this.connection?.state === HubConnectionState.Connected;
  }
}

export const signalrService = new SignalRService();
