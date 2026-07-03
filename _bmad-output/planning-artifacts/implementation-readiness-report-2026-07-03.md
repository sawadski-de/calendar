---
stepsCompleted: ["step-01", "step-02", "step-03", "step-04", "step-05", "step-06"]
inputDocuments:
  - "_bmad-output/planning-artifacts/prds/prd-calendar-neu-bmad-2026-07-02/prd.md"
  - "_bmad-output/planning-artifacts/architecture/architecture-calendar-neu-bmad-2026-07-02/ARCHITECTURE-SPINE.md"
  - "_bmad-output/planning-artifacts/ux-designs/ux-calendar-neu-bmad-2026-07-02/DESIGN.md"
  - "_bmad-output/planning-artifacts/ux-designs/ux-calendar-neu-bmad-2026-07-02/EXPERIENCE.md"
  - "_bmad-output/planning-artifacts/epics.md"
---

# Implementation Readiness Assessment Report

**Date:** 2026-07-03
**Project:** calendar-neu-bmad

## Document Inventory

**PRD:**
- Whole: `prds/prd-calendar-neu-bmad-2026-07-02/prd.md`

**Architecture:**
- Whole: `architecture/architecture-calendar-neu-bmad-2026-07-02/ARCHITECTURE-SPINE.md`

**UX Design (bmad-ux spine pair):**
- `ux-designs/ux-calendar-neu-bmad-2026-07-02/DESIGN.md`
- `ux-designs/ux-calendar-neu-bmad-2026-07-02/EXPERIENCE.md`

**Epics & Stories:**
- Whole: `epics.md`

No duplicates found (no whole+sharded conflicts for any document type).

## PRD Analysis

### Functional Requirements

FR-1: Ein Teammitglied kann seinen eigenen Kalender in Monats-, Wochen- und Tagesansicht anzeigen. Alle drei Ansichten zeigen sowohl native als auch synchronisierte Termine des eingeloggten Nutzers. Wechsel zwischen den drei Ansichten ist ohne Neuladen der Seite möglich.

FR-2: Ein Teammitglied kann eine Teilmenge der übrigen Teammitglieder auswählen und deren Kalender nebeneinander in Spalten sehen — in Tages- und Wochenansicht gleichermaßen. Vor der Mehrpersonen-Ansicht wählt der Nutzer die anzuzeigenden Personen explizit aus (kein automatisches Anzeigen aller 5-10 Personen gleichzeitig). Jede Spalte zeigt für fremde Termine ausschließlich "privat/beschäftigt" + Verfügbarkeits-Status (Privat-Default, FR-9), nie den echten Titel. Die Auswahl bleibt bestehen, wenn zwischen Tages- und Wochenansicht gewechselt wird. Out of Scope: eine überlagerte Darstellung mehrerer Kalender in einer einzigen Spalte.

FR-3: Ein Teammitglied kann einen neuen (nativen) Termin mit Titel, Datum/Uhrzeit, Dauer und optionalen Teilnehmern anlegen. Ein neu angelegter nativer Termin erscheint sofort in der eigenen Kalenderansicht. Nativen Terminen wird automatisch ein Verfügbarkeits-Status nach der Status-Heuristik (FR-10) zugewiesen.

FR-4: Ein Teammitglied kann per Klick auf einen eigenen Termin dessen volle Details öffnen (Titel, Zeit, Teilnehmer). Die Detailansicht zeigt bei eigenen Terminen immer die vollen Daten, unabhängig vom Privat-Default. Bei fremden Terminen zeigt ein Klick nur "privat/beschäftigt" + Verfügbarkeits-Status, keine Detaildaten — außer, wenn das Teammitglied selbst als Teilnehmer eingetragen ist (siehe FR-9, Ausnahme). Out of Scope: Ortsanzeige ist erst mit FR-15 Teil der Detailansicht.

FR-5: Das System importiert regelmäßig alle Kalendertermine eines verbundenen Outlook-Kontos. Ein neuer oder geänderter Termin in Outlook erscheint spätestens nach dem nächsten Sync-Zyklus (Ziel: wenige Minuten) im Tool. Der Import umfasst volle Termindaten (Titel, Teilnehmer, Dauer) für die interne Verarbeitung. Jeder importierte Termin wird anhand einer stabilen, providerseitigen Event-ID identifiziert; eine Verschiebung/Änderung aktualisiert den bestehenden Eintrag statt einen zusätzlichen Termin anzulegen. Wird ein Termin an der Quelle gelöscht/abgesagt, wird der Eintrag spätestens im nächsten Sync-Zyklus entfernt (inkl. eines eventuell davon abgeleiteten Status). Out of Scope: parallele Doppelkonten pro Person; Ausnahmen innerhalb importierter Serientermine.

FR-6: Das System importiert regelmäßig alle Kalendertermine eines verbundenen Google-Kontos, analog zu FR-5 (inkl. Dedup-/Lösch-Verhalten über Provider-Event-ID).

FR-7: Das System schreibt zu keinem Zeitpunkt Termindaten zurück nach Outlook oder Google. Ein im Tool angelegter, geänderter oder gelöschter nativer Termin erzeugt keine Schreiboperation gegen die Outlook- oder Google-API.

FR-8: Das System liest und speichert für jeden Termin (nativ wie synchronisiert) die vollen Daten: Titel, Teilnehmer, Dauer, Ort. Volle Termindaten sind Voraussetzung für die Status-Heuristik (FR-10) und stehen dem Termin-Eigentümer in der eigenen Detailansicht zur Verfügung (FR-4).

FR-9: Für jeden Termin, der nicht dem eigenen Account gehört, zeigt das System anderen Teammitgliedern ausschließlich "privat/beschäftigt" plus den Verfügbarkeits-Status — nie Titel, Teilnehmer oder Ort. Ausnahme: Ist das anfragende Teammitglied selbst als Teilnehmer in einem fremden nativen Termin eingetragen, sieht es dessen volle Details, auch ohne Eigentümer zu sein. Kein UI-Pfad zeigt einem Teammitglied den echten Titel/Teilnehmer eines fremden Termins ohne eigene Teilnahme. Die Filterung erfolgt serverseitig pro Anfrage anhand der Identität des anfragenden Nutzers — eine rein clientseitige Ausblendung erfüllt diese Anforderung nicht.

FR-10: Das System leitet für jeden Termin (nativ wie synchronisiert) automatisch einen Verfügbarkeits-Status ab, basierend auf Dauer, Teilnehmerzahl und Tageszeit. Termine ganz ohne weitere Teilnehmer gelten immer als unterbrechbar, unabhängig von ihrer Dauer (Vorrang vor allen anderen Regeln). Kurze Termine mit mindestens einem Teilnehmer (Richtwert bis ca. 30-45 Minuten) gelten als unterbrechbar. Lange Termine mit mindestens einem Teilnehmer (Richtwert ab ca. 1,5-2 Stunden mit 3+ Teilnehmern, sowie ganztägige Termine mit mindestens einem Teilnehmer) gelten als bitte-nicht-stören. Tageszeit ist nachrangig. Die Status-Ableitung läuft mit derselben Sync-Verzögerung wie der übrige Kalender. Hat ein Teammitglied zum aktuellen Zeitpunkt keinen laufenden Termin, gilt es standardmäßig als unterbrechbar.

FR-11: Ein Teammitglied kann seinen eigenen, aktuell angezeigten Verfügbarkeits-Status jederzeit manuell übersteuern. Der Override bezieht sich auf den Nutzer als Ganzes, nicht auf eine einzelne Termininstanz. Ein manuell gesetzter Status bleibt bestehen, bis der Nutzer ihn aktiv zurücksetzt oder ändert — er wird nicht beim nächsten Sync-Zyklus automatisch überschrieben und endet nicht automatisch mit dem Ende des zum Zeitpunkt der Übersteuerung aktiven Termins. Andere Teammitglieder sehen keinen Unterschied zwischen automatisch abgeleitetem und manuell gesetztem Status.

FR-12 (Should-Have): Ein Teammitglied kann mehrere Teammitglieder auswählen; das System schlägt automatisch gemeinsame freie Zeitfenster vor. Der Slot-Finder berücksichtigt ausschließlich reines Frei/Beschäftigt, nicht den abgeleiteten Verfügbarkeits-Status.

FR-13 (Should-Have): Das System kann ein Teammitglied an einen bevorstehenden eigenen Termin erinnern. Erinnerungen beziehen sich ausschließlich auf eigene Termine, nie auf fremde.

FR-14 (Should-Have): Ein Teammitglied kann wiederkehrende native Termine anlegen (täglich/wöchentlich/monatlich). Jede erzeugte Instanz einer Serie erscheint einzeln in Kalenderansicht und Mehrpersonen-Ansicht und erhält einzeln einen Verfügbarkeits-Status nach FR-10.

FR-15 (Should-Have): Ein Teammitglied kann einem nativen Termin einen Ort mit Autovervollständigung hinzufügen; die Detailansicht zeigt eine Kartendarstellung. Ein Termin ohne angegebenen Ort zeigt keine Kartendarstellung. Die Ortsangabe unterliegt demselben Privat-Default wie andere Termindetails.

Total FRs: 15 (11 Must-Have/MVP: FR-1 bis FR-11; 4 Should-Have/nicht-MVP: FR-12 bis FR-15)

### Non-Functional Requirements

NFR-1: Sync-Aktualität — Alle Sync-Zyklen (Outlook, Google, Status-Ableitung) laufen im Bereich weniger Minuten. Kein Echtzeit-Anspruch — validiert gegen SM-2.

NFR-2: Sync-Transparenz (Must-Have) — Die Oberfläche zeigt erkennbar an, wann ein Kalender zuletzt erfolgreich synchronisiert wurde. Bleibt ein Sync-Zyklus für ein Konto wiederholt aus (z. B. wegen abgelaufenem/widerrufenem Token), muss das für den Betreiber sichtbar werden.

NFR-3: Datenhaltung nativer Termine (Must-Have) — Native Termine existieren ausschließlich in der Tool-Datenbank und sind bei Datenverlust unwiederbringlich. Das System muss eine grundlegende Backup-/Wiederherstellungsfähigkeit für native Termine bereitstellen.

NFR-4: Zugriffsschutz (Must-Have) — Zugriff auf das Tool erfordert eine Anmeldung. Gespeicherte OAuth-Tokens (Zugriff auf Outlook/Google jedes Teammitglieds) müssen verschlüsselt abgelegt werden — ein Datenbankzugriff allein darf keinen direkten Zugriff auf die verbundenen Kalenderkonten ermöglichen.

NFR-5: Mobil/Responsiv — Alle Kernfunktionen (Ansichten, Termin anlegen, Verfügbarkeits-Status, Detailansicht) sind auf mobilen Endgeräten per Browser nutzbar. Für die Mehrpersonen-Ansicht (FR-2) ist das konkrete Verhalten bei vielen ausgewählten Spalten auf kleinen Bildschirmen architekturseitig zu lösen.

NFR-6: Betrieb — Selbst gehostet, ein Betreiber (Bus-Faktor 1) für Wartung und OAuth-Token-Pflege — bewusst akzeptiertes Betriebsrisiko, kein Anspruch auf Hochverfügbarkeit oder definierte Recovery-Zeiten über die native-Termine-Backup-Anforderung hinaus.

Total NFRs: 6

### Additional Requirements

**Constraints/Guardrails (PRD §8):**
- Privat-Default ist eine Anzeigeregel, keine Datenzugriffsgrenze; externe Teilnehmerdaten werden ohne zusätzliche Schutzmaßnahme gespeichert (akzeptiertes Risiko).
- Volle Kalender-API statt nur Frei/Busy-API ist technisch zwingend, damit die Status-Heuristik auch für synchronisierte Termine funktioniert.
- Google: sensibler Scope `calendar.readonly` — als Internal-App innerhalb einer Google-Workspace-Organisation ohne OAuth-Verifizierung nutzbar `[ASSUMPTION: gemeinsames Workspace]`.
- Microsoft Graph: `Calendars.Read` benötigt je nach Tenant-Konfiguration Admin-Consent; Tenant-Admins können Self-Consent organisationsweit sperren.
- Beide Provider garantieren keine zuverlässige Echtzeit-Zustellung von Änderungen — Polling-Intervall von wenigen Minuten umgeht dieses Problem.

**Non-Goals (PRD §5):** kein delegierter Zugriff, keine Mehrteam-Unterstützung, kein Echtzeit-Sync, kein Zurückschreiben, keine aktive Kollisionswarnung, kein Fragmentierungs-Erzwingungsmechanismus, kein Benachrichtigungsversand an externe Teilnehmer, keine Bearbeitung synchronisierter Termine, kein spezifizierter Deprovisionierungs-Workflow (in dieser Runde — später von Architecture in AD-12 doch gelöst).

**Offene Fragen (PRD §10):** 8 offene Punkte dokumentiert, u. a. Google-Workspace-Bestätigung, Microsoft-Tenant-Admin-Consent, Heuristik-Feinkalibrierung, Backup-Frequenz/Restore-Prozess, Deprovisionierung, Token-Widerruf-Sichtbarkeit, Zeitzonen-Annahme, externe Teilnehmerdaten-Aufbewahrung.

### PRD Completeness Assessment

Die PRD ist mit Status `final` sehr detailliert und präzise: jede FR trägt testbare "Consequences" und teils "Out of Scope"-Abgrenzungen, Non-Goals sind explizit, und ein eigener Assumptions-Index sowie ein Offene-Fragen-Abschnitt machen verbleibende Unsicherheiten transparent statt sie zu verstecken. Von den 8 offenen Fragen sind mehrere bereits in der Architecture-Spine aufgelöst worden (Heuristik-Kalibrierung → AD-5; Backup/Restore → AD-14; Deprovisionierung → AD-12; Token-Widerruf-Sichtbarkeit → AD-16), was für gute Weiterverarbeitung zwischen den Phasen spricht. Verbleibend offen (architektonisch nicht auflösbar, sondern echte externe/organisatorische Unsicherheiten): Google-Workspace-Bestätigung (#1), Microsoft-Tenant-Admin-Consent-Klärung (#2), Zeitzonen-Annahme-Bestätigung (#7), externe Teilnehmerdaten-Aufbewahrungsregel (#8). Diese vier sind keine Blocker für den Start der Implementierung, sollten aber vor bzw. während des produktiven Rollouts geklärt werden.

## Epic Coverage Validation

### Coverage Matrix

| FR-Nummer | PRD-Anforderung (Kurzform) | Epic-Abdeckung | Status |
| --- | --- | --- | --- |
| FR-1 | Eigene Kalenderansicht (Monat/Woche/Tag) | Epic 1, Story 1.2 | ✓ Covered |
| FR-2 | Mehrpersonen-Ansicht | Epic 3, Story 3.1 (+3.2 Monat) | ✓ Covered |
| FR-3 | Termin anlegen (nativ) | Epic 1, Story 1.3 | ✓ Covered |
| FR-4 | Termin-Detailansicht | Epic 1, Story 1.4 (eigen) + Epic 3, Story 3.1 (status-only) | ✓ Covered |
| FR-5 | Outlook-Import | Epic 2, Story 2.2 | ✓ Covered |
| FR-6 | Google-Calendar-Import | Epic 2, Story 2.1 | ✓ Covered |
| FR-7 | Kein Zurückschreiben | Epic 2, Story 2.1/2.2 (strukturell via `ICalendarProvider`) | ✓ Covered |
| FR-8 | Interner Vollzugriff | Epic 1 (nativ, Story 1.3) + Epic 2 (synchronisiert, Story 2.1/2.2) | ✓ Covered |
| FR-9 | Privat-Default gegenüber anderen | Epic 3, Story 3.1 | ✓ Covered |
| FR-10 | Automatische Status-Ableitung | Epic 1, Story 1.3 (inkl. Grenzwert-ACs) | ✓ Covered |
| FR-11 | Manueller Status-Override | Epic 4, Story 4.1 | ✓ Covered |
| FR-12 (Should-Have) | Gemeinsamer Slot-Finder | Kein Epic — bewusst nicht im MVP-Breakdown | ⚪ Deferred (by design) |
| FR-13 (Should-Have) | Termin-Erinnerungen | Kein Epic — bewusst nicht im MVP-Breakdown | ⚪ Deferred (by design) |
| FR-14 (Should-Have) | Serientermine (nativ) | Kein Epic — bewusst nicht im MVP-Breakdown | ⚪ Deferred (by design) |
| FR-15 (Should-Have) | Ortsangabe mit Kartenanzeige | Kein Epic — bewusst nicht im MVP-Breakdown | ⚪ Deferred (by design) |

### Missing Requirements

Keine unbeabsichtigt fehlende Abdeckung gefunden. FR-12 bis FR-15 sind laut PRD §6.2 explizit "Out of Scope für MVP" und in `epics.md` unter "Nicht im MVP abgedeckt" korrekt als bewusste Auslassung dokumentiert (kein stillschweigendes Vergessen). Alle NFRs (NFR-1 bis NFR-6) sind ebenfalls in der FR/NFR-Coverage-Map von `epics.md` einem Epic zugeordnet (NFR-1/NFR-2 → Epic 2; NFR-3/NFR-6 → Epic 5; NFR-4 → Epic 1; NFR-5 → Cross-cutting Epic 1+3).

### Coverage Statistics

- Total PRD FRs: 15 (11 Must-Have/MVP + 4 Should-Have)
- FRs covered in epics: 11 von 11 MVP-FRs (100%); 0 von 4 Should-Have-FRs (bewusst, siehe PRD §6.2)
- Coverage-Prozentsatz (MVP-Scope, relevant für Implementierungsbereitschaft): **100%**
- Total NFRs: 6, alle 6 einem Epic zugeordnet (100%)

## UX Alignment Assessment

### UX Document Status

Found — bmad-ux Spine-Paar (`DESIGN.md` + `EXPERIENCE.md`), Status `final`.

### UX ↔ PRD Alignment

Die drei Key Flows in EXPERIENCE.md decken die PRD-Nutzungsmomente direkt ab (Flow 1 = UJ-1 "Mara prüft Kollegen-Status"; Flow 2 = FR-3 Terminerstellung; Flow 3 = operative Sync-Transparenz-NFR aus Betreiber-Sicht). Vier Punkte gehen über den wörtlichen PRD-Text hinaus, sind aber in EXPERIENCE.md jeweils explizit als "explicit user decision" bzw. "memlog decision" der UX-Discovery gekennzeichnet — kein stiller Scope-Creep, sondern dokumentierte, mit dem Nutzer getroffene Erweiterungen, die `epics.md` bereits korrekt aufgenommen hat:

- **Mehrpersonen-Monatsansicht** — PRD FR-2 spezifiziert nur Tag/Woche; EXPERIENCE.md erweitert bewusst auf Monat ("UX-phase scope extension beyond PRD FR-2 ... flagged back to product"). → Epic 3, Story 3.2.
- **Echtes i18n-Fundament (DE/EN)** — in PRD nicht erwähnt, in EXPERIENCE.md als explizite Nutzerentscheidung eingeführt. → Epic 1, Story 1.1 (UX-DR36).
- **Echtes Admin/Member-Rollensystem** — in PRD nicht erwähnt (PRD nennt nur "ein Betreiber, Bus-Faktor 1"), in EXPERIENCE.md als explizite Entscheidung für ein wachstumsfähiges Team eingeführt. → Epic 1 (Rollenfeld) + Epic 2, Story 2.3 (Durchsetzung).
- **WCAG 2.1 AA als formales Ziel** — in PRD nicht erwähnt, in EXPERIENCE.md formal adoptiert. → über alle Epics verteilt als Akzeptanzkriterien.

Diese vier Erweiterungen sind bereits in `epics.md` korrekt verarbeitet; sie stellen keine Inkonsistenz dar, sondern spätere, dokumentierte Präzisierungen des Product Owners während der UX-Phase.

### UX ↔ Architecture Alignment

Die Architecture-Spine referenziert DESIGN.md/EXPERIENCE.md explizit als Quellen (`sources:`-Frontmatter) und bildet UX-Oberflächen direkt auf Architektur-Komponenten ab (Capability-Map: "Admin → Sync-Übersicht (EXPERIENCE.md) → AD-16/AD-17"). Keine UI-Komponente aus der UX-Spine bleibt architektonisch ungedeckt:

- Person-Selektor, Status-Override-Flyout, Sync-Indikator, Admin-Übersicht → jeweils konkreten Application-Services/Entitäten zugeordnet (`AppointmentViewService`, `CurrentStatusService`, `CalendarConnection`, `Person.Role`).
- Cookie-Session-Auth (AD-10) unterstützt die UX-Anforderung "kein Access-Token im Client".
- RFC-7807-Fehlerformat (AD-13) unterstützt die i18n-Anforderung (Übersetzung ausschließlich im Frontend).

### Warnings

Keine kritischen Warnungen. Einziger Hinweis: Die vier oben genannten PRD-stillen/UX-expliziten Erweiterungen sollten Dennis als Product Owner bewusst sein (er ist es bereits, da er sie selbst in der UX-Discovery entschieden hat) — keine Rückfrage an Dritte nötig, da Ein-Personen-Projekt.

## Epic Quality Review

Rigorose Prüfung gegen die create-epics-and-stories-Standards (Nutzerwert, Unabhängigkeit, Abhängigkeitsrichtung, Story-Größe).

### Compliance-Checkliste je Epic

| Epic | Nutzerwert | Unabhängig (nur rückwärts) | Stories richtig dimensioniert | Keine Vorwärtsabhängigkeit | Entitäten bei Bedarf | ACs klar | FR-Rückverfolgbarkeit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | ✓ | ✓ | ✓ (1.1 bewusst groß, s. u.) | ⚠️ siehe Finding #1 | ✓ | ✓ | ✓ |
| 2 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| 3 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| 4 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| 5 | ✓ | ✓ (aber Abhängigkeitsangabe ungenau, s. Finding #2) | ✓ | ✓ | ✓ | ✓ | ✓ |

### 🟠 Major Issues

**Finding #1 — Vorwärtsreferenz in Epic 1, Story 1.1:** Die AC "ein Admin versucht, sich selbst oder den letzten verbleibenden Admin auf `Member` herabzustufen **oder zu deaktivieren**" setzt ein `IsActive`-Feld voraus. Dieses Feld wird aber laut Datenbank/Entity-Erstellungsprinzip erst in Epic 5, Story 5.1 (AD-12, Soft-Deprovisionierung) eingeführt. Story 1.1 kann diese AC damit zum Zeitpunkt ihrer Umsetzung nicht vollständig implementieren — ein klassischer Verstoß gegen "keine Vorwärtsabhängigkeit innerhalb/zwischen Stories".
- **Impact:** Ein Dev-Agent, der Story 1.1 exakt nach AC umsetzt, müsste entweder das `IsActive`-Feld vorzeitig einführen (Verstoß gegen "Entitäten nur bei Bedarf") oder die AC nicht vollständig erfüllen können.
- **Empfehlung:** Story 1.1s Guard auf die Rollen-Herabstufung beschränken ("... auf `Member` herabzustufen" — `IsActive`/"deaktivieren" streichen). Epic 5, Story 5.1 trägt bereits eine eigene, korrekte AC für "letzten Admin nicht deaktivieren" (verweist auf Story 1.1) — dieser Verweis bleibt gültig, sobald Story 1.1 korrigiert ist, da beide Guards (Rollen-Herabstufung in 1.1, Deaktivierung in 5.1) unabhängig voneinander denselben Bus-Faktor-1-Grundsatz durchsetzen.

**Finding #2 — Ungenaue Abhängigkeitsangabe für Epic 5:** Die epics_list-Begründung für Epic 5 nennt keine explizite Abhängigkeit außer Epic 1. Tatsächlich referenziert Story 5.1 sowohl den Personen-Selektor (`... verschwindet aus dem Personen-Selektor (FR-2)`, eingeführt in Epic 3, Story 3.1) als auch OAuth-Tokens/`CalendarConnection` (`... werden gelöscht/widerrufen`, eingeführt in Epic 2). Epic 5 hängt damit funktional von Epic 1, 2 UND 3 ab, nicht nur von Epic 1.
- **Impact:** Kein struktureller Fehler — die dokumentierte Ausführungsreihenfolge (1→2→3→4→5) erfüllt diese Abhängigkeit bereits korrekt. Das Risiko ist rein dokumentarisch: Würde jemand versuchen, Epic 5 parallel zu Epic 3/4 einzuplanen (weil die epics_list nur "braucht Epic 1" suggeriert), entstünde eine unvollständige Story.
- **Empfehlung:** Epic-5-Beschreibung in `epics.md` präzisieren: "Setzt Epic 1 (Rollenfeld), Epic 2 (OAuth/CalendarConnection) und Epic 3 (Personen-Selektor) voraus."

### 🟡 Minor Concerns

- Epic 1 Story 1.1 ist deutlich umfangreicher als die übrigen Stories (Scaffold + Login + i18n + Tokens + Admin-Bootstrapping). Dies wurde bereits während der Story-Erstellung per Advanced-Elicitation-Review mit Dennis bewusst akzeptiert (vergleichbar mit einem "Starter-Template-Setup" bei Epic 1/Story 1, hier ohne Starter-Template) — kein neuer Befund, hier nur zur Vollständigkeit dokumentiert.
- Kein CI/CD-Pipeline-Setup in Epic 1 vorgesehen. Weder PRD noch Architecture-Spine fordern eine CI/CD-Pipeline; für ein Bus-Faktor-1-Solo-Projekt ohne Team-Review-Prozess (vgl. `project-context.md`, Development Workflow Rules bewusst ausgelassen) ist das nachvollziehbar und kein Verstoß gegen eine bestehende Anforderung — reine Beobachtung, keine Handlungsempfehlung.

### 🔴 Critical Violations

Keine gefunden. Insbesondere: keine rein-technischen Epics ohne Nutzerwert, keine Epic-Ebene-Vorwärtsabhängigkeiten (jede Epic-Abhängigkeit zeigt nach hinten), keine Story, die auf eine zukünftige Story derselben Epic wartet.

## Summary and Recommendations

### Overall Readiness Status

**READY** — beide Major-Findings wurden am 2026-07-03 direkt in `epics.md` behoben (Story 1.1 Guard beschränkt, Epic-5-Voraussetzungen präzisiert). Keine offenen kritischen oder strukturellen Mängel mehr.

### Critical Issues Requiring Immediate Action

Keine kritischen Probleme gefunden.

### Major Issues Requiring Action Before Implementation

1. **Epic 1, Story 1.1** — AC referenziert vorzeitig ein `IsActive`-Feld ("... herabzustufen oder zu deaktivieren"), das erst in Epic 5 eingeführt wird. Guard auf Rollen-Herabstufung beschränken. **✅ Behoben (2026-07-03):** AC beschränkt sich jetzt auf Rollen-Herabstufung; Deaktivierungs-Guard ist explizit Epic 5/Story 5.1 zugewiesen.
2. **Epic 5, Beschreibung** — Abhängigkeitsangabe unvollständig; Epic 5 hängt tatsächlich von Epic 1, 2 UND 3 ab (Personen-Selektor, OAuth-Tokens), nicht nur von Epic 1. Beschreibung präzisieren. **✅ Behoben (2026-07-03):** Epic-5-Abschnitt trägt jetzt einen expliziten "Voraussetzungen"-Hinweis (Epic 1, 2, 3).

### Recommended Next Steps

1. Story 1.1s Admin-Guard-AC korrigieren (Deaktivierungs-Referenz entfernen, nur Rollen-Herabstufung abdecken).
2. Epic-5-Beschreibung in `epics.md` um die tatsächlichen Abhängigkeiten (Epic 1, 2, 3) ergänzen.
3. Nach Korrektur: keine erneute vollständige Runde nötig — beide Fixes sind isoliert und beeinflussen keine andere Story.
4. Danach: Freigabe für Phase 4 (Sprint Planning) erteilen.

### Final Note

Diese Bewertung identifizierte 2 Major-Findings und 2 Minor-Beobachtungen über 5 Prüfkategorien (Dokumenten-Discovery, PRD-Analyse, Epic-Coverage, UX-Alignment, Epic-Qualität). Die FR-/NFR-Abdeckung ist vollständig (100% MVP), UX und Architektur sind eng aufeinander abgestimmt. Die beiden Major-Findings sollten behoben werden, bevor Sprint Planning beginnt — sie sind jedoch klein genug, um sofort und ohne weitere Elicitation-Runde behoben zu werden.

**Assessor:** bmad-check-implementation-readiness · **Date:** 2026-07-03
