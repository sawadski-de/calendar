import { ColleagueAppointmentSlot } from './appointment.model';
import { computeColleagueDayStatuses, hasDndAggregate } from './colleague-day-status';
import { PersonSummary } from './calendar.service';

describe('computeColleagueDayStatuses / hasDndAggregate (Story 3.2)', () => {
  const roster: PersonSummary[] = [
    { id: 'p1', email: 'bjoern@example.com' },
    { id: 'p2', email: 'katharina@example.com' },
  ];
  const day = new Date(2026, 6, 6);

  function slot(status: 'Unterbrechbar' | 'BitteNichtStoeren', startHour: number, endHour: number): ColleagueAppointmentSlot {
    return {
      id: `${status}-${startHour}`,
      startUtc: new Date(2026, 6, 6, startHour, 0).toISOString(),
      endUtc: new Date(2026, 6, 6, endHour, 0).toISOString(),
      status,
    };
  }

  it('returns one entry per requested person, defaulting to Unterbrechbar for zero appointments (AD-5)', () => {
    const result = computeColleagueDayStatuses(['p1', 'p2'], roster, {}, day);

    expect(result).toEqual([
      { personId: 'p1', email: 'bjoern@example.com', status: 'Unterbrechbar' },
      { personId: 'p2', email: 'katharina@example.com', status: 'Unterbrechbar' },
    ]);
  });

  it('resolves to BitteNichtStoeren when the person has a mix of statuses that day (strictest wins)', () => {
    const colleagueAppointments = { p1: [slot('Unterbrechbar', 9, 10), slot('BitteNichtStoeren', 14, 15)] };

    const result = computeColleagueDayStatuses(['p1'], roster, colleagueAppointments, day);

    expect(result[0].status).toBe('BitteNichtStoeren');
  });

  it('ignores appointments on other days', () => {
    const otherDay = new Date(2026, 6, 7, 9, 0);
    const colleagueAppointments = {
      p1: [{ id: 'x', startUtc: otherDay.toISOString(), endUtc: new Date(2026, 6, 7, 10, 0).toISOString(), status: 'BitteNichtStoeren' as const }],
    };

    const result = computeColleagueDayStatuses(['p1'], roster, colleagueAppointments, day);

    expect(result[0].status).toBe('Unterbrechbar');
  });

  it('hasDndAggregate is true iff at least one selected person is BitteNichtStoeren that day', () => {
    const colleagueAppointments = { p1: [slot('BitteNichtStoeren', 9, 10)] };

    expect(hasDndAggregate(['p1', 'p2'], roster, colleagueAppointments, day)).toBe(true);
    expect(hasDndAggregate(['p2'], roster, colleagueAppointments, day)).toBe(false);
  });

  it('hasDndAggregate is false for an empty selection', () => {
    expect(hasDndAggregate([], roster, {}, day)).toBe(false);
  });
});
