---
title: Reconciliation — Brief-Addendum vs. PRD (+ PRD-Addendum)
created: 2026-07-02
---

# Reconciliation: brief-addendum.md gegen PRD + PRD-Addendum

## Geprüfter Input
`briefs/brief-calendar-neu-bmad-2026-07-02/addendum.md` — enthält zwei Kernentscheidungen:

1. **Technischer Constraint:** Für die kontext-bewusste Verfügbarkeit auch bei importierten Terminen muss die App vollen Lese-Zugriff auf die Kalender-API (volle Event-Objekte) beantragen, nicht nur Free/Busy. Der "Privat-Default" ist explizit eine **Anzeigeregel**, keine Datenzugriffsgrenze.
2. **Verworfene Alternativen** zum Privat-Default-vs-Kontext-Ableitung-Konflikt:
   - Alt. 1: Kontext-Ableitung nur für native Termine (verworfen)
   - Alt. 2: Manuelles Kontext-Tag statt automatischer Ableitung (verworfen)
   - **Gewählt:** Grobe Heuristik (Dauer/Tageszeit/Teilnehmerzahl) auch für importierte Termine, begründet mit (a) ohnehin vollem Backend-Zugriff und (b) grundsätzlichem Team-Vertrauen. Alt. 1/2 bleiben als geprüfte Fallback-Optionen dokumentiert, falls die Entscheidung später (weniger Vertrauen, Cloud-Hosting mit mehr Beteiligten) in Frage gestellt wird.

## Abgleich gegen PRD.md

| Brief-Addendum-Element | Fundstelle in PRD | Befund |
|---|---|---|
| Voller API-Zugriff (nicht nur Free/Busy) technisch zwingend | FR-8 (Interner Vollzugriff), FR-5/FR-6 Consequences ("Import umfasst volle Termindaten ... unabhängig vom Privat-Default"), Abschnitt 8 "Integrations-Constraints": "Volle Kalender-API statt nur Frei/Busy-API ist technisch zwingend, damit die Status-Heuristik (FR-10) auch für synchronisierte Termine funktioniert." | Konsistent, wörtlich fast identisch übernommen |
| Privat-Default = Anzeigeregel, keine Datenzugriffsgrenze | Glossar-Eintrag "Privat-Default" (Abschnitt 3), FR-9 Consequences, Abschnitt 8 Privacy: "Privat-Default (FR-9) ist eine Anzeigeregel, keine Datenzugriffsgrenze — das Backend liest immer volle Termindaten (siehe Addendum zum Brief)." | Konsistent, korrekt auf Quelle referenziert |
| Gewählte Alternative: Heuristik auch für importierte/synchronisierte Termine | FR-10: "Das System leitet für jeden Termin (nativ wie synchronisiert) automatisch einen Verfügbarkeits-Status ab" | Konsistent — die gewählte Alternative wird als einzige, nicht mehr als offene Option dargestellt |
| Begründung "ohnehin voller Backend-Zugriff" | FR-8 + Abschnitt 8 Integrations-Constraints (kausale Verknüpfung: volle API nötig, "damit die Status-Heuristik auch für synchronisierte Termine funktioniert") | Konsistent |
| Begründung "Team-Vertrauen" | FR-9 Notes: "bewusst akzeptiertes Risiko auf Basis des grundsätzlichen Vertrauens im Team" | Konsistent, gleiche Sprache |
| Alt. 1 (nur native Termine) nicht wieder als offen dargestellt | FR-10 gilt explizit für "nativ wie synchronisiert"; kein Hinweis in Offene Fragen (Abschnitt 10) oder MVP-Scope, der das in Frage stellt | Kein Wiederaufgreifen, kein Widerspruch |
| Alt. 2 (rein manuelles Tag statt Ableitung) nicht wieder als offen dargestellt | FR-11 (manueller Override) ergänzt FR-10, ersetzt es aber nicht — Automatik bleibt Standardmechanismus, Override ist Zusatzfunktion für Einzelfälle (siehe UJ-1 Edge Case) | Kein Wiederaufgreifen. FR-11 ist eine Ergänzung, keine Rückkehr zu Alt. 2, da die automatische Ableitung der Standardfall bleibt |
| Google/Microsoft OAuth-Scope-Details (calendar.readonly, Admin-Consent) | Abschnitt 4.3 Feature-NFRs, Abschnitt 8 Integrations-Constraints, Abschnitt 10 Offene Fragen 1+2 | Konsistent und mit mehr technischem Detail versehen als im Brief-Addendum |

## Abgleich gegen PRD-eigenes Addendum.md

Das PRD-Addendum vertieft die Recherche-Grundlage, ohne die Entscheidung zu verändern:
- Abschnitt "OAuth/API-Gotchas" liefert die vollständige Recherche-Basis zu genau den Punkten, die die PRD unter "Integrations-Constraints" zusammenfasst (Google Internal-App/CASA, MS-Graph Admin-Consent, Webhook-Unzuverlässigkeit beider Provider) — reine Vertiefung, kein Widerspruch.
- Abschnitt "Vergleichslandschaft" ordnet FR-9 (Privat-Default) explizit gegen Reclaim.ai ein: "dort Redaktion auf Datenebene, hier eine reine Anzeigeregel bei vollem internen Datenzugriff" — bestätigt noch einmal explizit die Anzeigeregel-vs-Datenzugriffsgrenze-Unterscheidung aus dem Brief-Addendum.
- Abschnitt "Herkunft der Heuristik-Richtwerte" ist eine Ergänzung zu FR-10, betrifft nicht den Privat-Default-Konflikt oder die Alternativen.

## Ergebnis

**Keine Lücken gefunden.** Der technische OAuth-Scope-Constraint (volle Calendar-API statt Free/Busy) und die gewählte Alternative (grobe Heuristik auch für importierte Termine, Privat-Default als Anzeigeregel) werden im PRD und seinem Addendum durchgehend korrekt, an mehreren Stellen redundant abgesichert (Glossar, FR-8/9/10, Abschnitt 8) und mit Quellenverweis auf das Brief-Addendum übernommen. Die drei verworfenen Alternativen werden nirgends stillschweigend als offen oder gleichwertig dargestellt; FR-11 (manueller Override) ist eine additive Erweiterung und keine Rückkehr zu Alternative 2. Kein Widerspruch, keine implizite Neuverhandlung, kein Verlust des Constraints.
