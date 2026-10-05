import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { ENV } from '@/config';
import { useAuthStore } from '@/store';

class SignalRService {
  private connection: HubConnection | null = null;
  private isStarting = false;

  private createConnection(): HubConnection {
    return new HubConnectionBuilder()
      .withUrl(ENV.HUB_URL, {
        accessTokenFactory: () => {
          const token = useAuthStore.getState().accessToken;
          return token || '';
        },
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(ENV.IS_DEV ? LogLevel.Information : LogLevel.Error)
      .build();
  }

  public async start(): Promise<void> {
    if (this.connection && this.connection.state === HubConnectionState.Connected) {
      return;
    }

    if (this.isStarting) {
      return;
    }

    if (!this.connection) {
      this.connection = this.createConnection();

      this.connection.onreconnecting((error) => {
        if (ENV.IS_DEV) {
          console.warn('[SignalR] Đang thử kết nối lại do gián đoạn:', error?.message);
        }
      });

      this.connection.onreconnected((connectionId) => {
        if (ENV.IS_DEV) {
          console.log('[SignalR] Đã kết nối lại thành công, ConnectionId:', connectionId);
        }
      });

      this.connection.onclose((error) => {
        if (ENV.IS_DEV && error) {
          console.error('[SignalR] Kết nối đã đóng với lỗi:', error.message);
        }
      });
    }

    try {
      this.isStarting = true;
      await this.connection.start();
      if (ENV.IS_DEV) {
        console.log('[SignalR] Đã kết nối thành công tới SocialHub.');
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
      }
    }
  }

  public on<T = unknown>(eventName: string, newMethod: (...args: T[]) => void): void {
    if (!this.connection) {
      this.connection = this.createConnection();
    }
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    this.connection.on(eventName, newMethod as any);
  }

  public off<T = unknown>(eventName: string, method?: (...args: T[]) => void): void {
    if (this.connection) {
      if (method) {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        this.connection.off(eventName, method as any);
      } else {
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

  public isConnected(): boolean {
    return this.connection?.state === HubConnectionState.Connected;
  }
}

export const signalrService = new SignalRService();
