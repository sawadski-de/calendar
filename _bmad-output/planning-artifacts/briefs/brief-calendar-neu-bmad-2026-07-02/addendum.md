---
title: Addendum — Team-Terminkalender mit Synchronisation
related_brief: brief.md
created: 2026-07-02
updated: 2026-07-02
---

# Addendum

Ergänzendes Material aus der Discovery, das für die Architektur-/Implementierungsphase relevant ist, aber nicht in den Brief selbst gehört.

## Technischer Constraint: OAuth-Scope für Outlook/Google

Damit die Kontext-bewusste Verfügbarkeit (siehe Brief, Abschnitt "The Solution") auch für importierte Termine funktioniert, muss die App bei der OAuth-Registrierung vollen Lese-Zugriff auf die Kalender-API beantragen (volle Event-Objekte inkl. Titel, Teilnehmer, Dauer) — nicht nur auf die Free/Busy-API. Der "Privat-Default" ist damit bewusst eine Anzeige-Regel der Anwendung gegenüber anderen Nutzern, keine technische Zugriffsbeschränkung auf Datenebene. Das ist eine bewusste Entscheidung (siehe unten), die bei der OAuth-App-Registrierung entsprechend berücksichtigt werden muss.

## Verworfene Alternativen zum Privat-Default-Konflikt

Der Privat-Default (importierte Termine zeigen nur "privat/beschäftigt") und die live abgeleitete Kontext-Verfügbarkeit stehen in Spannung: Für importierte Termine gäbe es je nach Datenzugriffsmodell keine Grundlage, um Kontext abzuleiten. Drei Optionen wurden erwogen:

1. **Kontext-Ableitung nur für nativ im Tool erstellte Termine** — einfach umzusetzen, schwächt aber das Kernfeature genau für die Termine, die am meisten Kollisionen verursachen (importierte).
2. **Manuelles Kontext-Tag** unabhängig von der Quelle, statt automatischer Ableitung — kein Blackbox-Raten, aber kein "live"-Signal mehr, sondern eine bewusste Nutzeraktion.
3. **Gewählt:** Grobe Heuristik (Dauer/Tageszeit/Teilnehmerzahl) auch für importierte Termine, weil das Backend ohnehin vollen Zugriff auf die Kalender-Metadaten hat (volle Calendar-API statt nur Free/Busy) — konsistent mit der oben beschriebenen Einordnung des Privat-Default als Anzeige-Regel — begründet durch grundsätzliches Vertrauen im Team, da es sich um ein internes Tool handelt.

Falls diese Entscheidung später in Frage gestellt wird (z. B. bei Erweiterung auf ein Team mit weniger Grundvertrauen oder bei einem Wechsel zu einem Cloud-Hosting-Modell mit mehr Beteiligten), stehen die Alternativen 1 und 2 als geprüfte, aber verworfene Optionen zur Verfügung.
