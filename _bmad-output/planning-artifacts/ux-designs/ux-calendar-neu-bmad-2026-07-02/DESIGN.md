---
name: calendar-neu-bmad
description: Self-hosted, dark-mode-native team calendar that aggregates Outlook/Google into one calm, unified view with a derived interruptibility signal.
status: final
updated: 2026-07-02
colors:
  bg: '#12151A'
  surface: '#1A1E24'
  surface-2: '#20242B'
  text: '#E7EAEE'
  muted: '#8B95A1'
  border: '#2B3038'
  border-interactive: '#647079'
  accent: '#4FD1C5'
  accent-on: '#0A1F1E'
  interruptible: '#59C2C0'
  interruptible-fill: 'rgba(89,194,192,0.12)'
  interruptible-text: '#A9E4E2'
  dnd: '#C2685F'
  dnd-fill: 'rgba(194,104,95,0.12)'
  dnd-text: '#E7B7B0'
  bg-light: '#F4F6F8'
  surface-light: '#FFFFFF'
  surface-2-light: '#FFFFFF'
  text-light: '#1B1F24'
  muted-light: '#667085'
  border-light: '#E2E5E9'
  border-interactive-light: '#7E8894'
  accent-light: '#2E9B99'
  accent-text-light: '#1D6664'
  accent-on-light: '#FFFFFF'
  interruptible-light: '#2E9B99'
  interruptible-fill-light: '#E3F3F2'
  interruptible-text-light: '#1D6664'
  dnd-light: '#A94F45'
  dnd-fill-light: '#F5E4E1'
  dnd-text-light: '#7A362E'
typography:
  heading:
    fontFamily: 'IBM Plex Sans'
    fontSize: 20px
    fontWeight: '700'
    lineHeight: '1.25'
    letterSpacing: -0.01em
  label:
    fontFamily: 'IBM Plex Sans'
    fontSize: 13.5px
    fontWeight: '700'
    lineHeight: '1.3'
  body:
    fontFamily: 'IBM Plex Sans'
    fontSize: 11.5px
    fontWeight: '400'
    lineHeight: '1.35'
  caption:
    fontFamily: 'IBM Plex Sans'
    fontSize: 11px
    fontWeight: '500'
    lineHeight: '1.4'
  label-caps:
    fontFamily: 'IBM Plex Sans'
    fontSize: 9.5px
    fontWeight: '700'
    lineHeight: '1.4'
    letterSpacing: 0.06em
rounded:
  sm: 8px
  md: 12px
  lg: 16px
  full: 9999px
spacing:
  '1': 4px
  '2': 8px
  '3': 12px
  '4': 16px
  '5': 20px
  '6': 24px
  '7': 28px
  '8': 36px
  time-gutter: 64px
components:
  status-badge:
    interruptible-bg: '{colors.interruptible-fill}'
    interruptible-border: '{colors.interruptible}'
    interruptible-text: '{colors.interruptible-text}'
    dnd-bg: '{colors.dnd-fill}'
    dnd-border: '{colors.dnd}'
    dnd-text: '{colors.dnd-text}'
    radius: '{rounded.md}'
    borderSide: 'left only, 3px'
  calendar-column:
    background: '{colors.surface}'
    border: '{colors.border}'
    radius: '{rounded.lg}'
    ownAppointmentBg: '{colors.surface-2}'
    ownAppointmentBorder: '{colors.accent}'
  month-day-cell:
    background: '{colors.surface}'
    hover: '{colors.surface-2}'
    markerColor: '{colors.dnd}'
    radius: '{rounded.md}'
  status-override-control:
    buttonBg: '{colors.bg}'
    buttonBorder: '{colors.border-interactive}'
    buttonText: '{colors.muted}'
    buttonRadius: '{rounded.full}'
    flyoutBg: '{colors.surface-2}'
    flyoutBorder: '{colors.border}'
    flyoutRadius: '{rounded.md}'
    flyoutWidth: 'intrinsic, min-width ~180px, no fixed px lock'
    activeItemBg: 'rgba(79,209,197,0.1)'
    activeItemText: '{colors.accent}'
  sync-indicator:
    text: '{colors.muted}'
    icon: '{colors.muted}'
    errorText: '{colors.dnd-text}'
    errorIcon: '{colors.dnd}'
  appointment-detail-popover:
    background: '{colors.surface-2}'
    border: '{colors.border}'
    radius: '{rounded.md}'
    labelText: '{colors.muted}'
    valueText: '{colors.text}'
  appointment-create-entry:
    ghostBg: 'transparent'
    ghostBorder: '{colors.border-interactive}'
    ghostText: '{colors.muted}'
    radius: '{rounded.full}'
  view-switcher:
    background: '{colors.surface-2}'
    radius: '{rounded.full}'
    inactiveText: '{colors.muted}'
    activeBg: '{colors.accent}'
    activeText: '{colors.accent-on}'
  person-selector:
    chipBg: '{colors.surface-2}'
    chipBorder: '{colors.border}'
    chipText: '{colors.text}'
    chipRemoveText: '{colors.muted}'
    chipRadius: '{rounded.full}'
    addChipBg: 'transparent'
    addChipBorder: '{colors.border-interactive}'
    addChipText: '{colors.muted}'
    addChipActiveBg: '{colors.accent}'
    addChipActiveText: '{colors.accent-on}'
    dropdownBg: '{colors.surface-2}'
    dropdownBorder: '{colors.border}'
    dropdownRadius: '{rounded.md}'
    dropdownItemHoverBg: '{colors.surface}'
    dropdownItemRadius: '{rounded.sm}'
  language-switcher:
    background: 'transparent'
    text: '{colors.muted}'
    activeText: '{colors.text}'
    radius: '{rounded.full}'
---

> `mockups/` contains the promoted visual sources this spine distills: the chosen direction (`mockups/direction-slate-dark.html`) and key-screen mocks (`mockups/key-month-view.html`, `mockups/key-settings-connections.html`, `mockups/key-admin-sync-overview.html`, `mockups/key-appointment-create.html`, `mockups/key-login.html`, `mockups/key-person-selector.html`). `wireframes/flow-month-multiperson-2026-07-02.excalidraw` documents the month-view mechanism exploration. Non-chosen direction explorations remain in `.working/` as an audit trail. All of these illustrate; wherever a mock and this document's text disagree, this document's text wins.

## Brand & Style

This is an internal tool built to consumer-grade polish, not a bare-bones prototype — that stakes call is explicit from discovery, even though usage is confined to one 5-10 person team. The register is a **modern, high-usability web application**: no marketing flourish, no gamification, no urgency-manufacturing. The brand posture is **ruhig/zurückhaltend** — calm and reserved — because the product's entire premise is to help people *avoid* interrupting each other; a UI that itself shouts for attention would undercut its own purpose.

Two structural commitments follow directly from discovery decisions:

- **Dark-mode-native, with full light-mode parity.** The chosen visual direction ("Slate Dark-first") treats dark as the natural resting state of the product — "ein ständig sichtbares Team-Status-Panel, das man beiläufig im Augenwinkel mitliest" (an always-visible team status panel read peripherally, in passing) — not dark mode bolted on as an afterthought. Because the direction file itself designates dark as the "PRIMÄR / nativer Zustand" and demonstrates light only as a small adaptation swatch, this DESIGN.md makes **dark the default (unsuffixed) token set** and light the `-light`-suffixed override — the conventional pattern for dark-native products. Light mode is nonetheless a first-class, fully specified must-have (explicit user decision), not a stripped-down fallback.
- **Airy, calm density over information density.** For the multi-person view specifically — and by extension the whole product — the explicit design principle is generous whitespace and a relaxed feel, even at the cost of scrolling, rather than cramming the maximum number of columns or appointments into view. A "don't interrupt people" tool should not itself feel like a cockpit.

## Colors

The palette is deliberately restrained: two neutral tonal scales (dark-native, light-derived) plus exactly three chromatic signals, each with a single, non-overlapping job. Visual reference: `mockups/direction-slate-dark.html` (the chosen direction; illustrates, does not override, the values below).

- **`{colors.bg}` (`#12151A`) / `{colors.bg-light}` (`#F4F6F8`)** — the app canvas. Cool slate, not pure black/white, to keep the always-on status panel from feeling either clinical or harsh.
- **`{colors.surface}` (`#1A1E24`) and `{colors.surface-2}` (`#20242B`)** — two dark elevation tiers: `surface` for cards/columns/panels/popovers, `surface-2` for headers, hover states, and the topbar. In light mode both tiers collapse to `{colors.surface-light}` (`#FFFFFF`) — `[ASSUMPTION]`: the direction file only sampled two light tones (`bg`, `surface`), not a distinct third tier, so light mode is specified as flatter than dark mode by design intent, not by omission.
- **`{colors.text}` (`#E7EAEE`) / `{colors.text-light}` (`#1B1F24`)** and **`{colors.muted}` (`#8B95A1`) / `{colors.muted-light}` (`#667085`)** — primary and secondary text. `muted` carries captions, timestamps, and time-gutter labels.
- **`{colors.border}` (`#2B3038`) / `{colors.border-light}` (`#E2E5E9`)** — hairlines between columns, cards, and rows. Purely decorative dividers only — see `{colors.border-interactive}` below for anything that also marks a control's tappable boundary.
- **`{colors.border-interactive}` (`#647079`) / `{colors.border-interactive-light}` (`#7E8894`)** — the outline for interactive controls whose boundary is defined only by a stroke with no fill behind it (the status-override button, the appointment-create ghost pills, the person-selector "+" chip): `{colors.border}` alone computes ≈1.38:1 against `{colors.bg}`, well under the 3:1 AA 1.4.11 threshold for non-text UI component boundaries; `{colors.border-interactive}` clears it (≈3.6:1 on `{colors.bg}`, ≈3.3:1 on `{colors.surface}`; light mode ≈3.3:1 on `{colors.bg-light}`, ≈3.6:1 on white). Use `{colors.border}` for layout dividers (columns, cards, rows) and `{colors.border-interactive}` for any stroke-only interactive control boundary.
- **`{colors.interruptible}` (`#59C2C0`, teal) — "Unterbrechbar."** Used exclusively as a soft glow-bar (`{colors.interruptible-fill}`, 12%-opacity teal wash) with a 3px left border and a small dot/glyph — never a full-opacity fill. This is the calm, "go ahead" signal.
- **`{colors.dnd}` (`#C2685F`, rose) — "Bitte nicht stören."** Same glow-bar treatment as teal, in rose. Deliberately muted (a dusty rose, not alarm-red) so that a colleague's do-not-disturb state reads as *information*, not as a warning klaxon — consistent with the calm/reserved brand posture. Raw `{colors.dnd}` is for fills, 3px borders, and icons (graphical-object contexts, 3:1 threshold) — never for caption/body-weight text, where it computes only ≈4.33:1 on `{colors.surface}` and fails the 4.5:1 AA text threshold; text contexts use `{colors.dnd-text}` instead (see `components.sync-indicator` below and Do's/Don'ts).
- **`{colors.accent}` (`#4FD1C5`, cyan)** — the one interactive/navigational accent: active view-switcher pill, link text (dark mode only — see light-mode exception below), focus rings, the current-time indicator. Never used for status. Keeping status colors (teal/rose) and the interactive accent (cyan) visually distinct prevents a colleague's status from ever being mistaken for a button.
- **Light-mode status colors are deepened, not just lightened**: `{colors.interruptible-light}` (`#2E9B99`) and `{colors.dnd-light}` (`#A94F45`) are intentionally darker/more saturated than a naive light-derivation would produce, specifically for AA contrast on a light background (per the direction file's own annotation: "vertieft für AA-Kontrast auf Hell"). `{colors.accent-light}` reuses the deepened teal value — the direction's light-mode swatch uses the same `#2E9B99` chip in the accent position, so the interactive accent and the "interruptible" status share a value in light mode; treat this as a deliberate economy of the palette, not a coincidence to "fix." That said, `{colors.accent-light}` on white computes only ≈3.35:1 — it clears the 3:1 bar for rings/icons/fills but fails 4.5:1 for body-sized link text. Light-mode link *text* therefore uses the dedicated `{colors.accent-text-light}` (`#1D6664`, ≈6.7:1 on white) — a further-deepened value in the same hue family, extending the same "deepen for AA" move already applied to the status colors, rather than reusing `{colors.accent-light}` for text. Dark mode needs no equivalent: `{colors.accent}` on `{colors.bg}`/`{colors.surface}` already clears ≈9:1.
- **`{colors.accent-on}` (`#0A1F1E`) / `{colors.accent-on-light}` (`#FFFFFF`)** — text/label color for content sitting directly on a solid `{colors.accent}` fill (as opposed to the usual glow/tint fills used elsewhere). Used by the view-switcher's active pill and the person-selector's "+" chip in its active/open state (`components.view-switcher`, `components.person-selector`) — the only two places in the system where accent is applied as a full-strength fill rather than a tint.

**Base text/background contrast (AA floor, computed against the hex values above):**

| Pair | Ratio | AA 4.5:1 (body) |
|---|---|---|
| `{colors.text}` on `{colors.bg}` | 15.16:1 | pass |
| `{colors.text}` on `{colors.surface}` | 13.86:1 | pass |
| `{colors.muted}` on `{colors.bg}` | 6.02:1 | pass |
| `{colors.muted}` on `{colors.surface}` | 5.51:1 | pass |
| `{colors.muted}` on `{colors.surface-2}` | 5.12:1 | pass |
| `{colors.text-light}` on `{colors.bg-light}` | 15.29:1 | pass |
| `{colors.text-light}` on `{colors.surface-light}` | 16.56:1 | pass |
| `{colors.muted-light}` on `{colors.bg-light}` | 4.59:1 | pass (narrow) |
| `{colors.muted-light}` on `{colors.surface-light}` | 4.97:1 | pass |

All base text/background pairs — including `muted`-on-dark-surface, the common AA failure pattern — clear 4.5:1 as specified; no change to these token values was needed. `{colors.muted-light}` on `{colors.bg-light}` is the narrowest pass (4.59:1) and should not be darkened further without re-checking.

**Avoid:** any third chromatic color beyond teal/rose/cyan — the restraint of exactly three signals is the point. (See Do's and Don'ts for the status-fill, sync-error-color, and dnd-text usage rules.)

## Typography

Font: **IBM Plex Sans**, intended but currently unavailable in the exploration tooling — production must load the real family. Fallback stack: `-apple-system, "Segoe UI", Roboto, Arial, sans-serif`. The voice is "clear, modern, slightly technical, but calm" (direction file annotation) — a working, high-usability sans, not a display face.

Five roles cover the whole surface; there is no hero/display scale, consistent with the calm/no-marketing-flourish brand posture:

- **`{typography.heading}`** (20px/700, −0.01em) — the date heading and other page-level headings ("Donnerstag, 2. Juli 2026").
- **`{typography.label}`** (13.5px/700) — person names, the wordmark, popover titles: things that name an entity.
- **`{typography.body}`** (11.5px/400) — appointment and status-block text, the primary reading size inside the calendar grid.
- **`{typography.caption}`** (11px/500) — sync timestamps, time-gutter labels, secondary metadata.
- **`{typography.label-caps}`** (9.5px/700, tracked 0.06em, uppercase) — micro-labels only: the status-flyout section header, the "AKTIV" tag. Used sparingly — this is the one place the type system raises its voice, and only for a single word at a time.

## Layout & Spacing

`[ASSUMPTION]`: the direction file does not name an explicit spacing scale; the ramp below (`{spacing.1}`–`{spacing.8}`, 4px-based) is inferred from the gap/padding values actually used across the Slate Dark mockup and should be treated as a reasonable starting scale to formalize in implementation, not a value confirmed token-by-token with the user.

Per the airy-over-dense principle (see Brand & Style), concretely:

- Calendar columns in the Day/Week multi-person layout use a fixed time-gutter (`{spacing.time-gutter}`, 64px) plus flexible day columns, ending in a persistent person-selector "+" chip rather than auto-filling every available person into view. `{spacing.time-gutter}` is deliberately **not** part of the `{spacing.1}`–`{spacing.8}` rhythm scale: it is a time-axis ruler sized to fit the longest anticipated time label (e.g. "08:00"), not a spacing-between-elements value, so it should not scale or be substituted with a rhythm token even though it lives in the same `spacing` block for convenience. If a locale-driven 12-hour "8:00 AM"-style label is ever introduced, re-verify 64px still fits before reusing this token as-is.
- Selected teammates render strictly **side-by-side in columns**, never overlaid — this is a hard product rule (PRD FR-2 "Out of Scope": no overlaid rendering of multiple calendars in one column), and it is also what keeps each person's status glow-bars independently legible.
- Generous internal padding inside cards/popovers (`{spacing.5}`–`{spacing.6}`) versus tighter rhythm between closely related elements (`{spacing.1}`–`{spacing.2}`, e.g. a status dot and its label).

## Elevation & Depth

Elevation is used sparingly and only for genuinely floating layers — the resting UI (columns, cards, headers) is distinguished by tone (`{colors.surface}` vs `{colors.surface-2}`) rather than shadow. Two floating layers get a real shadow:

- **Popovers and flyouts** (status-override flyout, appointment-detail popover, month-cell status popover): a soft, dark-tinted shadow (`0 14–18px 30–40px, -12/-18px offset, rgba(6,8,11,0.6)`), just enough to read as "on top of," not dramatic.
- **The page-level app frame** gets the heaviest shadow in the system (`0 24px 60px rgba(6,8,11,0.55)` plus a tighter secondary shadow) — appropriate once, for the outermost container, never repeated at the component level.

No hover-lift on cards, no drama on interaction — consistent with the calm posture.

## Shapes

Two radius tiers plus a pill do all the work:

- **`{rounded.lg}` (16px)** — cards, calendar columns, panels, the outer app frame. The largest, calmest curve, reserved for containers.
- **`{rounded.md}` (12px)** — appointment blocks, status blocks/badges, popovers, month-day cells, avatars. The workhorse radius for anything that holds content.
- **`{rounded.sm}` (8px)** — small inline elements (flyout menu items).
- **`{rounded.full}` (pill)** — every interactive chip and button: the view-switcher (`components.view-switcher`), the status-override button, avatar chips and the "+" add-person chip (`components.person-selector`), the language switcher (`components.language-switcher`), "+ Neuer Termin" ghost buttons. Pills read as soft, tappable, and low-friction — appropriate for a tool that wants interaction to feel easy, not effortful.

## Components

- **Status badge / status block** (`components.status-badge`) — the visual encoding of "Unterbrechbar" vs. "Bitte nicht stören." A soft glow-fill background (`{colors.interruptible-fill}` or `{colors.dnd-fill}`), a 3px left border in the full-strength color (`{colors.interruptible}` / `{colors.dnd}`), `{rounded.md}` corners, and a small glyph + text label — never a solid fill, never color alone (see Accessibility Floor). **Per FR-11, there is no visual difference between an automatically derived status and a manually overridden one** — both render through this exact same component with no "auto" vs. "manual" indicator visible to other viewers. In a colleague's calendar column this is the *only* content shown for a time block (no title, no time) — the Privat-Default (FR-9) rendered as a component contract, not just a data rule.
- **Calendar column** (`components.calendar-column`) — the Day/Week grid column, `{colors.surface}` background, `{colors.border}` hairlines, `{rounded.lg}` corners. Two content modes live inside the same column shape: the **owner's own column** renders full appointment blocks (`ownAppointmentBg: {colors.surface-2}`, `ownAppointmentBorder: {colors.accent}`, title + time visible), while every **colleague's column** renders only status blocks (see above). This dual mode is the component-level expression of the Privat-Default.
- **Month day cell** (`components.month-day-cell`) — stays visually clean by default (`{colors.surface}` background, `{rounded.md}`); the *only* affordance shown inline is a subtle aggregate marker (`markerColor: {colors.dnd}`) when at least one selected teammate is in "Bitte nicht stören" during that day. Hover state uses `{colors.surface-2}`. Clicking the cell opens the **month-cell status popover** — a small popover (shares the `appointment-detail-popover` visual spec) listing each selected teammate's name plus their status badge for that day. This is the chosen mechanism (Option C from the wireframe exploration — see `wireframes/flow-month-multiperson-2026-07-02.excalidraw`; a rendered key-screen mock lives at `mockups/key-month-view.html`): no per-person dots or bars cluttering the grid, detail on demand only.
- **Status-override control** (`components.status-override-control`) — an icon-button ("Status ändern", `{colors.bg}` fill, `{colors.border-interactive}` outline, `{rounded.full}`) that opens a flyout menu (`{colors.surface-2}`, `{rounded.md}`, drop shadow). The flyout width is intrinsic to its content (min-width ~180px) rather than a fixed pixel lock, since it holds translated option labels that vary in length across languages. The flyout lists exactly three options — Unterbrechbar / Bitte nicht stören / Automatisch — with the active choice highlighted (`activeItemBg: rgba(79,209,197,0.1)`, `activeItemText: {colors.accent}`) and tagged "AKTIV" in `{typography.label-caps}`. Always present on the user's own column header, regardless of current derived state. The button's outline uses `{colors.border-interactive}`, not the decorative `{colors.border}`, because it is a stroke-only control with no fill differentiating its resting state (see Colors, non-text UI contrast).
- **Sync-timestamp indicator** (`components.sync-indicator`) — a small icon + caption-weight text ("Zuletzt synchronisiert vor 3 Min.") in `{colors.muted}`, shown per-person in column headers, per-provider in Settings → Calendar connections (`mockups/key-settings-connections.html`), and aggregated in Admin → Sync overview (`mockups/key-admin-sync-overview.html`). On repeated or silent sync failure (Sync-Transparenz NFR), the same slot switches to `{colors.dnd-text}` text (not raw `{colors.dnd}` — see Colors: raw `{colors.dnd}` fails the 4.5:1 AA text threshold at caption weight; `{colors.dnd-text}` clears it at ≈9.4:1 on `{colors.surface}`) and `{colors.dnd}` icon — reusing the "needs attention" rose rather than introducing a new error color, so failure reads as urgent without breaking the palette. This state transition must also be announced to assistive tech, not just shown visually (see EXPERIENCE.md Accessibility Floor, `aria-live`).
- **Appointment detail popover** (`components.appointment-detail-popover`) — two content modes on one shape (`{colors.surface-2}`, `{rounded.md}`, drop shadow): the **full variant** (own appointment, or a colleague's appointment where the viewer is a listed attendee) shows title, time, location, attendees; the **status-only variant** (any other colleague's appointment) shows only a status badge plus an explicit privacy note ("Details sind privat — nur der Status ist sichtbar.").
- **Appointment-create entry points** (`components.appointment-create-entry`) — the ghost-pill affordance style ("+ Neuer Termin"): transparent background, `{colors.border-interactive}` outline, `{colors.muted}` text, `{rounded.full}`. Two entry points share this visual spec but differ in behavior (see EXPERIENCE.md Interaction Primitives): double-click (or `Enter`/`Space` on a focused, empty time slot) opens the same create form pre-filled with that time; the standalone button opens it blank. Rendered mock (prefilled, blank, and validation-error states): `mockups/key-appointment-create.html`.
- **View-switcher** (`components.view-switcher`) — the Month/Week/Day pill group, always visible. `{colors.surface-2}` track, `{rounded.full}`, each option a pill-shaped segment. Inactive options render `{colors.muted}` text on transparent background; the active option is the one place besides the person-selector's "+" chip where accent is used as a full-strength fill rather than a tint — `activeBg: {colors.accent}`, `activeText: {colors.accent-on}` (dark) / `{colors.accent-on-light}` (light), giving those two previously-orphaned tokens their real use. Switching views is instant, client-side (FR-1) — no loading state on this control.
- **Person selector** (`components.person-selector`) — the mechanism for choosing which teammates appear in the multi-person view (memlog decision, resolving the previously-unspecified FR-2 entry point). A horizontal row of **avatar chips** (`chipBg: {colors.surface-2}`, `chipBorder: {colors.border}`, `{rounded.full}`, `chipText: {colors.text}`), one per currently-selected teammate, each showing an avatar/initial + name + a small "×" remove affordance in `{colors.muted}`. The row ends in a trailing **"+" chip** (`addChipBg: transparent`, `addChipBorder: {colors.border-interactive}`, `addChipText: {colors.muted}`) that, when open/active, fills solid (`addChipActiveBg: {colors.accent}`, `addChipActiveText: {colors.accent-on}`/`{colors.accent-on-light}`) and opens a **searchable dropdown** (`dropdownBg: {colors.surface-2}`, `dropdownBorder: {colors.border}`, `{rounded.md}`, drop shadow matching other floating layers) listing the team roster, filterable by typing; each result row uses `{rounded.sm}` and highlights on hover/focus with `dropdownItemHoverBg: {colors.surface}`. There is **no hard cap** on the number of selected chips (memlog decision, resolving the earlier "bis zu 3" contradiction) — the chip row scrolls horizontally rather than blocking further additions, consistent with the airy/calm density principle. Behavioral spec: EXPERIENCE.md Component Patterns and Interaction Primitives. Rendered mock (expanded dropdown state): `mockups/key-person-selector.html`.
- **Language switcher** (`components.language-switcher`) — a minimal pill control (`{rounded.full}`, transparent background, `{colors.muted}` text for the inactive language, `{colors.text}` for the active one) realizing the real-i18n Foundation requirement (German/English minimum). Deliberately unobtrusive — no flags, no dropdown ceremony — consistent with the calm/no-marketing-flourish posture. Behavioral spec: EXPERIENCE.md Component Patterns.

## Do's and Don'ts

| Do | Don't |
|---|---|
| Render status exclusively as glow-bar + icon + text label (`{colors.interruptible-fill}` / `{colors.dnd-fill}`) | Fill an appointment or status block solid with teal/rose |
| Keep automatic and manually-overridden status visually identical (FR-11) | Add an "auto" vs "manual" badge, icon, or label difference |
| Use `{colors.accent}` only for interactive/navigation chrome | Use the cyan accent to indicate status or availability |
| Show only "privat/beschäftigt" + status for any appointment the viewer doesn't own or attend | Leak title, attendees, or location into a colleague's column or popover |
| Let the multi-person view breathe — side-by-side columns, scroll if needed | Compress columns or overlay calendars to fit more people on screen |
| Use pill shapes (`{rounded.full}`) for every interactive control | Mix square and pill buttons in the same control group |
| Reuse `{colors.dnd}` (rose) for sync-failure/error states | Introduce a separate alarm-red error color |
| Specify light mode with deepened status colors for AA contrast | Simply lighten the dark palette 1:1 and assume contrast holds |
| Use `{colors.dnd-text}`/`{colors.dnd-text-light}` for any caption/body-weight error text | Use raw `{colors.dnd}`/`{colors.dnd-light}` as text color (fine for fills/icons/borders only) |
| Use `{colors.border-interactive}` for stroke-only interactive control boundaries (status-override button, ghost pills, person-selector "+" chip) | Use the decorative `{colors.border}` (≈1.38:1) for a control whose entire tappable edge is defined by that stroke |
| Use `{colors.accent-text-light}` for light-mode link/body text | Use `{colors.accent-light}` for body-sized text (reserve it for rings, icons, and fills, where 3:1 applies) |
| Let the person-selector's avatar-chip row scroll horizontally for any number of selected teammates | Cap the number of selectable teammates or truncate/hide chips past a fixed count |
