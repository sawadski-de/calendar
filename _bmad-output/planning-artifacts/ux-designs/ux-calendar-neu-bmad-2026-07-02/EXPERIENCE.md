---
name: calendar-neu-bmad
status: final
sources:
  - "{planning_artifacts}/briefs/brief-calendar-neu-bmad-2026-07-02/brief.md"
  - "{planning_artifacts}/briefs/brief-calendar-neu-bmad-2026-07-02/addendum.md"
  - "{planning_artifacts}/prds/prd-calendar-neu-bmad-2026-07-02/prd.md"
  - "{planning_artifacts}/prds/prd-calendar-neu-bmad-2026-07-02/addendum.md"
updated: 2026-07-02
---

# calendar-neu-bmad — Experience Spine

> Self-hosted, responsive web team calendar for a fixed 5-10 person team. Aggregates Outlook/Google (one-way import only) into a unified view with a derived interruptibility signal ("Unterbrechbar" vs. "Bitte nicht stören"). Paired with `DESIGN.md` (visual identity: Slate Dark direction). This spine captures the discovery/coaching decisions recorded in `.memlog.md` plus the PRD's functional requirements (FR-1…FR-15) and UJ-1.
>
> `mockups/` and `wireframes/` hold the promoted visual sources this spine distills; see `DESIGN.md`'s intro for the full inventory. Same precedence rule applies here: all of these illustrate, and wherever a mock and this document's text disagree, this document's text wins. Individual mocks are cited inline below at their relevant surface/flow.

## Foundation

Responsive web application only — no native app, must work on mobile via browser (brief, PRD §7 "Mobil/Responsiv"). Self-hosted, single fixed team (5-10 people), one operator (Dennis) who is simultaneously product owner, developer, and admin — a bus-factor-1 operating reality that shapes the admin surface below. `DESIGN.md` is the visual identity reference (Slate Dark: dark-mode-native with full light-mode parity); this document is the experience/behavior spine.

- **i18n:** real internationalization with a language switcher, German + English minimum (explicit user decision; all source documents are German-authored, so German strings are treated as the canonical/first-authored copy, with English as a full parallel language, not an afterthought).
- **Accessibility target:** WCAG 2.1 AA, formally adopted (explicit user decision).
- **Roles:** a real role system — Admin vs. Member — with actual permission checks, not just an unguarded extra page, because the team may grow beyond Dennis-only administration later (explicit user decision). The Admin → Sync overview surface is gated behind the Admin role.
- **Data model note carried from PRD:** the Privat-Default (FR-9) is a *display* rule, not a data-access boundary — the backend always reads full appointment data (title, attendees, duration, location) for every appointment, native or synced. Every experience pattern below that talks about "hiding" a title or attendee list is a display-layer behavior; server responses to unauthorized viewers must already omit those fields (see PRD FR-9 Consequences — client-side hiding alone does not satisfy the requirement).

## Information Architecture

| Surface | Reached from | Purpose |
|---|---|---|
| Login | App entry (unauthenticated) | Authenticate before any calendar data is reachable (Zugriffsschutz, Must-Have NFR). Mechanism left to architecture — **detailed states (loading, invalid-credential, lockout) are a deliberate deferral, not an oversight**: the auth mechanism itself was never decided in discovery, so its states cannot be meaningfully specified ahead of that architecture-phase decision. Rendered mock (tone/brand reference only, not a mechanism commitment): `mockups/key-login.html`. |
| Own Calendar — Month/Week/Day | Post-login landing (default) | View own native + synced appointments; switch views without page reload (FR-1). View-switcher: see Component Patterns. |
| Multi-person view — Day/Week | View-switcher + person selector, within the calendar surface | Selected teammates' calendars side-by-side in columns, never overlaid (FR-2). Selection persists across Day↔Week switch. Person selector (avatar chips + "+" add-dropdown): see Component Patterns. |
| Multi-person view — Month | Month view + person selector | UX-phase scope extension beyond PRD FR-2 (memlog decision, flagged back to product) — FR-2 only specified Day/Week. Day cells show a subtle aggregate marker only if someone selected is in "Bitte nicht stören" that day; click opens a popover listing each selected teammate's status (chosen mechanism: Option C from wireframe exploration — `wireframes/flow-month-multiperson-2026-07-02.excalidraw`; rendered mock: `mockups/key-month-view.html`). |
| Appointment creation — pre-filled | Double-click (or `Enter`/`Space` on a focused, empty time slot — see Interaction Primitives) an empty time slot in Day/Week | Opens the create form with that date/time pre-filled (memlog decision; realizes FR-3). Rendered mock (pre-filled/blank/validation-error states): `mockups/key-appointment-create.html`. |
| Appointment creation — blank | "+ Neuer Termin" button | Opens the same create form with no time pre-selected; user picks the time manually (memlog decision). Validation-error state (e.g. missing title): see State Patterns. Rendered mock: `mockups/key-appointment-create.html`. |
| Appointment detail — own | Click on an owned appointment | Full detail: title, time, attendees; location once FR-15 ships (FR-4). |
| Appointment detail — others' | Click on a colleague's status block/slot | Status-only: "privat/beschäftigt" + status badge, no title/attendees/location — **unless** the requester is a listed attendee on that appointment, in which case full detail is shown (FR-4, FR-9 exception). |
| Settings → Calendar connections | Settings menu (per-user) | Per-provider (Outlook/Google) row: connected/not-connected, "Verbinden" button, last-sync timestamp, error indicator. Optional and reached later — **not** a forced onboarding step; new users land directly in their own (empty) calendar (memlog decision). Rendered mock: `mockups/key-settings-connections.html`. |
| Admin → Sync overview | Admin-only nav item (role-gated) | All team members' sync health at a glance: person \| provider \| status \| last sync. A distinct surface from per-user Settings, serving the bus-factor-1 operator need (memlog decision). Rendered mock: `mockups/key-admin-sync-overview.html`. |

**Present in the PRD but not yet designed** (Should-Have, deferred past this UX pass, not silently dropped):
- **Slot-finder** (FR-12) — select multiple teammates, system proposes free slots based on raw free/busy only (explicitly ignores the derived interruptibility status).
- **Reminders** (FR-13) — browser push + in-app banner/toast (explicit user decision; email was not selected). Own appointments only, never for others'.
- **Recurring appointments** (FR-14) — edit/delete granularity (this instance / this-and-following / whole series) and interaction with Status-Override are unresolved per PRD, deferred to that feature's own design pass.
- **Location with map** (FR-15) — autocomplete + map in appointment detail; subject to the same Privat-Default as other detail fields.

## Voice and Tone

Microcopy. Brand posture and visual identity live in `DESIGN.md`. German is the primary authored language (English is a full parallel translation, not a derivative). Tone is ruhig/zurückhaltend (calm, reserved) — a working tool, not a consumer app trying to delight.

| Do | Don't |
|---|---|
| "Unterbrechbar" / "Bitte nicht stören" | "🟢 Free to chat!" / "🔴 DO NOT DISTURB!!" |
| "Zuletzt synchronisiert vor 3 Min." | "Synced just now! ✓" |
| "Details sind privat — nur der Status ist sichtbar." | "Sorry, you don't have permission to see this." |
| "Synchronisierung für Björns Outlook-Konto seit 2 Zyklen fehlgeschlagen." | "Sync error" (bare, unattributed, no actionable detail) |
| "+ Neuer Termin" / "+ Person hinzufügen" | "Let's schedule something! 🚀" |
| Plain, complete sentences; state facts | Exclamation marks, gamification, streak/engagement language |

## Component Patterns

Behavioral specs. Visual specs for the same components live in `DESIGN.md.components`.

| Component | Use | Behavioral rules |
|---|---|---|
| Status badge / status block | Any appointment slot in any calendar column; appointment-detail popover | Encodes "Unterbrechbar" vs. "Bitte nicht stören" only. **No visual or textual difference between an automatically derived status and a manually overridden one (FR-11)** — a viewer can never tell which one they're looking at, by design. In a colleague's column it is the *entire* content of the slot — no title, no time range, no attendee count leaks through. |
| Calendar column (day/week) | Own calendar; multi-person view | Own column always renders full appointment content (click → full detail). Every other column renders status-blocks only (click → status-only popover, or full detail if the viewer is a listed attendee — FR-9 exception). Columns never overlay; they sit side-by-side and the set persists across Day↔Week switching (FR-2). |
| Month day cell + popover | Month view, own and multi-person | Default state shows no per-person detail at all — only a subtle aggregate marker if ≥1 selected teammate is "Bitte nicht stören" sometime that day. Click opens a popover listing every selected teammate's name + status badge for that day (Option C). No popover content is shown on hover — click/tap only, so the behavior is identical on touch and pointer devices. |
| Status-override control (button + flyout) | Own column header, any view | Icon-button labeled "Status ändern" is present at all times regardless of the current derived status — override is never gated behind a particular state. Flyout offers exactly three choices: Unterbrechbar / Bitte nicht stören / Automatisch, with the active choice marked. Selecting "Unterbrechbar" or "Bitte nicht stören" sets a manual override that **persists until the user changes it again** — it does not expire when the appointment that was active at override-time ends, and it is not silently overwritten by the next sync/heuristic cycle (FR-11). Selecting "Automatisch" returns control to the heuristic (FR-10). |
| Sync-timestamp indicator | Column headers (per person); Settings → Calendar connections; Admin → Sync overview | Always visible, never requires a click to reveal ("Zuletzt synchronisiert vor N Min."). On a sync cycle that repeatedly fails for an account, the indicator switches to an explicit failure state (see State Patterns) using `{colors.dnd-text}` (not raw `{colors.dnd}`, which fails AA at text weight — see `DESIGN.md` Colors) — this must be visible somewhere the affected user *and* the admin will see it, and the transition itself is announced via `aria-live="polite"` (see Accessibility Floor) since it can happen asynchronously with no page reload; a silent failure is a direct NFR violation (Sync-Transparenz, PRD §7). |
| Appointment-create entry points | Own calendar, any view | Double-click on an empty time slot opens the create form with date+time pre-filled from the clicked slot; `Enter`/`Space` on a focused, empty time slot does the same (keyboard equivalent — see Interaction Primitives, Accessibility Floor). The standalone "+ Neuer Termin" button opens the identical form with no time pre-selected — the user sets date/time manually inside the form. Both paths produce the same native appointment object and receive an automatic status via the heuristic (FR-10) on save; a missing title or invalid date/time blocks save with an inline validation error (see State Patterns). |
| Appointment detail popover | Any appointment, own or colleague's | Two content modes on one component: full detail (own appointments, or a colleague's appointment where the requester is a listed attendee) vs. status-only + explicit privacy note (every other colleague appointment). Which mode renders is decided server-side per FR-9's testable consequence — the client never receives fields it isn't allowed to show. |
| Person selector (avatar chips + "+" dropdown) | Multi-person view header, Day/Week/Month | One avatar chip per currently-selected teammate, plus a trailing "+" chip (memlog decision — resolves the previously-unspecified FR-2 entry point). Clicking a chip's "×" removes that teammate immediately, no confirmation. Clicking (or `Enter`/`Space` on) "+" opens a searchable dropdown: typing filters the team roster by name in real time; arrow keys move focus through the filtered results; `Enter` adds the focused person as a new chip and keeps the dropdown open for adding more; `Esc` or an outside click closes it and returns focus to the "+" chip. **No hard cap on selections** (memlog decision) — the chip row scrolls horizontally rather than blocking further additions once it overflows, consistent with the airy/calm density principle (see Responsive & Platform). Rendered mock (expanded dropdown state): `mockups/key-person-selector.html`. |
| View-switcher | Every calendar surface (Month/Week/Day) | A three-way pill toggle; selecting an option switches the view client-side with no page reload (FR-1) and no loading state. The active option is visually distinct (solid accent fill, not just a border) from the two inactive ones; current selection is announced to assistive tech via standard tab/radio-group semantics. |
| Language switcher | Global chrome, always visible | Toggles the UI language between German (canonical/first-authored) and English (full parallel translation), per the real-i18n Foundation requirement. Switching is instant and client-side — no reload, no loss of current view/selection state (e.g. the person-selector's chip row and current Month/Week/Day view persist across a language switch). |

## State Patterns

| State | Surface | Treatment |
|---|---|---|
| New user, no calendar connected | Own Calendar (cold) | Calendar renders empty — not an error, not a forced setup wizard. Message invites, doesn't block: "Noch keine Termine — verbinde deinen Kalender in den Einstellungen, wann immer du bereit bist." Link to Settings → Calendar connections. Consistent with the decision that connecting a provider is optional and never a forced onboarding gate. |
| Calendar-connection error (consent denied / blocked by tenant admin) | Settings → Calendar connections; Admin → Sync overview | A real, flagged risk (PRD §8, Offene Fragen #2: Microsoft tenant admins can block Self-Consent org-wide). Row shows an explicit error indicator with provider-specific, actionable text where possible (e.g. "Verbindung von deinem Unternehmen blockiert — bitte den IT-Admin kontaktieren"), not a generic "connection failed." Never silently retried without surfacing that it's still failing. |
| Sync failure (repeated/silent) | Column headers; Settings; Admin → Sync overview | Must be visible, not silent (Sync-Transparenz NFR — this is a Must-Have, not a nice-to-have, precisely because Dennis is bus-factor-1 and won't notice a quiet failure otherwise). Indicator switches from the neutral "Zuletzt synchronisiert vor N Min." caption to an explicit failed state using `{colors.dnd-text}` for the text (reusing the rose/DND hue family, not a separate alarm color, per `DESIGN.md`; raw `{colors.dnd}` is reserved for the icon — see Accessibility Floor for why text uses the lightened variant) and announced via `aria-live="polite"` since the transition can happen with no page reload. Surfaced per-account to the affected user *and* aggregated for all accounts on the Admin surface. |
| No teammates selected | Multi-person view (Day/Week/Month) | Own column always renders regardless. Remaining space shows the person-selector's "+" chip with brief copy, not a blank void — e.g. "Wähle Teammitglieder, um ihre Verfügbarkeit zu sehen." No hard cap on selections (memlog decision, consistent with Responsive & Platform below) — the chip row and columns scroll horizontally instead. |
| Permission denied — Admin surface | Admin → Sync overview, for non-Admin members | Nav item is simply absent for non-Admins, not shown-then-blocked. No "you don't have access" dead end — consistent with a real role gate rather than a decorative one. |
| Fremder Termin ohne Teilnahme (status-only) | Appointment detail popover, colleague's appointment | Not an error state — the expected, designed-for default. Status badge + one-line privacy note ("Details sind privat — nur der Status ist sichtbar."), never a blank/greyed-out card that reads as broken. |
| Appointment creation — validation error | Appointment creation (pre-filled or blank) | Missing title or an invalid/incomplete date-time blocks save with an inline, field-level error message (not a toast, not a silent no-op) — e.g. "Bitte gib einen Titel ein." under the title field. The form stays open with entered values preserved so the user doesn't re-enter anything; focus moves to the first invalid field on a failed save attempt. |

## Interaction Primitives

- **Click to open detail.** Any appointment (own or colleague's) opens its detail popover on click — full detail or status-only, decided server-side.
- **Double-click to create, pre-filled.** Double-clicking an empty time slot opens the create form with that slot's time already filled in.
- **Keyboard equivalent for pre-filled create.** Time slots are `Tab`-reachable (or reachable via arrow-key grid navigation within the calendar); pressing `Enter` or `Space` on a focused, empty time slot opens the same pre-filled create form as a double-click. Double-click is a pointer convenience, not the only path to this entry point.
- **Button to create, blank.** The "+ Neuer Termin" button opens the identical form with no time chosen — deliberately a *different* entry point from double-click, not a shortcut to it, so users can create either "at a moment I'm looking at" or "at a time I'll specify."
- **Click to expand month popover.** Month-view day cells never reveal per-person status on hover; a click/tap is required, keeping touch and pointer behavior identical (no hover-only affordance excluded from touch, see Accessibility Floor).
- **Open the person-selector dropdown.** Clicking (or pressing `Enter`/`Space` on) the person-selector's "+" chip opens a searchable dropdown and moves focus into its search input; typing filters the team roster by name; arrow keys move focus through the filtered results; `Enter` adds the focused person as a new avatar chip; `Esc` closes the dropdown without adding anyone and returns focus to the "+" chip.
- **View-switch without reload.** Month/Week/Day (and multi-person on/off) all switch client-side; no full-page reload (FR-1 Consequences).
- **Manual override always available.** The status-override control is never disabled or hidden based on current state — a user can always declare their own status, regardless of what the heuristic currently says (FR-11).
- **Escape/outside-click closes floating layers.** Status-override flyout and appointment/month popovers close on `Esc` or a click outside — no separate close button required, but one may be present for touch users.
- **Motion respects user preference.** Any hover/open/close transition (flyout, popover, dropdown) shortens to near-instant or is disabled outright under `prefers-reduced-motion: reduce` — the calm brand posture already avoids drama on interaction (see `DESIGN.md` Elevation & Depth), and this makes that a testable floor rather than just a stylistic default.

## Accessibility Floor

WCAG 2.1 AA (explicit target). Visual contrast values live in `DESIGN.md`; this section is the behavioral/testable floor.

- **Status must never rely on color alone.** The interruptible/DND distinction is the single most consequence-bearing signal in the product (acting on it wrongly means interrupting someone who asked not to be), so every status badge/block pairs its color with an icon glyph *and* a text label ("Unterbrechbar" / "Bitte nicht stören") — never a bare colored dot or bar. This also protects color-blind users, for whom the teal/rose pair could otherwise be genuinely hard to distinguish. The two states also use visually distinct glyph shapes (not just distinct colors on an identical glyph) as defense-in-depth, in case a label is ever hidden/truncated. **Status text labels must never truncate or ellipsize**, even under horizontal scroll or a narrow column — a truncated label would silently degrade the signal back to color-alone.
- **Contrast targets for status colors specifically:** dark mode uses `{colors.interruptible-text}` (`#A9E4E2`) on `{colors.interruptible-fill}` and `{colors.dnd-text}` (`#E7B7B0`) on `{colors.dnd-fill}`; light mode uses the deepened `{colors.interruptible-light}` (`#2E9B99`) and `{colors.dnd-light}` (`#A94F45`) specifically because a naive lightening of the dark values would fail AA on a light background (per `DESIGN.md` Colors). Implementation must verify actual rendered contrast, not just reuse these token values on faith.
- **Contrast targets for base text/background pairs:** `{colors.text}`/`{colors.muted}` against `{colors.bg}`/`{colors.surface}`/`{colors.surface-2}` (and the `-light` equivalents against `{colors.bg-light}`/`{colors.surface-light}`) all computed and verified ≥4.5:1 for normal text — see the table in `DESIGN.md` Colors. The narrowest pass is `{colors.muted-light}` on `{colors.bg-light}` at 4.59:1; do not darken `muted-light` further without re-checking.
- **Sync-failure text color (critical fix):** the sync-indicator's failure-state text now uses `{colors.dnd-text}` (`#E7B7B0` on `#1A1E24` ≈ 9.4:1 dark mode; `{colors.dnd-text-light}` `#7A362E` on white ≈ 8.8:1 light mode), not raw `{colors.dnd}`, which computed only ≈4.33:1 against `{colors.surface}` and failed the 4.5:1 AA text threshold. Raw `{colors.dnd}`/`{colors.dnd-light}` remain correct for the failure-state icon and any border/fill use, where the 3:1 graphical-object threshold applies.
- **Non-text UI component contrast (AA 1.4.11):** interactive control boundaries defined only by a stroke (status-override button, appointment-create ghost pills, person-selector "+" chip) use `{colors.border-interactive}`/`{colors.border-interactive-light}` (≥3:1 against adjacent surfaces), not the decorative `{colors.border}` (≈1.38:1), which is reserved for non-interactive layout dividers.
- **Light-mode link/body text:** `{colors.accent-light}` is restricted to rings, icons, and fills (≈3.35:1 on white — clears 3:1 but not 4.5:1); body-sized link text in light mode uses the dedicated `{colors.accent-text-light}` (≈6.7:1 on white) instead.
- **Keyboard operability:** the status-override flyout, both popover types (appointment detail, month-cell status), and the person-selector dropdown must be fully operable by keyboard — reachable via `Tab`, openable via `Enter`/`Space`, internally navigable via arrow keys where a list of options is shown (the override flyout's three choices; the person-selector's filtered search results), and closable via `Esc`. Time slots are keyboard-reachable and `Enter`/`Space` on a focused, empty slot triggers the same pre-filled create action as a double-click (see Interaction Primitives) — there is no pointer-only path to any primary action.
- **Focus trap and restore on close:** opening the status-override flyout, either popover type, or the person-selector dropdown moves focus into that layer and traps `Tab` cycling within it while open; closing it (via `Esc`, outside-click, or selecting an option) returns focus to the control that triggered it. Focus must never land on, or get stuck behind, a now-hidden layer.
- **Focus states:** every interactive control (status-override button, flyout items, calendar slots, month cells, view-switcher, person-selector chips and dropdown items, "+ Neuer Termin", language switcher) has a visible focus ring using `{colors.accent}`, distinguishable from the resting and hover states, matching AA contrast against its background.
- **Reduced motion:** transitions and hover/open/close states respect `prefers-reduced-motion: reduce` (see Interaction Primitives).
- **Live regions for async state changes:** the sync-indicator's failure-state transition is announced via an `aria-live="polite"` region (or equivalent `role="status"`), not communicated by a visual color/text change alone — this is an asynchronous, no-reload state change per the Sync-Transparenz NFR, so a screen-reader user needs an explicit announcement to learn about it at all.
- **Component sizing for i18n:** components holding translated strings (the status-override flyout, status badges, the person-selector's chips and dropdown) size to content with a reasonable `min-width`/`max-width`, not a fixed pixel lock — German strings ("Bitte nicht stören") already run longer than their English equivalents, and other future languages could run longer still.
- **Screen reader labeling:** status blocks announce role + label (e.g. "Termin, Status: Bitte nicht stören" for a colleague's slot; full title+time for an owned slot) — the redaction that happens visually must also happen (correctly, in the *other* direction — i.e., not over-redact) in the accessibility tree.
- **Directionality:** current requirements are German/English only (both LTR); left-anchored structural choices (the status-badge's left border, the fixed-left time-gutter, icon-then-label ordering) are LTR-specific and not yet expressed as logical/direction-aware properties. Not an active violation today, but note this now so `border-left`-style properties are revisited before any RTL language is added.
- **Time format tolerance:** the 64px time-gutter (`DESIGN.md` `{spacing.time-gutter}`) is sized for a 24h "08:00"-style label; a 12-hour "8:00 AM" locale format runs longer. `[OPEN QUESTION]` — whether to widen the gutter for 12h locales or pin the product to 24h display regardless of locale was not decided in discovery; flagging rather than guessing.

## Responsive & Platform

Responsive web app, not a native app — must work on mobile via browser for all core functions (PRD §7). No breakpoint-specific behavior was specified for most surfaces; the following is a reasonable default split, not a confirmed spec:

| Breakpoint | Behavior |
|---|---|
| Desktop / tablet landscape | Full multi-person Day/Week columns side-by-side; Month grid with popover as specified. |
| Small screen (phone / narrow viewport) | The PRD left the general question open: "für die Mehrpersonen-Ansicht ist das konkrete Verhalten bei vielen ausgewählten Spalten auf kleinen Bildschirmen (horizontales Scrollen vs. Obergrenze der Auswahl) architekturseitig zu lösen" (PRD §7). This is now resolved by explicit user decision: **no hard cap on teammate selection, ever — horizontal scroll handles any number of selected columns/chips**, with the user's own column pinned/first. Consistent with the direction file's own mockup, which already shows a scroll-hint affordance past the last rendered column, and with the airy/calm density principle (don't force a cramped fit; let people scroll). `[OPEN QUESTION]` narrows to the remaining, genuinely unresolved detail — exact column width / scroll-snap behavior at very narrow viewports — not whether a cap exists.
| All breakpoints | View-switching (Month/Week/Day) and the create-appointment flow are core functions and must work identically on mobile browsers — not degraded to read-only. |

## Inspiration & Anti-patterns

Per the PRD addendum's comparison landscape — treated as market context for a prototype, not a market claim.

- **Lifted from Clockwise:** the core idea of *automatically deriving* a do-not-disturb-style signal from calendar data (duration/attendee-count heuristic) rather than requiring manual tagging for every appointment. **Diverges deliberately:** Clockwise uses this to optimize the *owner's own* schedule (Focus Time) and pushes a resulting status to Slack/Teams; this product instead classifies *existing* appointments — including colleagues' — and displays the result *to colleagues*, inside the calendar itself. Don't import Clockwise's self-scheduling/optimization framing; the derived status here is purely observational for the viewer, not a scheduling assistant.
- **Lifted from Reclaim.ai:** a real precedent for redacting appointment titles to a generic label ("Busy") when synced across calendar boundaries — validates the Privat-Default's basic shape. **Diverges deliberately:** Reclaim redacts at the sync/data layer; this product's Privat-Default is a *display* rule only — the backend always holds full appointment data and redaction happens per-request based on viewer identity (PRD FR-9). Don't assume "Busy"-style redaction implies restricted data storage — it explicitly does not here.
- **Rejected — gamification/engagement patterns generally** (streak counters, celebratory toasts, urgency-manufacturing copy): inconsistent with the ruhig/zurückhaltend brand posture and with a tool whose success metric is quiet daily habit, not engagement spikes.
- **Rejected — proactive collision warnings:** explicitly out of scope (PRD §5 Non-Goals) — the tool makes collisions *visible* via shared calendar access, but does not actively warn when creating a conflicting appointment.

## Key Flows

### Flow 1 — Mara checks a colleague's status before interrupting (UJ-1)

From PRD §2.3, UJ-1: "Mara prüft, ob sie einen Kollegen jetzt ansprechen kann."

1. Mara, at her desk, needs a quick answer from a colleague.
2. Instead of opening his calendar directly or messaging to ask "hast du gerade Zeit?", she opens the tool (already logged in).
3. She's in Day view with her colleague's column already selected alongside her own (multi-person view, FR-2).
4. She reads his current status block directly — "Unterbrechbar" or "Bitte nicht stören" — without opening the appointment or seeing its title.
5. **Climax:** If he's "Unterbrechbar," she messages him immediately — no detour through his calendar, no interruption to ask if it's okay to interrupt. If "Bitte nicht stören," she waits, with confidence that isn't a guess.

**Edge case (part of UJ-1 as specified):** the status shows "Bitte nicht stören" but the colleague is actually free right now — he opens his own status-override control and manually sets "Unterbrechbar" (FR-11). Mara's view updates on the next sync/refresh; she now sees the override, visually identical to an automatically derived status (no "manual" tag), and messages him.

### Flow 2 — Creating an appointment: pre-filled vs. blank (illustrative, consistent with the calm/German-language register)

1. Jonas is looking at his Week view and notices a free 30-minute gap on Thursday afternoon.
2. He double-clicks directly on that empty slot. The create form opens with Thursday's date and that time range already filled in — he only needs to add a title ("Kurzabstimmung Projekt Atlas") and, optionally, an attendee.
3. He saves. The appointment appears immediately in his own column, with a status automatically derived from its short duration (FR-10: short + few/no attendees → "Unterbrechbar").
4. Later the same day, Mara wants to schedule a call for *next* Monday morning — no slot visible in her current view. She clicks "+ Neuer Termin" instead of hunting for the right cell.
5. **Climax:** The same create form opens, but blank — she picks Monday's date and time herself inside the form, adds Jonas as an attendee, and saves. Two different starting gestures (double-click vs. button), one identical form and one identical resulting appointment object — neither path is a compromise version of the other.

**Failure branch:** Mara leaves the title blank and clicks save. The form does not close and nothing is created — an inline validation error appears under the title field ("Bitte gib einen Titel ein."), her entered date/time/attendee are preserved untouched, and focus moves to the title field so she can immediately correct it and retry (see State Patterns, "Appointment creation — validation error").

### Flow 3 — Dennis checks team-wide sync health (operator persona, not in PRD's UJ list but named as a distinct need)

This flow has no journey in the source PRD — it is inferred directly from the bus-factor-1 operating reality stated in the brief ("ein Betreiber (Bus-Faktor 1) für Wartung und OAuth-Token-Pflege") and the Sync-Transparenz Must-Have NFR, plus the memlog decision to give Dennis a dedicated admin overview.

1. Dennis, in his role as the tool's sole operator, opens Admin → Sync overview (visible to him because his account holds the Admin role) — a habit, not triggered by any in-app alert, since the product doesn't yet push notifications for this.
2. The surface lists every team member with their connected provider(s), current sync status, and last-successful-sync timestamp — person | provider | status | last sync.
3. One row stands out: a colleague's Outlook connection shows a failure state, not just an old timestamp — flagged distinctly per the Sync-Transparenz requirement rather than blending in as "just a bit stale."
4. **Climax:** Dennis immediately knows *which* account and *which* provider needs attention — most likely an expired or revoked OAuth token (PRD §7, §8) — without having to ask the colleague whether their calendar "looks right," and without having silently let a sync failure go unnoticed for days, which is the exact single-point-of-failure risk this surface exists to cover.

Failure mode this flow guards against: if this surface didn't exist, a silently failing sync would be indistinguishable from "that colleague just has a quiet week" — invisible until someone gets missed for a meeting.
