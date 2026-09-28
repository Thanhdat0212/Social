export const InteractionType = {
  View: 1,
  Like: 2,
  Comment: 3,
  Share: 4,
  Save: 5,
  Follow: 6,
  Skip: 7,
  NotInterested: 8,
} as const;

export type InteractionType = (typeof InteractionType)[keyof typeof InteractionType];

export interface TrackInteractionRequestDto {
  postId?: string;
  interactionType: InteractionType;
  value?: number;
  metadata?: string;
}

export interface BatchTrackInteractionsRequestDto {
  events: TrackInteractionRequestDto[];
}
