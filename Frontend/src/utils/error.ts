import { AxiosError, isAxiosError } from 'axios';

/**
 * Trích xuất thông báo lỗi thân thiện cho ASP.NET Core Web API (ProblemDetails & ValidationProblemDetails)
 */
export function getApiErrorMessage(error: unknown): string {
  if (isAxiosError(error)) {
    return getAxiosErrorMessage(error);
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'Đã có lỗi không xác định xảy ra. Vui lòng thử lại.';
}

function getAxiosErrorMessage(error: AxiosError): string {
  const data = error.response?.data as Record<string, unknown> | undefined;

  if (data && typeof data === 'object') {
    // 1. Kiểm tra validation errors từ FluentValidation / ASP.NET ValidationProblemDetails
    if (data.errors && typeof data.errors === 'object') {
      if (Array.isArray(data.errors) && data.errors.length > 0) {
        const first = data.errors[0];
        if (typeof first === 'string') return first;
        if (typeof first === 'object' && first !== null) {
          const obj = first as Record<string, unknown>;
          return (obj.errorMessage ?? obj.message ?? JSON.stringify(first)) as string;
        }
      } else {
        const values = Object.values(data.errors);
        if (values.length > 0 && Array.isArray(values[0]) && values[0].length > 0) {
          return String(values[0][0]);
        }
      }
    }

    // 2. ProblemDetails: detail hoặc message hoặc title
    if (typeof data.detail === 'string' && data.detail.trim()) {
      return data.detail;
    }

    if (typeof data.message === 'string' && data.message.trim()) {
      return data.message;
    }

    if (
      typeof data.title === 'string' &&
      data.title.trim() &&
      data.title !== 'One or more validation errors occurred.'
    ) {
      return data.title;
    }
  }

  // 3. Status-based mapping
  if (error.response?.status === 401) {
    return 'Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại.';
  }

  if (error.response?.status === 403) {
    return 'Bạn không có quyền thực hiện hành động này.';
  }

  if (error.response?.status === 404) {
    return 'Không tìm thấy tài nguyên yêu cầu.';
  }

  if (error.response?.status === 500) {
    return 'Lỗi hệ thống máy chủ. Vui lòng thử lại sau.';
  }

  if (error.message) {
    const msg = error.message.toLowerCase();
    if (msg.includes('network error')) {
      return 'Lỗi kết nối mạng hoặc máy chủ không phản hồi. Vui lòng kiểm tra lại.';
    }
    if (msg.includes('timeout')) {
      return 'Yêu cầu quá thời gian chờ (timeout). Vui lòng thử lại.';
    }
    return error.message;
  }

  return 'Không thể kết nối máy chủ. Vui lòng kiểm tra lại.';
}
