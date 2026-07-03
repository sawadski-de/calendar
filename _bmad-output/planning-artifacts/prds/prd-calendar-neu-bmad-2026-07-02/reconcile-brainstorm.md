# Reconciliation: brainstorm-intent.md → prd.md

Source input: `brainstorm-team-kalender-sync-2026-07-01/brainstorm-intent.md` (EN)
Downstream artifact checked: `prd-calendar-neu-bmad-2026-07-02/prd.md` (DE)
Note: the brief (`brief-calendar-neu-bmad-2026-07-02`) sits between these two documents; some divergence may have been introduced there rather than in the PRD step itself, but this check only compares brainstorm → PRD end-to-end.

## Element-by-element trace

| Brainstorm element | Found in PRD? | Where | Verdict |
|---|---|---|---|
| Unified Inbox analogy (primary blueprint) | Yes, explicit | Section 1 Vision, first sentence | Preserved |
| Unified Inbox step 1: merge sources | Yes | FR-5/FR-6/FR-7 | Preserved |
| Unified Inbox step 2: distinguish per source (color-coding) | Yes, demoted to Could-Have | Section 6.2 "Farbcodierung nach Quelle" | Preserved (matches brainstorm's own MoSCoW, which also lists it as Could Have) |
| Unified Inbox step 3: signal layer (context-aware availability) | Yes, called "Kernstück des Tools" | Section 4.5 | Preserved, arguably strengthened |
| Presence status analogy ("closer to chat-app presence than to a calendar grid") | Weakened | Vision mentions a Slack-badge only as a *possible future step after MVP validation*; the actual Must-Have interaction model is Month/Week/Day grids plus a side-by-side multi-person column view (FR-1, FR-2, UJ-1) | **Gap (see below)** |
| Context-aware availability = killer feature, live-derived, visible everywhere | Yes | FR-10, FR-11, described as core | Preserved |
| Privacy-by-default as trust feature (not just restriction) | Yes | Section 4.4 Beschreibung: "...ist die Grundlage des Vertrauens in das Tool"; JTBD in 2.1 | Preserved |
| Sync is one-directional, read-only, no write-back | Yes | FR-7, Non-Goals, Section 8 | Preserved |
| Originating trigger (couldn't tell if colleague was free) | Yes, folded into narrative | Vision, UJ-1 | Preserved (as narrative, not a named concept, but substance intact) |
| Constraint: integrate Outlook + Google | Yes | FR-5, FR-6 | Preserved |
| Constraint: mobile/responsive web app | Yes | Section 6.1, Section 7 | Preserved |
| Constraint: single fixed team, no multi-group | Yes | Section 2.2, Non-Goals | Preserved |
| Must Have: calendar views (M/W/D) | Yes | FR-1 | Preserved |
| Must Have: create own appointments + detail view | Yes | FR-3, FR-4 | Preserved |
| Must Have: one-way sync both providers | Yes | FR-5–FR-7 | Preserved |
| Must Have: private-by-default display | Yes | FR-8, FR-9 | Preserved |
| Must Have: context-aware availability | Yes | FR-10, FR-11 | Preserved |
| Must Have: mobile/responsive | Yes | Section 6.1/7 | Preserved |
| Should Have: shared free-slot finder | Yes | FR-12 | Preserved |
| Should Have: notifications (reminders **+ alert when awaited colleague becomes free**) | Partially reversed | FR-13 keeps reminders but explicitly puts "Hinweis, sobald ein wartender Kollege frei wird" **Out of Scope**, arguing the sync interval makes it too unreliable | **Gap (see below)** |
| Should Have: recurring appointments | Yes | FR-14 | Preserved |
| Should Have: location + autocomplete + map | Yes | FR-15 | Preserved |
| Could Have: color-coding, notes, quick search, travel-time warning, absence/vacation category | Yes, all listed | Section 6.2 | Preserved |
| Won't Have: delegated/shared calendar access | Yes | Non-Goals | Preserved |

## Gaps found

### 1. Presence-status analogy diluted into a deferred, external-only idea
The brainstorm's insight #2 is a distinct design principle from the Unified Inbox analogy: *"Rather than exposing a full calendar, show one always-visible status indicator — closer to chat-app presence than to a calendar grid."* This is a statement about the primary interaction model, not just a possible integration.

In the PRD, this idea survives only as a single aside in the Vision section — a Slack badge "possibly" built after MVP validation, as an outward extension. The actual MVP interaction model that the PRD specifies (FR-1, FR-2) is a full calendar grid with a side-by-side multi-person column view — structurally the opposite of "one always-visible status indicator instead of a calendar grid." Nothing in the Must-Have scope offers a lightweight, presence-only view inside the tool itself (e.g., a compact roster/status strip). Note: the brainstorm's own MoSCoW list already put full calendar views in Must Have, so this tension pre-dates the PRD; but the PRD's Vision and Feature sections don't acknowledge or resolve the tension the way section 4.4 does for the privacy analogy — the presence framing is not carried forward as a stated design principle anywhere it could still apply (e.g., as a rationale for how the multi-person column view should visually foreground status over calendar detail). Worth a PM decision: either explicitly retire this framing with reasoning, or ensure the eventual UX pass foregrounds the status signal the way the analogy intended.

### 2. "Alert when an awaited colleague becomes free" explicitly dropped from Should-Have notifications
The brainstorm's Should-Have scope for notifications names two things: "appointment reminders" and "alert when an awaited colleague becomes free." The second is arguably the more direct payoff of the whole "context-aware availability" concept — it's the notification version of the originating trigger (insight #6: not knowing when a colleague becomes available).

The PRD's FR-13 keeps only appointment reminders and explicitly places the "awaited colleague becomes free" alert Out of Scope, with the justification that the multi-minute sync interval makes such a signal unreliable. This is a reasoned, transparent decision (not silently dropped — it's visible in the FR itself), but it is a genuine scope reduction relative to the source brainstorm's stated Should-Have, and it removes a feature that ties directly back to the project's original triggering pain point. Flagging so it's a deliberate, visible PM call rather than an artifact of the FR template silently narrowing "notifications" to the easier-to-spec half.

### 3. (Minor / borderline) "Marketed as a trust feature" framing narrows to an internal design rationale
Brainstorm insight #4 explicitly frames privacy-by-default as something to be *marketed* — i.e., an outward-facing positioning/communication decision, not just an internal design choice, since "there are no real privacy concerns within the team." The PRD (section 4.4, section 8) preserves the *substance* well (privacy-by-default exists, is described as the foundation of trust in the tool, and section 8 explicitly notes the residual risk is accepted "based on team-internal trust"). What's absent is any trace of the marketing/positioning angle — e.g., no note that onboarding or in-product copy should actively communicate this as a trust-building feature rather than let users infer it. This is arguably out of scope for a PRD's FR structure (marketing copy isn't a functional requirement), so this is flagged as minor/borderline rather than a hard gap — noted for completeness, not necessarily actionable.

## Non-gaps checked and cleared
- Team size (brainstorm says "small/overviewable," PRD says "5-10") — not a contradiction, just added specificity (likely from the brief).
- Self-hosting requirement in PRD — an addition not present in the brainstorm's constraints, but additive, not a loss.
- Slot-finder ignoring derived status and using raw free/busy (FR-12) — an elaboration/design decision not addressed either way in the brainstorm, not a contradiction.
- All MoSCoW Must/Could/Won't-Have items traced 1:1 with no other omissions.
