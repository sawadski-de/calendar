import { AvailabilityStatus, ColleagueAppointmentSlot } from './appointment.model';
import { PersonSummary } from './calendar.service';
import { dateKey, groupByDay } from './date-utils';

export interface ColleagueDayStatus {
  personId: string;
  email: string;
  status: AvailabilityStatus;
}

export type ColleagueDayIndex = Record<string, Map<string, ColleagueAppointmentSlot[]>>;

/**
 * Groups every selected colleague's slots by day once. Callers that need per-day statuses for many
 * days (e.g. `MonthView`'s ~35–42 visible cells) must build this once per `colleagueAppointments()`
 * change (wrap in a `computed()`) and pass it to `computeColleagueDayStatusesFromIndex` per day —
 * re-grouping a person's full appointment array from scratch on every single-day query is the exact
 * cost this index avoids (code review finding, Story 3.2).
 */
export function buildColleagueDayIndex(colleagueAppointments: Record<string, ColleagueAppointmentSlot[]>): ColleagueDayIndex {
  const index: ColleagueDayIndex = {};
  for (const personId of Object.keys(colleagueAppointments)) {
    index[personId] = groupByDay(colleagueAppointments[personId]);
  }
  return index;
}

/**
 * One entry per requested person, in the given order — a person with zero appointments that day
 * defaults to `Unterbrechbar` (AD-5), a person with a mix of statuses that day resolves to
 * `BitteNichtStoeren` (strictest-wins, same principle as AD-6's overlap tie-break, applied here across
 * one person's appointments within one day rather than across two overlapping ones).
 */
export function computeColleagueDayStatusesFromIndex(
  personIds: string[],
  roster: PersonSummary[],
  index: ColleagueDayIndex,
  date: Date
): ColleagueDayStatus[] {
  const key = dateKey(date);

  return personIds.map((personId) => {
    const slotsToday = index[personId]?.get(key) ?? [];
    const status: AvailabilityStatus = slotsToday.some((slot) => slot.status === 'BitteNichtStoeren')
      ? 'BitteNichtStoeren'
      : 'Unterbrechbar';
    const email = roster.find((person) => person.id === personId)?.email ?? '';

    return { personId, email, status };
  });
}

/**
 * Convenience wrapper for a single-day, low-frequency query (e.g. a test, or a one-off lookup) —
 * builds the index just for this call. Any call site that queries many days for the same
 * `colleagueAppointments` (like `MonthView`'s cell grid) must build the index once via
 * `buildColleagueDayIndex` instead of going through this wrapper per day.
 */
export function computeColleagueDayStatuses(
  personIds: string[],
  roster: PersonSummary[],
  colleagueAppointments: Record<string, ColleagueAppointmentSlot[]>,
  date: Date
): ColleagueDayStatus[] {
  return computeColleagueDayStatusesFromIndex(personIds, roster, buildColleagueDayIndex(colleagueAppointments), date);
}

/** The per-cell aggregate-marker condition (Story 3.2, AC 1/2). */
export function hasDndAggregate(
  personIds: string[],
  roster: PersonSummary[],
  colleagueAppointments: Record<string, ColleagueAppointmentSlot[]>,
  date: Date
): boolean {
  return computeColleagueDayStatuses(personIds, roster, colleagueAppointments, date).some(
    (entry) => entry.status === 'BitteNichtStoeren'
  );
}
