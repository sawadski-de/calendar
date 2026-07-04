const MINUTE_MS = 60_000;

/** Whole minutes elapsed since `timestamp` (or 0 if there's no timestamp yet) — used by both the
 * per-user Settings connections page and the Admin sync overview so "Zuletzt synchronisiert vor N
 * Min." never drifts between the two views for the same connection (code review finding: this was
 * duplicated verbatim in both components). */
export function minutesSince(timestamp: string | null): number {
  if (!timestamp) {
    return 0;
  }
  const elapsedMs = Date.now() - new Date(timestamp).getTime();
  return Math.max(0, Math.floor(elapsedMs / MINUTE_MS));
}
