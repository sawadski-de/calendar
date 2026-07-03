export type AvailabilityStatus = 'Unterbrechbar' | 'BitteNichtStoeren';

export interface Appointment {
  id: string;
  title: string;
  startUtc: string;
  endUtc: string;
  status: AvailabilityStatus;
}
