---
title: Input Reconciliation — brief.md vs. prd.md + addendum.md
created: 2026-07-02
---

# Reconciliation: Product Brief → PRD (+ Addendum)

**Input checked:** `briefs/brief-calendar-neu-bmad-2026-07-02/brief.md`
**Against:** `prds/prd-calendar-neu-bmad-2026-07-02/prd.md` + `.../addendum.md`

**Method:** Every substantive claim, decision, scope item (MoSCoW), success criterion, and named risk in the brief was traced to its counterpart in the PRD/addendum. Additions, elaborations, and explicit resolutions of open questions are expected and not flagged. Only silent drops, contradictions, or unexplained weakenings would count as gaps.

## Result: No gaps found

The PRD is unusually faithful and traceable back to the brief. Every item below was located successfully.

### Executive summary / Solution / Vision
- Unified view over Outlook + Google, "Unified Inbox" analogy, one-way import-only sync, new appointments created directly in tool, primary success = daily use instead of opening Outlook → all present in PRD §1 Vision (near-verbatim).
- Context-aware availability (interruptible / do-not-disturb) derived via heuristic (duration, time of day, participant count), same sync delay as rest of calendar (no real-time claim), applies equally to imported and self-created appointments, internal full-data read vs. "private/busy" external display → FR-8, FR-9, FR-10, and Glossary (§3) all reproduce this faithfully.

### Who this serves
- Fixed 5–10 person team, Outlook/Google mix, including Dennis → PRD §2 Zielgruppe, JTBD.

### Success Criteria (brief) → Success Metrics (PRD §9) / NFRs (§7)
- Daily use instead of Outlook → SM-1.
- Fewer double-bookings via visibility, no active warning → SM-3, confirmed again in Non-Goals §5.
- Perceptibly faster availability check → SM-2.
- "Sync within minutes is sufficient, no real-time claim" → repositioned from a brief success criterion into PRD §7 NFR "Sync-Aktualität" (explicitly cross-referenced to SM-2) and reiterated in Non-Goals and FR-5's feature NFR. This is a reclassification, not a drop — the commitment itself is preserved and appears in at least three places.

### Scope — Must Have (brief) → PRD §6.1 / FR-1…FR-11
All six Must-Have bullets are present: month/week/day views (FR-1), Mehrpersonen-Ansicht as an added elaboration (FR-2), create-own-appointment with click-through detail (FR-3, FR-4), one-way import sync every few minutes (FR-5–FR-7), private-default (FR-9), heuristic-based context-aware availability (FR-10), mobile/responsive (§7), self-hosted (§7).
- Note: the brief's Must-Have bullet scopes the private-default to "synchronisierte Termine," while PRD FR-9 applies it to *all* non-owned appointments (native + synced). This is a broadening, not a contradiction — and it matches the brief's own Solution-section prose ("Das funktioniert für importierte wie für selbst angelegte Termine gleichermaßen … anderen Teammitgliedern gegenüber zeigt es aber standardmäßig nur 'privat/beschäftigt'"), so the PRD is actually resolving an internal tension in the brief in the more permissive/consistent direction.

### Scope — Should Have (brief) → PRD §4.6 / FR-12…FR-15
Slot-finder (FR-12), reminders (FR-13), recurring appointments (FR-14), location with map (FR-15) — all present.
- The brief's "Hinweis, wenn ein wartender Kollege frei wird" is explicitly carved out as Out-of-Scope under FR-13, with the reasoning that the multi-minute sync interval makes it unreliable. This directly resolves a tension the brief itself flagged as a known risk ("Scope-Spannung Benachrichtigungen vs. Sync-Intervall") — an explicit decision, not a silent drop.

### Scope — Could Have (brief) → PRD §6.2 Out of Scope für MVP
Color-coding by source, per-appointment notes, quick search, location/video-link with travel-time estimate + tight-scheduling warning, absence/vacation category — all five items appear (travel-time item is reworded slightly shorter but same feature, no FR elaboration needed since it's a Could-Have).

### Scope — Won't Have (brief) → PRD §5 Non-Goals
Delegated/shared access, multi-team support, real-time sync, write-back to Outlook/Google — all four present, plus the PRD adds two further explicit Non-Goals (no active collision warning; no mechanism forcing tool-first appointment creation) that operationalize brief risks rather than contradicting anything.

### Bekannte Risiken (brief) → PRD treatment
All six named risks are traceable and each is either carried forward as an accepted risk or explicitly resolved with a decision:
1. Fragmentation is a behavioral risk, not solved by scope → PRD §5 Non-Goals (last bullet) + counter-metric SM-C1 (§9), explicitly not treated as solved.
2. Multi-person view undefined → resolved by FR-2 (side-by-side columns, explicitly not overlaid), a legitimate "brief left this open, PRD decides" case.
3. Operations without resilience concept (self-hosted, bus factor 1, no backup story) → PRD §7 "Betrieb" keeps bus-factor-1 as an accepted risk, and additionally adds a Must-Have backup/recovery NFR for native appointments (§6.1, §7) — strictly stronger than the brief, not weaker.
4. Google/Microsoft permission-model dependency → covered extensively in PRD §4.3 (feature NFRs), §8 Constraints, and Offene Fragen #1/#2, with full backing detail in addendum.md ("OAuth/API-Gotchas").
5. Notifications vs. sync-interval tension → resolved explicitly in FR-13 Out-of-Scope.
6. Privacy of external attendees' data → FR-9 Notes + §8 Privacy bullet, explicitly named as an accepted risk on the same basis as the brief (team-internal trust).

### Vision (brief) → PRD §1
Next steps after MVP validation (Should-Haves, multi-team expansion, external signal e.g. Slack badge) reproduced near-verbatim.

### Addendum-only material (not required in PRD body)
The brief's "What Makes This Different" differentiation argument is not repeated in the PRD body, but this is appropriate: the PRD addendum's "Vergleichslandschaft" section carries the equivalent (and more researched) competitive-differentiation content, and the addendum explicitly states its own material "gehört nicht in die PRD selbst."

## Out-of-scope observation (not a gap against the requested inputs)
The PRD (§0) references a *brief-side* addendum at `briefs/brief-calendar-neu-bmad-2026-07-02/addendum.md` (OAuth-scope constraint, discarded alternatives to the private-default) as one of its source inputs. That file is distinct from the PRD-side `addendum.md` reviewed here and was not part of the three files given for this reconciliation pass, so its content could not be checked. Worth a follow-up reconciliation pass if that brief-addendum exists and carries additional commitments.
