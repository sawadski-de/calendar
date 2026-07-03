# PRD Quality Review — Team-Terminkalender mit Synchronisation

## Overall verdict

This PRD is well-calibrated to its stakes: an internal 5-10 person prototype, run by one person in three roles, that deliberately stays leaner than an enterprise spec while still being genuinely decision-ready. Its strongest trait is honesty — trade-offs, risks, and open questions are named rather than smoothed over (§4.3, §4.4, §8, §10). The main real gap is done-ness clarity for the deferred Should-Have FRs (FR-13–FR-15), which lack testable consequences and will need work before they're ever pulled into a sprint.

## Decision-readiness — strong

The PRD states trade-offs as trade-offs, not as balanced "considerations." §4.3's feature-specific NFRs explicitly choose polling over webhooks and name what's given up ("das deckt sich mit dem fehlenden Echtzeit-Anspruch... und umgeht die Unzuverlässigkeit von Provider-Webhooks... ohne zusätzliche Reconciliation-Logik"). §4.4's Notes block on FR-9 accepts a concrete privacy risk in plain language ("Namen/E-Mail-Adressen externer Teilnehmer... landen dadurch im Backend, ohne separate Schutzmaßnahme... bewusst akzeptiertes Risiko"). The Glossar entry for **Privat-Default** (§3) is unusually candid: "der Privat-Default ist eine Anzeigeregel, keine Datenzugriffsgrenze" — a PM could easily have let a display rule read as a privacy guarantee, and the PRD doesn't.

Open Questions (§10) are genuinely open — Google Workspace confirmation, MS tenant admin-consent, heuristic threshold calibration, backup process — none are rhetorical-with-an-answer-attached. The `[NOTE FOR PM]` at §6.2 sits at a real prioritization tension (which Should-Have to build first under time pressure), not a safe checkpoint.

### Findings
- **medium** External-participant PII risk settled as a Constraint rather than an Open Question (§4.4 Notes, §8 Privacy) — The risk is honestly named, but it concerns third-party data (customers) outside the team's own consent, which is a different risk class than internal team trust. *Fix:* Consider moving this from "accepted Constraint" to an Offene Frage, or explicitly note that no legal/GDPR review has occurred, so it isn't mistaken for a reviewed decision.

## Substance over theater — strong

No persona bloat — one named protagonist (Mara, UJ-1) plus Dennis's own JTBD in §2.1, both load-bearing (UJ-1 is cited by FR-1, FR-2, and FR-10). The Vision (§1) is specific to this product (unified-inbox analogy, the unterbrechbar/bitte-nicht-stören signal, the reason sync stays one-directional) rather than a swappable generic statement. The Addendum's "Vergleichslandschaft" self-limits its own differentiation claim: "ohne dass daraus ein Marktanspruch abgeleitet werden sollte (dieses Projekt ist ein interner Prototyp, kein Launch)" — a rare instance of a PRD refusing to oversell its own novelty. Cross-Cutting NFRs (§7) are product-specific, not boilerplate: "ein Betreiber (Bus-Faktor 1)... bewusst akzeptiertes Betriebsrisiko" is the opposite of "system must be reliable."

## Strategic coherence — strong

The thesis (merge calendars + derive an interruptibility signal + stay private-by-default) is stated once in the Vision and never contradicted downstream. Feature prioritization follows it: Must-Have (§6.1) is exactly views + creation + sync + privacy + status heuristic; the four Should-Haves (§6.2) are explicitly framed as extensions, not omissions. Success Metrics avoid the vanity-metric trap the rubric warns about — SM-1 measures daily use over Outlook specifically (not raw DAU), and SM-C1 is a genuine counter-metric that would falsify the tool's value even if SM-1 looked good ("Hohe Nutzung bei gleichzeitig hohem Anteil zeigt, dass das Tool nur zum Lesen dient").

## Done-ness clarity — adequate

All 11 Must-Have FRs (FR-1–FR-11) carry testable "Consequences" bullets with concrete, checkable conditions (e.g. FR-1: "Wechsel zwischen den drei Ansichten ist ohne Neuladen der Seite möglich"; FR-7: "erzeugt keine Schreiboperation gegen die Outlook- oder Google-API"). FR-10's numeric thresholds are vague by design but not silently vague — they're tagged `[ASSUMPTION]`, called out as "Out of Scope" for final calibration, and tracked in both Offene Fragen #3 and the Assumptions-Index. That's the honest way to leave a number open.

### Findings
- **medium** FR-14 (Serientermine) and FR-15 (Ortsangabe mit Kartenanzeige) have no "Consequences (testable)" section at all — just a one-line description each (§4.6). FR-13 has an "Out of Scope" note but likewise no consequences. *Fix:* Since these are Should-Have/deferred, this may be acceptable for now, but add testable consequences before any of the three is promoted into a sprint — otherwise story creation will have to invent acceptance criteria from scratch.
- **low** §7's backup/restore NFR uses "grundlegende Backup-/Wiederherstellungsfähigkeit" without a bound (frequency, retention, RPO/RTO) — but this is explicitly deferred to architecture and tracked in Offene Fragen #4, so it's a flagged gap rather than a hidden one. No action needed beyond following through in the architecture phase.

## Scope honesty — strong

Non-Goals (§5) does real work — six explicit exclusions, each with a one-line rationale rather than a bare bullet (e.g. collision warnings: "die gemeinsame Sichtbarkeit macht Kollisionen sichtbar, das Tool warnt aber nicht proaktiv davor"). `[ASSUMPTION]` tags appear inline at both points of inference (§4.3 Google Workspace type; FR-10 thresholds) and both round-trip cleanly into the Assumptions-Index (§11) — no orphaned tags, no untagged index entries. Open-items density (4 Open Questions + 2 indexed assumptions + 1 NOTE FOR PM) is proportionate to a low-stakes internal prototype, matching the rubric's calibration guidance directly.

## Downstream usability — strong

Glossar (§3) terms are used consistently everywhere checked — **Privat-Default**, **Verfügbarkeits-Status**, **Status-Heuristik**, **Mehrpersonen-Ansicht** all appear in FR text with the same casing and meaning as defined. FR IDs run FR-1 through FR-15 with no gaps or duplicates; SM IDs (SM-1, SM-2, SM-3, SM-C1) are likewise contiguous. UJ-1 has a named, context-carrying protagonist (Mara) and is cross-referenced by ID from the FRs it realizes (FR-1, FR-2, FR-10) rather than "see above." Sections are largely self-contained via Glossary terms rather than positional references.

## Shape fit — strong

This is correctly shaped as a capability spec for a small internal tool rather than forced into a full multi-persona/multi-UJ template: exactly one UJ, matched to the one recurring decision-critical moment (checking a colleague's availability). NFRs are operational rather than enterprise-grade (§7's explicit "kein Anspruch auf Hochverfügbarkeit"), which fits a single-operator, self-hosted prototype. The PRD doesn't over-invest in Discovery-style differentiation either — the Addendum's competitive scan is scoped down to "reale, wenig schmale Differenzierung" and explicitly disclaims market ambition.

## Mechanical notes

- Inline assumption-tag format is inconsistent: §4.3 uses a prefix style (`` `[ASSUMPTION]`: Das Team nutzt...``) while §8 uses an inline-complete style (`` `[ASSUMPTION: Team nutzt ein gemeinsames Workspace]` ``) for what is the same underlying assumption. Low severity — both resolve to the same Assumptions-Index entry, but a consistent tagging convention would make future greps easier.
- No broken cross-references found: `siehe FR-10`, `siehe Addendum`, `siehe Abschnitt 8`, `siehe Non-Goals` all resolve to real, matching content.
- Assumptions-Index (§11) roundtrips cleanly: both indexed entries (Google Workspace type; FR-10 numeric thresholds) have corresponding inline `[ASSUMPTION]` tags, and no inline tag is left unindexed.
- UJ-1 is the only User Journey in the document; given the single-dominant-use-case shape of this tool, that is proportionate rather than thin (see Shape fit).
