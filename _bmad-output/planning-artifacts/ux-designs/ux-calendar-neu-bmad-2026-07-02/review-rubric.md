# Spine Pair Review — calendar-neu-bmad

## Overall verdict

The pair is well-formed and mostly source-extractable: canonical section order is intact in both files, sources/UJ/FR references resolve verbatim, and the seven jointly-specified components (status badge, calendar column, month day cell, status-override control, sync indicator, appointment popover, create-entry) have real, symmetric visual+behavioral specs. However, three load-bearing gaps would stop a downstream consumer cold or send them down the wrong path: the primary teammate-picker interaction (FR-2's core mechanism) has zero visual or behavioral spec anywhere; a hard "max 3 teammates" limit is implied by microcopy in one section while another section explicitly argues against any hard cap; and the Accessibility Floor states contrast targets only for status colors, never for the base text/background pairs that appear on every screen. Visual grounding in `.working/` is real but invisible — the spines paraphrase "the direction file" and "the wireframe exploration" extensively without ever giving a path, which is acceptable pre-promotion but still a traceability gap worth a one-line fix now.

## 1. Flow coverage — adequate

Sources frontmatter lists brief.md, addendum.md, prd.md, prd-addendum.md. The PRD names exactly one journey, UJ-1 (Mara). EXPERIENCE.md's Key Flows section covers UJ-1 plus two illustrative flows (appointment creation, admin sync health) that are explicitly labeled as inferred/illustrative rather than sourced — good transparency, not silently invented.

- Flow 1 (Mara, UJ-1): named protagonist, 5 numbered steps, explicit `**Climax:**`, and an edge-case/failure path (status-override correction) — full coverage.
- Flow 2 (Jonas/Mara, appointment creation): named protagonists, numbered steps, explicit climax — but no failure path at all (e.g., save failure, wrong slot clicked). Appointment creation is exactly the kind of flow where a save-error or validation-error branch is expected.
- Flow 3 (Dennis, sync health): named protagonist, numbered steps, climax, and a closing "failure mode this flow guards against" paragraph — but that paragraph explains the flow's *raison d'être*, not an in-flow failure branch (e.g., admin surface fails to load, or shows stale data).

### Findings
- **medium** Flow 2 (appointment creation) has no failure path, unlike Flow 1 and the failure-path convention shown in both example specs (`experience-example-mobile.md` Flow 1, `experience-example-shadcn.md` Flow 1 both end with an explicit `Failure:` line). (EXPERIENCE.md Key Flows, Flow 2). *Fix:* add a one-line failure branch, e.g. save fails / double-click lands on the wrong slot.
- **low** No Key Flow walks the Month-view popover interaction (Option C) or the Settings→Calendar-connections error path end-to-end, even though both are non-trivial, decision-heavy surfaces covered elsewhere only in table form. (EXPERIENCE.md Information Architecture, State Patterns). *Fix:* optional — a short Flow 4 for either would remove ambiguity for story-dev, but tables already carry the behavioral rules so this is not blocking.

## 2. Token completeness — adequate

Frontmatter defines 28 color tokens (14 dark + 14 light-suffixed pairs), 5 typography roles, a 3-tier rounded scale, an 8-level spacing scale, and 7 component token blocks. Cross-checked every `{path.to.token}` reference in both files' prose against the frontmatter: all resolve. Two issues remain.

- `accent-on` (`#0A1F1E`) and `accent-on-light` (`#FFFFFF`) are defined in frontmatter but never referenced by `{colors.accent-on}` anywhere in DESIGN.md or EXPERIENCE.md, and no component uses them. Likely intended as text-on-filled-accent (e.g., a solid view-switcher active state) but that use case is never specified.
- The 64px time-gutter is called out by name in Layout & Spacing as a specific, load-bearing measurement but is hardcoded in prose rather than promoted to a named spacing token (the spec explicitly supports named tokens like `gutter`, `margin-mobile` for exactly this case).
- Accessibility Floor states explicit contrast targets for the interruptible/DND status-text pairs only. No contrast target (or stated ratio) is given anywhere for `{colors.text}` on `{colors.bg}`/`{colors.surface}`, or — more riskily — `{colors.muted}` on dark surfaces, which is used for captions, timestamps, and time-gutter labels across every screen. Given WCAG 2.1 AA is a formally adopted, explicit requirement, the highest-traffic text/background pairs are the ones most in need of a stated target.

### Findings
- **high** No contrast target stated for base text (`text`/`muted`) against `bg`/`surface`/`surface-2`, despite AA being an explicit, formally adopted requirement and status-color pairs getting an explicit callout. (EXPERIENCE.md Accessibility Floor; DESIGN.md Colors). *Fix:* add a line stating the target ratio (4.5:1 body, 3:1 large text) and note whether `{colors.muted}` on `{colors.surface}` has been verified to clear it — muted-gray-on-dark-slate is a common AA failure pattern.
- **medium** Time-gutter (64px) is a named, load-bearing layout decision but not tokenized, unlike the spec's own recommended pattern for named spacing values. (DESIGN.md Layout & Spacing). *Fix:* add `spacing.time-gutter: 64px` to frontmatter and reference it.
- **low** `accent-on` / `accent-on-light` are defined but orphaned — no prose reference, no component use. (DESIGN.md frontmatter colors). *Fix:* either wire them into a component (e.g., a solid-fill view-switcher active state) or drop them.

## 3. Component coverage — thin

Both files list the same 7 components with real, matching visual (DESIGN.md.Components) and behavioral (EXPERIENCE.md.Component Patterns) specs: status badge, calendar column, month day cell + popover, status-override control, sync-timestamp indicator, appointment detail popover, appointment-create entry points. Symmetry between the two files is good — no component appears in one table but not the other.

The gap is components that are *named repeatedly in prose* but never promoted to either table:

- **Person selector / teammate picker** — named twice in EXPERIENCE.md's IA table ("View-switcher + person selector," "Month view + person selector") as the mechanism for FR-2's core interaction (choosing which teammates appear), but has no visual spec (DESIGN.md) and no behavioral spec (EXPERIENCE.md) anywhere. This is the primary interaction surface for the product's second-most-central requirement, and a downstream builder has nothing to go on beyond the resulting side-by-side-columns *outcome*.
- **View-switcher** — named in DESIGN.md Colors ("active view-switcher pill") and Shapes ("the view-switcher"), and in EXPERIENCE.md IA and Accessibility Floor ("focus ring... view-switcher"), but never given a `components` frontmatter entry or a row in either Components/Component Patterns table, despite being present on every screen and directly implementing FR-1's "switch without reload."
- **Avatar chips**, **language switch** — each mentioned once in DESIGN.md Shapes as pill-shaped elements, with no visual or behavioral spec at all. Lower priority than the two above but still undocumented despite i18n being an explicit, Must-Have-adjacent user decision.

### Findings
- **high** Person selector (teammate picker for multi-person view) has no visual or behavioral spec despite being named twice in the IA table and being the entry point to FR-2. (EXPERIENCE.md Information Architecture, rows "Multi-person view — Day/Week" and "— Month"; absent from both Components tables). *Fix:* add a component entry (visual: DESIGN.md, e.g. search/list/dropdown treatment; behavioral: EXPERIENCE.md, e.g. how many can be selected, how removal works).
- **medium** View-switcher referenced in 4+ places across both files but has no dedicated component row in either file. (DESIGN.md Colors, Shapes; EXPERIENCE.md Information Architecture, Accessibility Floor). *Fix:* add a minimal component entry — likely low effort since it reuses the pill/accent language already defined.
- **low** Avatar chips and language switch are named once each (DESIGN.md Shapes) with no further spec. (DESIGN.md Shapes). *Fix:* defer to mockup promotion if genuinely secondary, but note the gap explicitly rather than leaving it implicit.

## 4. State coverage — adequate

Walked every IA surface against expected states (empty/cold-load, error, permission-denied, offline where relevant):

| Surface | States covered | Gap |
|---|---|---|
| Login | none | mechanism deferred to architecture; no loading/invalid-credential state — arguably acceptable given explicit deferral |
| Own Calendar | cold/empty ✓, sync failure ✓ | — |
| Multi-person Day/Week | no-teammates-selected ✓ | no "selection limit reached" state despite implied cap (see §7) |
| Multi-person Month | no-teammates-selected ✓ (shared row) | — |
| Appointment creation (prefilled/blank) | — | no validation-error state (e.g., empty title, invalid time) |
| Appointment detail — own | — | none needed at this level |
| Appointment detail — others' | status-only/no-participation ✓ | — |
| Settings → Calendar connections | connection error ✓ | — |
| Admin → Sync overview | sync failure (aggregated) ✓, permission-denied ✓ | — |

### Findings
- **medium** No state defined for appointment-creation form validation errors (missing title, invalid date/time). (EXPERIENCE.md State Patterns — table has no row for the creation surfaces). *Fix:* add a row, even a minimal one, since this is a Must-Have surface (FR-3).
- **low** Login has zero states specified (loading, invalid credentials). Deferral to architecture is reasonable given "mechanism left to architecture" in the IA table, but this should be stated as a deliberate omission rather than left silent. (EXPERIENCE.md Information Architecture, "Login" row). *Fix:* add a one-line note that login states are architecture-owned, mirroring how other deferred items are flagged elsewhere in the doc.

## 5. Visual reference coverage — adequate, with a traceability gap

`.working/` contains 4 direction explorations (`direction-nordic-minimal.html`, `direction-geometric-precision.html`, `direction-editorial-serif.html`, `direction-slate-dark.html`) and one wireframe (`flow-month-multiperson-2026-07-02.excalidraw`). No `mockups/`, `wireframes/`, or `imports/` folders exist yet — expected at this stage (memlog confirms promotion/mockup creation is planned for after this review gate), not a defect.

Neither DESIGN.md nor EXPERIENCE.md contains a single file path or link into `.working/`, despite both files leaning on it heavily in prose: "the direction file itself designates dark as...", "per the direction file's own annotation", "Option C from the wireframe exploration", "per the Slate Dark direction's own empty-column copy." Compare to both example EXPERIENCE.md files, which use an explicit `→ Composition reference: `mockups/....html`` line under Information Architecture. Here, "the direction file" and "the wireframe exploration" function as named references but resolve to nothing a reader can click through to — a downstream consumer without chat/memlog access cannot locate the artifact being described.

Separately, the three *rejected* visual directions (nordic-minimal, geometric-precision, editorial-serif) are never acknowledged anywhere in either spine — not in DESIGN.md's Brand & Style, not in EXPERIENCE.md's Inspiration & Anti-patterns (which only covers product-level rejects like Clockwise/gamification, not the visual-direction bake-off).

### Findings
- **medium** No inline path/link to `.working/direction-slate-dark.html` or `.working/flow-month-multiperson-2026-07-02.excalidraw` despite both being paraphrased extensively and treated as authoritative sources. (DESIGN.md Colors, Layout & Spacing, Components; EXPERIENCE.md Information Architecture). *Fix:* add a `→ Composition reference:` line (matching the example convention) pointing at the two `.working/` files, even ahead of formal mockup promotion.
- **low** The 3 rejected visual directions are unacknowledged in either spine, losing the "why Slate Dark and not X" trail for a downstream reader. (DESIGN.md Brand & Style / EXPERIENCE.md Inspiration & Anti-patterns — absent). *Fix:* optional one-line mention if traceability to the discovery bake-off matters later.

## 6. Bloat & overspecification — strong

No section reads as filler. DESIGN.md's editorial prose (Brand & Style, Colors) is consistently decision-tied — every paragraph justifies a specific token or rule rather than restating brand personality in the abstract. EXPERIENCE.md stays restrained: FRs are pointed to by ID rather than re-quoted at length, and Key Flow narrative color (e.g., "Kurzabstimmung Projekt Atlas") is illustrative detail of exactly the kind both example specs use (Drift's "she picks up her coffee" is the same move), not decorative padding. No persona/scope restatement, no pixel-chasing where tokens already cover the case, no prose used where a table would serve better.

### Findings
None material.

## 7. Inheritance discipline — thin, one real contradiction

Sources frontmatter resolves to all 4 real files. UJ-1's quote is verbatim from PRD §2.3. FR references (FR-1 through FR-11) are used correctly and consistently by ID across both files; FR-12–FR-15 are explicitly deferred rather than silently dropped. Component names match 1:1 between DESIGN.md and EXPERIENCE.md (mod the two missing components noted in §3, which are absent from *both* consistently rather than mismatched). Token references from EXPERIENCE.md resolve to DESIGN.md by name in every instance checked.

One direct contradiction was found: EXPERIENCE.md's State Patterns table gives example copy "Wähle bis zu 3 weitere Teammitglieder, um ihre Verfügbarkeit zu sehen" (choose up to 3 more teammates) for the no-teammates-selected state, sourced verbatim from the Slate Dark mockup's placeholder copy. This implies a hard cap of 3 additional teammates. But the Responsive & Platform section, two subsections later in the same file, explicitly argues the opposite: "`[ASSUMPTION]`: lean toward horizontal scroll with the user's own column pinned/first, rather than a hard cap on how many teammates can be selected... A firm cap, if one turns out to be needed, should be treated as an architecture-phase refinement on top of this default, not a replacement for it." A downstream builder reading top to bottom would first conclude there's a cap of 3, then be told there explicitly isn't one — with no reconciling note either place.

### Findings
- **high** Internal contradiction: State Patterns implies a hard cap of 3 additional teammates via mockup-sourced copy; Responsive & Platform explicitly argues against any hard cap. Neither section cross-references the other. (EXPERIENCE.md State Patterns, "No teammates selected" row vs. Responsive & Platform, "Small screen" row). *Fix:* pick one: either mark the "bis zu 3" copy as illustrative/placeholder-only (not a real constraint) with an explicit note, or promote "max 3" to an actual decision and update the Responsive section to stop arguing against it.

## 8. Shape fit — strong

DESIGN.md sections appear in canonical order exactly: Brand & Style → Colors → Typography → Layout & Spacing → Elevation & Depth → Shapes → Components → Do's and Don'ts. No omissions, no reordering.

EXPERIENCE.md has all required-default sections (Foundation, Information Architecture, Voice and Tone, Component Patterns, State Patterns, Interaction Primitives, Accessibility Floor, Key Flows) plus both required-when-applicable sections correctly triggered: Inspiration & Anti-patterns (sources/memlog show reference products — Clockwise, Reclaim.ai — and explicit rejects — gamification, collision warnings) and Responsive & Platform (multi-surface/responsive web with breakpoint-relevant behavior). Section order matches the convention shown in `experience-example-shadcn.md`. No invented sections without justification.

### Findings
None material.

## Mechanical notes

- Frontmatter completeness: DESIGN.md and EXPERIENCE.md frontmatter both parse cleanly; EXPERIENCE.md's `sources` array resolves to 4 real, existing files (brief.md, brief addendum.md, prd.md, prd addendum.md) via the `{planning_artifacts}` placeholder, which maps correctly to `_bmad-output/planning-artifacts/`.
- No Mermaid diagrams present in either file — n/a for syntax checking.
- Terminology: PRD glossary uses lowercase German terms ("unterbrechbar", "bitte-nicht-stören"); both spines and UI-copy tables render these as title-case UI labels ("Unterbrechbar", "Bitte nicht stören"). This reads as intentional (glossary term vs. actual UI string) rather than a drift, but flagging for awareness.
- IA surface names in EXPERIENCE.md are authored in English ("Multi-person view") where PRD/glossary uses German ("Mehrpersonen-Ansicht") — consistent with both example specs' convention of English meta-documentation around German/native UI copy; not a defect.
- All `{path.to.token}` references checked in both files resolve to a defined frontmatter token; no broken token references found.
- Component naming is consistent between DESIGN.md.Components and EXPERIENCE.md.Component Patterns for all 7 jointly-specified components (minor label variance only, e.g. "Month day cell" vs. "Month day cell + popover" — same referent).
