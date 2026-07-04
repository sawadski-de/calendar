export interface OverlapLayoutItem<T> {
  appointment: T;
  columnIndex: number;
  columnCount: number;
}

/**
 * Story 1.2, AC 6: two or more overlapping appointments must both render visibly, side by side,
 * rather than one hiding the other. Groups mutually/transitively overlapping appointments (tracking
 * a running group-end time) and greedily assigns each a column — the first column whose previous
 * occupant has already ended, or a new column if none is free. Pure and side-effect-free so it can
 * be unit-tested directly instead of only through rendered DOM/CSS assertions.
 *
 * Generic over anything with start/end timestamps — the algorithm only ever reads those two fields, so
 * both own `Appointment[]` and a colleague's `ColleagueAppointmentSlot[]` (Story 3.1) share this one
 * implementation rather than a second copy of the layout logic.
 */
export function computeOverlapLayout<T extends { startUtc: string; endUtc: string }>(appointments: T[]): OverlapLayoutItem<T>[] {
  const sorted = [...appointments].sort(
    (a, b) => new Date(a.startUtc).getTime() - new Date(b.startUtc).getTime()
  );

  const result: OverlapLayoutItem<T>[] = [];
  let group: T[] = [];
  let groupEndMs = Number.NEGATIVE_INFINITY;

  const flushGroup = () => {
    if (group.length === 0) {
      return;
    }

    const columnEndsMs: number[] = [];
    const assignments: { appointment: T; columnIndex: number }[] = [];

    for (const appointment of group) {
      const startMs = new Date(appointment.startUtc).getTime();
      const endMs = new Date(appointment.endUtc).getTime();
      const freeColumn = columnEndsMs.findIndex((end) => end <= startMs);

      if (freeColumn >= 0) {
        columnEndsMs[freeColumn] = endMs;
        assignments.push({ appointment, columnIndex: freeColumn });
      } else {
        columnEndsMs.push(endMs);
        assignments.push({ appointment, columnIndex: columnEndsMs.length - 1 });
      }
    }

    const columnCount = columnEndsMs.length;
    for (const assignment of assignments) {
      result.push({ ...assignment, columnCount });
    }
  };

  for (const appointment of sorted) {
    const startMs = new Date(appointment.startUtc).getTime();
    if (group.length > 0 && startMs >= groupEndMs) {
      flushGroup();
      group = [];
      groupEndMs = Number.NEGATIVE_INFINITY;
    }

    group.push(appointment);
    groupEndMs = Math.max(groupEndMs, new Date(appointment.endUtc).getTime());
  }
  flushGroup();

  return result;
}
