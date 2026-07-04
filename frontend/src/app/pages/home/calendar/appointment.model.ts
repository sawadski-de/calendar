export type AvailabilityStatus = 'Unterbrechbar' | 'BitteNichtStoeren';

export interface Appointment {
  id: string;
  title: string;
  startUtc: string;
  endUtc: string;
  status: AvailabilityStatus;
}

/** One colleague calendar-column slot — status-only, no title (Story 3.1, AC 3). */
export interface ColleagueAppointmentSlot {
  id: string;
  startUtc: string;
  endUtc: string;
  status: AvailabilityStatus;
}

/** Shared shape `CalendarColumn` renders — both `Appointment` (own, title required) and
 *  `ColleagueAppointmentSlot` (colleague, no title) satisfy this structurally (Story 3.1). */
export interface CalendarSlot {
  id: string;
  startUtc: string;
  endUtc: string;
  status: AvailabilityStatus;
  title?: string;
}
