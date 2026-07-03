import { Appointment } from './appointment.model';
import { computeOverlapLayout } from './overlap-layout';

function appt(id: string, startHour: number, endHour: number): Appointment {
  const day = '2026-07-06';
  return {
    id,
    title: id,
    startUtc: `${day}T${String(startHour).padStart(2, '0')}:00:00.000Z`,
    endUtc: `${day}T${String(endHour).padStart(2, '0')}:00:00.000Z`,
  };
}

describe('computeOverlapLayout', () => {
  it('gives non-overlapping appointments columnCount 1', () => {
    const result = computeOverlapLayout([appt('a', 9, 10), appt('b', 11, 12)]);

    expect(result.every((r) => r.columnCount === 1)).toBe(true);
    expect(result.every((r) => r.columnIndex === 0)).toBe(true);
  });

  it('gives two overlapping appointments columnCount 2 with distinct column indices (AC 6)', () => {
    const result = computeOverlapLayout([appt('a', 9, 10), appt('b', 9, 11)]);

    expect(result).toHaveLength(2);
    expect(result.every((r) => r.columnCount === 2)).toBe(true);
    const indices = result.map((r) => r.columnIndex).sort();
    expect(indices).toEqual([0, 1]);
  });

  it('reuses a freed column for a later, non-overlapping-with-the-first appointment', () => {
    // a: 9-10, b: 9:30-10:30 (overlaps a), c: 10:30-11 (overlaps b, not a) -> c can reuse a's column
    const a: Appointment = { id: 'a', title: 'a', startUtc: '2026-07-06T09:00:00.000Z', endUtc: '2026-07-06T10:00:00.000Z' };
    const b: Appointment = { id: 'b', title: 'b', startUtc: '2026-07-06T09:30:00.000Z', endUtc: '2026-07-06T10:30:00.000Z' };
    const c: Appointment = { id: 'c', title: 'c', startUtc: '2026-07-06T10:30:00.000Z', endUtc: '2026-07-06T11:00:00.000Z' };

    const result = computeOverlapLayout([a, b, c]);

    expect(result).toHaveLength(3);
    // All three are visible (none dropped) — the concrete AC 6 requirement.
    expect(new Set(result.map((r) => r.appointment.id))).toEqual(new Set(['a', 'b', 'c']));
    const byId = Object.fromEntries(result.map((r) => [r.appointment.id, r]));
    expect(byId['a'].columnIndex).not.toBe(byId['b'].columnIndex);
    expect(byId['c'].columnIndex).toBe(byId['a'].columnIndex);
  });

  it('does not mutate the input array', () => {
    const input = [appt('b', 11, 12), appt('a', 9, 10)];
    const copy = [...input];

    computeOverlapLayout(input);

    expect(input).toEqual(copy);
  });
});
