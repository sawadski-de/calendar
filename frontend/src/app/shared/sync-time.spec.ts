import { minutesSince } from './sync-time';

describe('minutesSince', () => {
  it('returns 0 when there is no timestamp yet', () => {
    expect(minutesSince(null)).toBe(0);
  });

  it('returns the whole number of minutes elapsed since the timestamp', () => {
    const threeMinutesAgo = new Date(Date.now() - 3 * 60_000).toISOString();
    expect(minutesSince(threeMinutesAgo)).toBe(3);
  });

  it('never returns a negative number for a timestamp slightly in the future (clock skew)', () => {
    const slightlyInTheFuture = new Date(Date.now() + 5_000).toISOString();
    expect(minutesSince(slightlyInTheFuture)).toBe(0);
  });
});
