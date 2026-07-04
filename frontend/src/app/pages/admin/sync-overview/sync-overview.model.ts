import { CalendarProviderId } from '../../settings/connections/connection.model';

export interface AdminCalendarConnectionRow {
  personId: string;
  personEmail: string;
  provider: CalendarProviderId;
  connected: boolean;
  lastSuccessfulSyncAt: string | null;
  hasError: boolean;
  errorCode: string | null;
}
