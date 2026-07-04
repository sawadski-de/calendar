export type CalendarProviderId = 'Google' | 'Outlook';

export interface CalendarConnectionStatus {
  provider: CalendarProviderId;
  connected: boolean;
  lastSuccessfulSyncAt: string | null;
  hasError: boolean;
  errorCode: string | null;
}
