---
baseline_commit: a24f3f9f98f2bdfd6348d71cace8585cd423c988
---

# Story 3.2: Mehrpersonen-Monatsansicht mit Aggregat-Popover

Status: done

## Story

As a Teammitglied,
I want in der Monatsansicht auf einen Blick sehen, an welchen Tagen ausgewählte Kollegen "Bitte nicht stören" haben,
so that ich Kollisionen und ungünstige Tage für Anfragen früh erkenne, ohne jeden Tag einzeln zu öffnen.

## Acceptance Criteria

1. **Given** ich habe Kollegen ausgewählt und befinde mich in der Monatsansicht, **when** ein Tag rendert, an dem mindestens ein ausgewählter Kollege irgendwann "Bitte nicht stören" ist, **then** zeigt die Tageszelle einen dezenten Aggregat-Marker — keine einzelnen Punkte/Balken pro Person, keine überladene Zelle.
2. **Given** eine Tageszelle ohne "Bitte nicht stören"-Kollegen, **when** sie rendert, **then** bleibt sie visuell sauber ohne jegliche Marker.
3. **Given** eine Tageszelle mit Aggregat-Marker, **when** ich sie anklicke oder per Tastatur (`Enter`/`Space` auf fokussierter Zelle) aktiviere, **then** öffnet sich ein Popover mit Name + Status-Badge jedes ausgewählten Teammitglieds für diesen Tag.
4. **Given** dasselbe Popover, **when** ich es auf einem Touch-Gerät öffne, **then** ist das Verhalten identisch zum Pointer-Gerät (Tap statt Klick, kein Hover-Vorschau-Unterschied).
5. **Given** das Popover ist geöffnet, **when** ich `Esc` drücke oder außerhalb klicke, **then** schließt es sich und der Fokus kehrt zur auslösenden Tageszelle zurück.

## Tasks / Subtasks

- [x] **Task 1 — Frontend: per-day colleague status aggregation (pure helper)** (AC: 1, 2, 3)
  - [x] New file `frontend/src/app/pages/home/calendar/colleague-day-status.ts`: `export interface ColleagueDayStatus { personId: string; email: string; status: AvailabilityStatus; }` and `export function computeColleagueDayStatuses(personIds: string[], roster: PersonSummary[], colleagueAppointments: Record<string, ColleagueAppointmentSlot[]>, date: Date): ColleagueDayStatus[]`. For each `personId` in `personIds` (in the given order — this determines popover row order, no sort requirement in any AC): filter that person's slots to the ones whose `startUtc` falls on `date` (reuse `dateKey`/`groupByDay` from `date-utils.ts` — do **not** write a second day-bucketing routine), then `status = slots.some(s => s.status === 'BitteNichtStoeren') ? 'BitteNichtStoeren' : 'Unterbrechbar'` — same "strictest value wins" reduction principle the Architecture Spine already uses for a single person's overlapping appointments (AD-6), applied here across one person's appointments *within one day* rather than across two overlapping ones; a person with zero appointments that day defaults to `Unterbrechbar`, matching AD-5's "no active appointment → Unterbrechbar" default. Look up `email` from `roster` (fallback `''` if not found, same defensive pattern as `Home.colleagueLabel`).
  - [x] `export function hasDndAggregate(personIds: string[], roster: PersonSummary[], colleagueAppointments: Record<string, ColleagueAppointmentSlot[]>, date: Date): boolean` — `true` iff `computeColleagueDayStatuses(...)` contains at least one `BitteNichtStoeren`. This is the per-cell marker condition (AC 1/2); pure and unit-testable without rendering a component.
- [x] **Task 2 — Frontend: `MonthView` gains colleague inputs, aggregate marker, and popover** (AC: 1, 2, 3, 4, 5)
  - [x] `month-view.ts`: add `readonly roster = input<PersonSummary[]>([])`, `readonly selectedPersonIds = input<string[]>([])`, `readonly colleagueAppointments = input<Record<string, ColleagueAppointmentSlot[]>>({})`. Extend the `cells()` computed's per-cell shape with `hasDndAggregate: boolean` (Task 1's `hasDndAggregate(...)` called per cell date) — computing this for every visible cell (~35–42 cells) on every `cells()` recompute is cheap (bounded by `selectedPersonIds().length`, no re-fetch), no memoization needed beyond the existing computed.
  - [x] Add `readonly openPopoverDate = signal<Date | null>(null)` and `readonly popoverStatuses = computed<ColleagueDayStatus[]>(...)` (empty array when `openPopoverDate()` is null, else `computeColleagueDayStatuses(this.selectedPersonIds(), this.roster(), this.colleagueAppointments(), openPopoverDate()!)`). `openPopover(date: Date): void` / `closePopover(): void` methods — only one popover open at a time (opening a new date's popover implicitly replaces any previous one, consistent with how every other floating layer in this app behaves).
  - [x] `month-view.html`: inside each `.month-view__cell`, after the existing title/`+N` block, add `@if (cell.hasDndAggregate) { <span class="month-view__aggregate-marker" appActivatable (activate)="openPopover(cell.date)" [attr.aria-label]="...">...</span> }`. **Design decision (not fully pinned down by the AC's grammar — "sie" could mean the marker or the whole cell):** make only the marker element itself the activation target, not the entire cell — the cell already hosts independently-activatable own-appointment titles (existing `appActivatable` per title, Story 1.2/1.4), and layering a second, cell-wide `appActivatable` on top would double-fire on a title click (the native click bubbles to the cell). A small dedicated marker (dot + short caption, e.g. "{{count}} Bitte nicht stören" via a new i18n key) avoids that conflict entirely and still satisfies AC 1's "dezenter Marker, keine überladene Zelle."
  - [x] Popover markup: when `openPopoverDate()` matches `cell.date` (same day via `isSameDay`, imported from `date-utils.ts`), give that specific `.month-view__cell` a `[class.month-view__cell--expanded]` (`position: relative`, per the UX mockup's `.day-cell.expanded`) and nest `<div class="month-view__popover" appFocusTrap (keydown.escape)="closePopover()">` inside it, absolutely positioned (`top: calc(100% + 4px)`, matching the person-selector dropdown's own positioning convention from Story 3.1 rather than inventing a new one). Contents: a heading with the formatted date (reuse/extend `date-utils.ts` rather than a third ad-hoc date-formatting call site — check `formatTime`'s neighboring exports first), then one row per `popoverStatuses()` entry (`<app-status-badge [status]="entry.status" />` + `{{ entry.email }}` — reuses the exact component built in Story 1.3, no new badge visual), then the existing `calendar.detailPrivacyNote` i18n string as a footer hint (Story 3.1 already introduced this exact copy — "Details sind privat — nur der Status ist sichtbar." — reuse the key verbatim, do not add a duplicate `monthPopoverHint` key with the same German text).
  - [x] Outside-click-to-close (AC 5, distinct from the two full-screen modal popovers elsewhere in this app which use a *dimmed* backdrop): add a `<div class="month-view__popover-backdrop" (click)="closePopover()">` sibling rendered alongside the popover, `position: fixed; inset: 0; background: transparent;` (no dimming — the mockup shows the rest of the month grid staying fully visible, unlike `appointment-detail`/`appointment-create`'s dimmed backdrops) at a `z-index` below the popover panel but above everything else, so a click anywhere outside the popover (including on another cell) closes it without also triggering that other cell's own click handler underneath — confirm this ordering doesn't accidentally swallow a legitimate click on the "+" chip / person-selector if the popover happens to be open when the user clicks there (acceptable: closing the popover on that click and requiring a second click to interact with the person-selector matches how every other floating layer + backdrop combo in this app already behaves, e.g. `appointment-create`'s backdrop).
  - [x] Focus: `appFocusTrap` on `.month-view__popover` already restores focus to whatever had focus before it opened (same directive as `appointment-create`/`appointment-detail`/`person-selector`, Story 1.3/3.1) — since the marker element is what receives focus on open (click or `Enter`/`Space`), this satisfies AC 5's "Fokus kehrt zur auslösenden Tageszelle zurück" for free, no new focus-management code needed; verify this in the component test rather than assuming.
- [x] **Task 3 — Frontend: wire `Home` to pass colleague data into `MonthView` for all views** (AC: 1, 2, 3)
  - [x] `home.html`: remove the `@if (viewType() !== 'month')` guard around `<app-person-selector>` — Story 3.1 deliberately scoped the selector to week/day only and deferred month to this story; month view now needs the exact same selector (same `selectedPersonIds`/`roster` bindings, same `onSelectionChanged` handler — no new wiring on the `Home` side beyond what Story 3.1 already built, since `loadColleagueAppointments()` already fetches for the whole visible month grid range regardless of `viewType()`).
  - [x] `<app-month-view>`'s binding: add `[roster]="roster()"`, `[selectedPersonIds]="selectedPersonIds()"`, `[colleagueAppointments]="colleagueAppointments()"` alongside the existing `[focusDate]`/`[appointments]`/`(appointmentActivated)` bindings.
- [x] **Task 4 — i18n** (AC: 1, 3)
  - [x] `frontend/public/i18n/de.json`/`en.json` + `frontend/src/app/testing/transloco-testing.ts`: add `calendar.monthAggregateMarker` ("{{count}} Bitte nicht stören" / "{{count}} do not disturb") under the existing `calendar.*` namespace. **Do not** add a new privacy-note key — reuse `calendar.detailPrivacyNote` (Story 3.1) verbatim in the popover footer.
  - [x] `month-view.ts`/`.html` currently import **no** `TranslocoPipe` at all (checked: no i18n strings rendered there today) — this story is the first to need one; add the import to the component and its test's `TestBed` module (`getTranslocoTestingModule()`, same as every other component spec).
- [x] **Task 5 — Tests** (AC: all)
  - [x] `colleague-day-status.spec.ts` (new): `computeColleagueDayStatuses` returns one entry per requested person id including a person with zero appointments that day (defaults to `Unterbrechbar`); a person with both an `Unterbrechbar` and a `BitteNichtStoeren` slot the same day resolves to `BitteNichtStoeren` (strictest-wins); `hasDndAggregate` is `true` iff at least one selected person resolves to `BitteNichtStoeren` that day, `false` for an empty selection.
  - [x] `month-view.spec.ts` (extend): no marker renders when no colleague is `BitteNichtStoeren` that day (AC 2); marker renders and is keyboard-activatable (`Enter`/`Space`) when at least one colleague is (AC 1); clicking/activating the marker opens a popover listing every selected colleague's email + status badge for that day (AC 3), including one with zero appointments that day (defaults to `Unterbrechbar` in the popover, not omitted); Esc and an outside click (via the backdrop) both close the popover; closing returns focus to the marker (verify via `document.activeElement`, mirroring how `person-selector.spec.ts` or `appointment-detail.spec.ts` could verify this pattern — check whether either already asserts focus-return and copy that assertion style if so, otherwise assert directly).
  - [x] `home.spec.ts` (extend if needed): the person-selector now also renders in Month view (`viewType() === 'month'` no longer hides it) — one small test confirming `app-person-selector` is present after switching to Month view.

## Dev Notes

### This story is entirely frontend — no backend changes

Unlike Story 3.1, this story needs **zero** new API surface: Story 3.1's `GET /api/appointments/colleagues` bulk endpoint already returns everything needed (per-slot `startUtc`/`status` for every selected colleague across the whole visible month-grid range — `Home.loadColleagueAppointments()` already fetches that range unconditionally, regardless of `viewType()`). The aggregate/popover logic is a pure client-side reduction over data Story 3.1 already delivers. Do not add a new endpoint, a new `IAppointmentViewService` method, or any per-day aggregation on the server — that would be a second, redundant implementation of a computation the client can already do from data it already has.

### Architecture compliance

- **AD-3** is already fully satisfied by Story 3.1's `GetForViewersAsync` — this story reads only `Status`/`StartUtc`/`EndUtc` from that response, never anything that could leak a title (the response shape has no `Title` field to begin with, see `ColleagueAppointmentSlotResponse`). No new privacy-sensitive surface is introduced.
- **AD-5/AD-6-style "strictest value wins"** is reused as a *pattern*, not literally AD-6 itself (AD-6's `CurrentStatusService`/`StatusOverride` don't exist yet — that's Epic 4). This story's `computeColleagueDayStatuses` independently applies the same *principle* (worse status wins when a person has more than one relevant appointment) for "which status badge represents person X's whole day" — a distinct, narrower question than AD-6's "current status right now." Do not attempt to wire this into a `CurrentStatusService` that doesn't exist yet.
- **UX-DR8** is this story's primary UX-Spine scope (month-day-cell + popover, Option C) — see epics.md Story 3.2 "Enthält zusätzlich" cross-reference to UX-DR8/UX-DR19.

### Existing code this story touches (read before changing)

- `frontend/src/app/pages/home/calendar/month-view/month-view.ts`/`.html`/`.css` — currently renders only the viewer's own appointments (title + "+N more" overflow), no i18n, no colleague awareness at all. This story adds three new inputs and the marker/popover — the existing own-appointment title rendering and its `appActivatable`/`appointmentActivated` wiring must not change (Story 1.2/1.4 regression risk if the new marker's click handling interferes with title clicks — see Task 2's note on why the marker, not the whole cell, is the activation target).
- `frontend/src/app/pages/home/home.html` — the `@if (viewType() !== 'month')` guard around `<app-person-selector>` was Story 3.1's deliberate scope boundary (see that story's Task 9 note: "month is Story 3.2's scope") — this story is what removes it.
- `frontend/src/app/pages/home/calendar/date-utils.ts` — reuse `dateKey`/`groupByDay`/`isSameDay` for all day-bucketing in Task 1/2; do not write a second bucketing routine.
- `frontend/src/app/pages/home/calendar/status-badge/status-badge.ts` — reused as-is in the popover, no changes.

### Previous Story Intelligence (3.1 → 3.2)

- Story 3.1 built `PersonSelector`, the `Home`-level `selectedPersonIds`/`roster`/`colleagueAppointments` state, and `loadColleagueAppointments()` — all of it is reused verbatim here; this story's `Home`-side changes are limited to Task 3's two small template edits (remove the month guard, add three bindings to `<app-month-view>`).
- Story 3.1's Dev Agent Record flagged that `Domain.Person` has no `DisplayName` (email is the display name everywhere) — the popover shows email too, consistent with the person-selector and every other attendee-listing surface.
- Story 3.1's Dev Agent Record also flagged a real bug it found by testing rather than by reasoning (`Home.isEmpty` not accounting for a colleague selection) — a reminder that this story's red-green testing (Task 5) may similarly surface an interaction the design didn't anticipate; if so, fix it and document it the same way rather than silently working around it in the test.
- Story 3.1 established the `appFocusTrap` + manual `(keydown.escape)` + backdrop-click convention for every floating layer in this app (`appointment-create`, `appointment-detail`, `person-selector`) — this story's month-popover is the fourth instance of the exact same pattern; do not invent a different one.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Epic 3, Story 3.2] — full AC text.
- [Source: _bmad-output/implementation-artifacts/3-1-personen-selektor-mehrpersonen-ansicht-tag-woche-mit-privat-default.md] — `PersonSelector`, `Home` colleague-fetch state, `ColleagueAppointmentSlot` model, `calendar.detailPrivacyNote` i18n key, all reused here.
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-calendar-neu-bmad-2026-07-02/mockups/key-month-view.html] — `.legend`/`.month-grid`/`.day-cell`/`.month-popover`/`.pop-row` class structure (legend UI itself is not part of this story's ACs — the marker's own caption text covers the "what does this mean" need without a separate persistent legend element).
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-calendar-neu-bmad-2026-07-02/DESIGN.md#Components] — month-day-cell + popover token/behavior description (Option C).
- [Source: frontend/src/app/pages/home/calendar/month-view/] — existing component this story extends.
- [Source: frontend/src/app/shared/focus-trap/, frontend/src/app/shared/activatable/] — reused directives.

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

- No design-vs-implementation surprises this time (unlike Story 3.1, whose `Home.isEmpty` bug only surfaced under test) — the aggregate/popover logic is a pure client-side reduction over data Story 3.1 already fetches, and both the new pure-function tests and the component tests passed on the first run.
- Confirmed (rather than assumed) that jsdom's synthetic `click()`/`dispatchEvent(MouseEvent(...))` does **not** auto-focus a target element the way a real browser click does — `focus-trap.spec.ts`'s existing "restores focus" test already works around this by calling `.focus()` explicitly before opening the trap; the new month-view focus-return test mirrors that same workaround rather than assuming a synthetic click would move focus.

### Completion Notes List

- All 5 tasks complete; all 5 ACs implemented and covered by tests.
- Entirely frontend, as scoped in Dev Notes — zero backend changes, zero new API surface. Reused Story 3.1's `GET /api/appointments/colleagues` data end to end.
- `ng build` clean (prod). Full frontend spec suite 118/118 (21 files) — 13 new tests (`colleague-day-status.spec.ts`: 5 new; `month-view.spec.ts`: 7 new; `home.spec.ts`: 1 new).
- Design decision (documented in the story before implementation, held up during implementation): only the aggregate-marker element itself is the click/keyboard-activation target for opening the popover, not the whole day cell — avoids a click-bubbling conflict with the cell's independently-activatable own-appointment titles (Story 1.2/1.4).
- Reused the existing `calendar.detailPrivacyNote` i18n key (Story 3.1) verbatim in the popover footer rather than adding a duplicate key with the same German copy, per the story's own instruction.
- The popover's "outside click closes it" behavior uses a transparent (non-dimming) full-screen backdrop, distinct from the dimmed backdrops `appointment-create`/`appointment-detail` use — matches the UX mockup, which shows the rest of the month grid staying fully visible around the popover.
- **Code review (run combined with Story 3.1, high effort):** no correctness or privacy-leak findings. One cleanup finding in this story's `month-view.ts`: `cells()` called `computeColleagueDayStatuses` once per visible day (~35–42 times per recompute), and that function re-grouped each selected colleague's full appointment array by day from scratch on every call — pure wasted work since the grouping is identical across all 42 calls. Fixed by extracting `buildColleagueDayIndex`/`computeColleagueDayStatusesFromIndex` in `colleague-day-status.ts` and memoizing the index build in a `colleagueDayIndex` computed signal, reused by both `cells()` and `popoverStatuses()`. The original `computeColleagueDayStatuses` is kept as a thin per-call convenience wrapper for low-frequency/test use. Re-verified: `ng build` clean, 118/118 frontend tests still pass (including the unchanged `colleague-day-status.spec.ts` assertions against the public function signature).

### File List

**New files**
- `frontend/src/app/pages/home/calendar/colleague-day-status.ts`
- `frontend/src/app/pages/home/calendar/colleague-day-status.spec.ts`

**Modified files**
- `frontend/src/app/pages/home/calendar/month-view/month-view.ts` (`roster`/`selectedPersonIds`/`colleagueAppointments` inputs, `hasDndAggregate`/`dndCount` per cell, popover open/close state)
- `frontend/src/app/pages/home/calendar/month-view/month-view.html` (aggregate marker, popover, backdrop)
- `frontend/src/app/pages/home/calendar/month-view/month-view.css` (`.month-view__aggregate-marker`, `.month-view__popover*`)
- `frontend/src/app/pages/home/calendar/month-view/month-view.spec.ts` (7 new tests)
- `frontend/src/app/pages/home/home.html` (person-selector no longer hidden for Month view; `<app-month-view>` gets the three new bindings)
- `frontend/src/app/pages/home/home.spec.ts` (1 new test)
- `frontend/public/i18n/de.json`, `en.json` (`calendar.monthAggregateMarker`)
- `frontend/src/app/testing/transloco-testing.ts` (same key duplicated for specs)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (3-2/epic-3 status)
