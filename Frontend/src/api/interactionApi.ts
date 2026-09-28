import { apiClient } from './client';
import type {
  BatchTrackInteractionsRequestDto,
  TrackInteractionRequestDto,
} from '@/types';

export const interactionApi = {
  /**
   * Ghi nhận 1 sự kiện tương tác
   */
  track: async (data: TrackInteractionRequestDto) => {
    try {
      await apiClient.post('/interactions/track', data);
    } catch {
      // Background tracking silently ignores errors to avoid disturbing UX
    }
  },

  /**
   * Ghi nhận hàng loạt sự kiện tương tác
   */
  trackBatch: async (data: BatchTrackInteractionsRequestDto) => {
    try {
      await apiClient.post('/interactions/track-batch', data);
    } catch {
      // Background tracking silently ignores errors
    }
  },
};
