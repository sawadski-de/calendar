/** Monday-start week (German convention) containing `date`, at local midnight. */
export function startOfWeek(date: Date): Date {
  const result = startOfDay(date);
  const isoDayOfWeek = (result.getDay() + 6) % 7; // 0 = Monday ... 6 = Sunday
  result.setDate(result.getDate() - isoDayOfWeek);
  return result;
}

export function startOfDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}

export function addDays(date: Date, days: number): Date {
  const result = new Date(date);
  result.setDate(result.getDate() + days);
  return result;
}

export function addMonths(date: Date, months: number): Date {
  const result = new Date(date);
  result.setMonth(result.getMonth() + months);
  return result;
}

/** The 7 local-midnight dates (Mon–Sun) of the week containing `date`. */
export function getWeekDays(date: Date): Date[] {
  const start = startOfWeek(date);
  return Array.from({ length: 7 }, (_, i) => addDays(start, i));
}

export function isSameDay(a: Date, b: Date): boolean {
  return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
}

/** Stable per-day grouping key, independent of locale formatting. */
export function dateKey(date: Date): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

/** Full Mon–Sun grid of local-midnight dates covering the month containing `date`, including the
 *  leading/trailing days of adjacent months needed to fill whole weeks. */
export function getMonthGridDays(date: Date): Date[] {
  const firstOfMonth = new Date(date.getFullYear(), date.getMonth(), 1);
  const lastOfMonth = new Date(date.getFullYear(), date.getMonth() + 1, 0);
  const gridStart = startOfWeek(firstOfMonth);
  const gridEndExclusive = addDays(startOfWeek(lastOfMonth), 7);

  const days: Date[] = [];
  for (let cursor = gridStart; cursor < gridEndExclusive; cursor = addDays(cursor, 1)) {
    days.push(cursor);
  }
  return days;
}

/** Locale-formatted "HH:mm"-style time label, shared by the calendar grid and the detail popover. */
export function formatTime(date: Date): string {
  return date.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' });
}

/** Groups items by the local calendar day their startUtc falls on — generic over anything with a
 *  startUtc timestamp (own `Appointment[]` or a colleague's `ColleagueAppointmentSlot[]`, Story 3.1). */
export function groupByDay<T extends { startUtc: string }>(items: T[]): Map<string, T[]> {
  const map = new Map<string, T[]>();
  for (const item of items) {
    const key = dateKey(new Date(item.startUtc));
    const list = map.get(key) ?? [];
    list.push(item);
    map.set(key, list);
  }
  return map;
}
