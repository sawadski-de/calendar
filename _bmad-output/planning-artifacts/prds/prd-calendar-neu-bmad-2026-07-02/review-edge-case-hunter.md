---
title: Edge Case Hunter Review — PRD Team-Terminkalender mit Synchronisation
source: prd-calendar-neu-bmad-2026-07-02/prd.md
method: bmad-review-edge-case-hunter (exhaustive path enumeration, unhandled paths only)
date: 2026-07-02
---

# Edge Case Hunter — Findings

Scope: FR-1 … FR-15, Abschnitt 5 (Non-Goals), Abschnitt 6 (MVP-Scope), Abschnitt 7 (Cross-Cutting NFRs), Abschnitt 8 (Constraints/Guardrails). Only unhandled branches/boundaries are listed; explicitly resolved paths (e.g. FR-11's override-persistence rule, FR-12's explicit free/busy-only scope) are omitted.

## Sync (FR-5, FR-6, FR-7)

1. **Dual-Provider-Duplikat.** Ein Teammitglied verbindet sowohl ein Outlook- als auch ein Google-Konto (oder ein Termin gelangt über beide Wege ins System, z. B. durch eine Weiterleitung/Kopie), und derselbe Termin wird aus beiden Quellen importiert. FR-5/FR-6 spezifizieren keine Dedup-/Merge-Regel über Provider-Grenzen hinweg. → Termin erscheint doppelt, Status-Heuristik (FR-10) läuft zweimal mit potenziell unterschiedlichem Ergebnis, Kalenderansicht wirkt unglaubwürdig.

2. **Löschung an der Quelle.** FR-5/FR-6 beschreiben nur, dass ein "neuer oder geänderter" Termin nach dem nächsten Sync-Zyklus erscheint. Keine Aussage dazu, was passiert, wenn ein Termin in Outlook/Google gelöscht oder eine Einladung storniert wird. → Gelöschte Termine bleiben potenziell als "Ghost-Termine" im Tool bestehen, inkl. daraus abgeleitetem bitte-nicht-stören-Status, der nie zurückgesetzt wird.

3. **Verschiebung an der Quelle (Identitäts-/Reconciliation-Regel fehlt).** Wird ein Termin in Outlook/Google auf eine andere Zeit verschoben, ist nicht spezifiziert, anhand welches stabilen Merkmals (Provider-Event-ID o. ä.) das System die alte und neue Version demselben Termin zuordnet. → Risiko, dass eine Verschiebung als zusätzlicher neuer Termin statt als Update interpretiert wird (Duplikat statt Korrektur).

4. **Wiederkehrende synchronisierte Termine.** FR-14 regelt nur native Serientermine. Für aus Outlook/Google importierte Serientermine (inkl. Einzelausnahmen: eine Instanz verschoben/abgesagt, Serie nachträglich geändert) gibt es keine Regel. → Ausnahmen einer importierten Serie könnten als Phantom-Instanzen erscheinen oder eine reale Absage einer Einzelinstanz wird nicht abgebildet.

5. **Token-Ablauf/-Widerruf während des Betriebs (technisch, nicht nutzerseitig).** Abschnitt 8 behandelt Admin-Consent/Verifizierung als offene Frage, aber ein laufzeitbedingter Token-Ablauf oder -Widerruf während des Betriebs (getrennt vom bewussten Zugriffsentzug durch ein Teammitglied) hat keine definierte Reaktion (Retry, Alarmierung des Betreibers, Nutzerhinweis). → Bei Bus-Faktor-1-Betrieb (Abschnitt 7) kann ein stiller Sync-Ausfall unbemerkt bleiben.

## Zeitzonen & Uhrzeit (FR-1, FR-2, FR-10, Abschnitt 7)

6. **Keine Zeitzonen-Regel für die Mehrpersonen-Ansicht.** FR-2 zeigt Spalten mehrerer Teammitglieder nebeneinander, aber es ist nicht spezifiziert, in welcher Zeitzone die Spaltenachse dargestellt wird, wenn Teammitglieder in unterschiedlichen Zeitzonen sitzen. → Nebeneinanderliegende Zeitfenster können unterschiedliche tatsächliche Ortszeiten repräsentieren, was die Kerneinschätzung "ist die Person jetzt ansprechbar" verfälscht.

7. **Zeitumstellung (DST).** Weder die Sync-Zyklen (FR-5/FR-6) noch die dauerbasierte Status-Heuristik (FR-10) adressieren den Fall einer Sommer-/Winterzeit-Umstellung (z. B. doppelt vorkommende oder fehlende lokale Uhrzeit während der Umstellungsnacht). → Fehlerhafte Dauerberechnung oder verschobene/verdoppelte Sync-Fenster rund um den Umstellungszeitpunkt.

## Status-Heuristik (FR-10, FR-11)

8. **Widerspruch bei ganztägigem Termin ohne Teilnehmer.** FR-10 formuliert zwei Regeln, die für denselben Fall unterschiedliche Ergebnisse liefern: "Termine ganz ohne weitere Teilnehmer gelten unabhängig von ihrer Dauer als unterbrechbar" vs. "…sowie ganztägige Termine gelten als bitte-nicht-stören" (ohne explizite Bedingung auf 3+ Teilnehmer für die ganztägige Variante). Ein ganztägiger Termin ohne Teilnehmer (z. B. selbst geblockter Fokustag) fällt unter beide Regeln mit gegensätzlichem Ergebnis. → Heuristik-Ergebnis für diesen Fall ist unspezifiziert/widersprüchlich.

9. **Nachträgliche Teilnehmer-Änderung an einem laufenden/bereits eingestuften Termin.** Weder für den Fall, dass einem ursprünglich kurzen/teilnehmerlosen Termin nachträglich (auch mitten in der Laufzeit) weitere Teilnehmer hinzugefügt werden, noch für die Interaktion mit einem aktiven Status-Override (FR-11) ist eine Neubewertungs-/Vorrangregel definiert. → Status kann veraltet bleiben (unterbrechbar angezeigt, obwohl der Termin faktisch zum Gruppen-Meeting wurde) oder ein Override wird inkonsistent behandelt.

10. **Geltungsbereich des manuellen Overrides unklar.** FR-11 spricht von "seinen eigenen … Verfügbarkeits-Status", ohne zu klären, ob sich der Override auf eine konkrete Termininstanz oder auf den nutzerweiten aktuellen Status unabhängig vom Kalender bezieht. → Nach Ende des überschriebenen Termins könnte ein global interpretierter Override den Status unbeabsichtigt dauerhaft auf bitte-nicht-stören belassen, obwohl keine Termine mehr laufen.

11. **Kein aktiver Termin.** Für den Zustand "Person hat aktuell keinen laufenden Termin" ist kein Default-Status definiert (FR-10 leitet nur pro Termin ab). → Uneinheitliche/undefinierte Anzeige im Leerlauf-Zustand.

12. **Mehrere überlappende Termine.** Falls eine Person zwei sich überschneidende Termine mit unterschiedlich abgeleitetem Status hat, ist nicht geregelt, welcher der "aktuell angezeigte" Status ist. → Mehrdeutiges Anzeigeverhalten bei Terminüberschneidung.

## Mehrpersonen-Ansicht / Team-Mitgliedschaft (FR-2, FR-8, FR-9, explizit angefragt)

13. **Zugriffsentzug durch ein ausgewähltes Teammitglied.** Widerruft ein Teammitglied den Kalenderzugriff (OAuth) oder eine Verbindung bricht, ist nicht spezifiziert, wie das Tool das für andere sichtbar macht (letzter bekannter Stand weiter angezeigt? Fehlerzustand? Hinweis an Betrachter oder nur an den Betreiber?). → Andere Teammitglieder könnten auf Basis veralteter, aber unmarkierter Daten eine falsche Verfügbarkeitseinschätzung treffen.

14. **Teammitglied verlässt das Team.** Keine Regel, ob die Person weiterhin in der Personenauswahl (FR-2) erscheint, ob ihre historischen nativen Termine (auch als Teilnehmer in fremden Terminen) erhalten oder gelöscht werden, und ob/wie ihr Konto/Datenzugriff deprovisioniert wird. → Daten einer ausgeschiedenen Person könnten unbegrenzt fortbestehen oder die Personenauswahl-UI bricht mit verwaisten Einträgen.

15. **Neues Teammitglied kommt hinzu (Kehrseite von #14).** Onboarding-Ablauf (OAuth-Verbindung, ob/wie historische Termine rückwirkend importiert werden) ist nicht spezifiziert.

## Termin anlegen / Eigentümerschaft (FR-3, FR-4, FR-7)

16. **Eigentümerschafts-/Sichtbarkeitsmodell bei mehreren Teammitgliedern als Teilnehmer.** Legt Mara einen nativen Termin an und lädt Tom (Teammitglied) als Teilnehmer ein, ist nicht geregelt, ob Tom den Termin in seiner eigenen Kalenderansicht (FR-1/FR-4: volle Details, da "eigen") oder als fremden Termin (FR-9: Privat-Default) sieht. Beide Regeln könnten gleichzeitig für denselben Termin bei unterschiedlichen Nutzern beansprucht werden, ohne dass eine Vorrangregel existiert. → Widersprüchliches Anzeigeverhalten für einen Termin mit mehreren Teammitgliedern als Teilnehmer.

17. **Externe Teilnehmer an nativen Terminen.** FR-3 erlaubt optionale Teilnehmer bei einem nativen Termin, aber da kein Zurückschreiben (FR-7) und kein E-Mail-/Einladungsversand spezifiziert ist, bleibt unklar, wie ein extern eingeladener Kunde von diesem Termin überhaupt erfährt. → Extern hinzugefügte Teilnehmer werden faktisch nie benachrichtigt.

18. **Bearbeitung synchronisierter Termine im Tool.** Es ist nicht spezifiziert, ob ein Nutzer die Felder eines synchronisierten (nicht-nativen) Termins im Tool überhaupt verändern kann. Falls ja: Da Sync nur einseitig importiert (FR-7), würde jede lokale Änderung beim nächsten Sync-Zyklus stillschweigend durch die Quelldaten überschrieben. → Möglicher Datenverlust/Verwirrung ohne Warnhinweis.

## Should-Have-Features (FR-13, FR-14)

19. **Serientermin-Bearbeitung/-Löschung (FR-14).** Es ist nicht geregelt, ob eine Änderung/Löschung "nur diese Instanz", "diese und folgende" oder "die ganze Serie" betrifft.

## Mobil/Responsiv (Abschnitt 7 × FR-2)

20. **Darstellung vieler Spalten auf Mobilgeräten.** Abschnitt 7 verlangt, dass Kernfunktionen mobil nutzbar sind; für die Mehrpersonen-Ansicht (bis zu ~9 Spalten bei 5-10 Personen) ist kein Verhalten auf kleinen Bildschirmen definiert (horizontales Scrollen, Stapelung, Begrenzung der gleichzeitig wählbaren Personen). → Mehrpersonen-Ansicht könnte auf Mobilgeräten faktisch unbenutzbar sein, ohne dass das als Anforderung erkennbar wäre.

---

**Zusammenfassung:** 20 unbehandelte Randfälle/Verzweigungen identifiziert, primär in den explizit angefragten Bereichen Sync-Deduplizierung/-Löschung, Zeitzonen, Status-Heuristik-Widerspruch (ganztägig + 0 Teilnehmer) und Mehrpersonen-Ansicht (Zugriffsentzug/Teammitglied-Austritt).
