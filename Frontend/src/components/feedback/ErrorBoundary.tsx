import { Component, type ErrorInfo, type ReactNode } from 'react';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends Component<Props, State> {
  public override state: State = {
    hasError: false,
    error: null,
  };

  public static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  public override componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('Uncaught error in React Component Tree:', error, errorInfo);
  }

  private handleReload = () => {
    window.location.reload();
  };

  public override render() {
    if (this.state.hasError) {
      if (this.props.fallback) {
        return this.props.fallback;
      }

      return (
        <div className="auth-container">
          <div className="card auth-card text-center py-8">
            <div className="auth-error-icon">⚠️</div>
            <h2>Đã xảy ra lỗi không mong muốn</h2>
            <p className="text-muted mt-2">
              {this.state.error?.message || 'Ứng dụng gặp sự cố trong quá trình hiển thị giao diện.'}
            </p>
            <div className="mt-6 flex-center gap-3">
              <button onClick={this.handleReload} className="btn btn-primary">
                Tải lại trang
              </button>
            </div>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}
