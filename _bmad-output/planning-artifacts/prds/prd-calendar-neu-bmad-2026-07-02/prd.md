---
title: Team-Terminkalender mit Synchronisation
status: final
created: 2026-07-02
updated: 2026-07-02
---

# PRD: Team-Terminkalender mit Synchronisation

## 0. Dokumentzweck

Diese PRD richtet sich an Dennis selbst als Product Owner, Entwickler und Betreiber in Personalunion sowie an alle nachgelagerten Arbeitsschritte (UX, Architektur, Umsetzung), die auf ihr aufbauen. Sie baut direkt auf [brief-calendar-neu-bmad-2026-07-02](../../briefs/brief-calendar-neu-bmad-2026-07-02/brief.md) (Problem, Lösung, Zielgruppe, MoSCoW-Scope) und dessen [Addendum](../../briefs/brief-calendar-neu-bmad-2026-07-02/addendum.md) (OAuth-Scope-Constraint, verworfene Alternativen zum Privat-Default) auf und dupliziert deren Inhalt nicht, sondern verfeinert ihn auf Anforderungsebene. Begriffe folgen dem Glossar in Abschnitt 3; Funktionale Anforderungen sind global durchnummeriert (FR-1 … FR-N) für stabile Referenzen in späteren Artefakten. Inline-Markierungen `[ASSUMPTION]` kennzeichnen Stellen, an denen abgeleitet statt explizit bestätigt wurde.

## 1. Vision

Ein selbst gehostetes Team-Kalender-Tool für ein festes 5-10-köpfiges Team, das die verstreuten Outlook- und Google-Kalender aller Mitglieder in einer gemeinsamen Ansicht zusammenführt — so wie eine Unified Inbox mehrere E-Mail-Konten zusammenführt. Statt in fremden Kalendern nachzusehen oder Kollegen zu fragen "hast du gerade Zeit?", zeigt das Tool auf einen Blick, wer verfügbar ist — mit Kontext (unterbrechbar vs. bitte-nicht-stören), automatisch aus den Kalenderdaten abgeleitet.

Neue Termine entstehen ab sofort direkt im Tool; Outlook und Google bleiben per einseitigem Sync eingebunden, weil extern ausgelöste Termine (z. B. eine Kundeneinladung) immer zuerst dort entstehen — Externe haben keinen Zugriff auf das neue Tool. Erfolg für diesen ersten Prototyp: Das Tool wird täglich genutzt, statt Outlook direkt zu öffnen. Bewährt sich der Prototyp im Alltag, sind die naheliegenden nächsten Schritte die Should-Have-Features (Slot-Finder, Benachrichtigungen) und ggf. eine Ausweitung auf weitere Teams oder ein Signal außerhalb des Tools (z. B. ein Slack-Badge).

## 2. Zielgruppe

### 2.1 Jobs To Be Done

- Als Teammitglied will ich in Sekunden sehen, ob eine Kollegin oder ein Kollege gerade ansprechbar ist, ohne im Kalender nachzusehen oder nachzufragen.
- Als Teammitglied will ich einen Termin anlegen können, ohne vorher in mehreren Kalendern nach einem gemeinsamen freien Slot suchen zu müssen.
- Als Teammitglied will ich, dass meine privaten Termininhalte (Titel, Teilnehmer) vor Kollegen verborgen bleiben, auch wenn das Tool sie technisch liest.
- Als Dennis (Betreiber) will ich ein Tool, das sich in den Alltag einfügt, ohne dass jemand seine Gewohnheiten aktiv ändern muss, außer neue Termine künftig im Tool statt in Outlook anzulegen.

### 2.2 Nicht-Nutzer (v1)

- Externe Personen (Kunden, Partner) — sie erscheinen nur als Teilnehmer in importierten Terminen, haben aber keinen eigenen Zugang zum Tool.
- Weitere Teams oder Projektgruppen außerhalb des einen festen 5-10-Personen-Teams (siehe Non-Goals).

### 2.3 Zentrale Nutzungsmomente

- **UJ-1. Mara prüft, ob sie einen Kollegen jetzt ansprechen kann.**
  Mara, Teammitglied, sitzt an ihrem Rechner und braucht kurz eine Rückmeldung von einem Kollegen. Statt seinen Kalender zu öffnen oder ihn per Chat zu fragen, öffnet sie das Tool (bereits eingeloggt), sieht in der Tagesansicht seine Spalte neben ihrer eigenen und erkennt sofort den abgeleiteten Status: "unterbrechbar" oder "bitte-nicht-stören", ohne den Termintitel zu sehen. Ist er unterbrechbar, schreibt sie ihm direkt; ist er im Status "bitte-nicht-stören", wartet sie, ohne nachfragen zu müssen. **Edge Case:** Zeigt der Status "bitte nicht stören", obwohl der Kollege gerade tatsächlich ansprechbar ist, kann er seinen Status manuell übersteuern (FR-11).

## 3. Glossar

- **Teammitglied** — Eine der 5-10 festen Personen im Team, jede mit eigenem Outlook- oder Google-Konto, das ins Tool synchronisiert wird.
- **Nativer Termin** — Ein Termin, der direkt im Tool angelegt wurde. Existiert ausschließlich in der Tool-Datenbank, nicht in Outlook/Google.
- **Synchronisierter Termin** — Ein Termin, der per Import-Sync aus Outlook oder Google ins Tool übernommen wurde. Kann im Notfall erneut aus der Quelle geladen werden.
- **Einseitiger Sync** — Regelmäßiger Import (alle paar Minuten) von Outlook- und Google-Kalenderdaten ins Tool. Kein Zurückschreiben in die Quellsysteme.
- **Privat-Default** — Die Anzeigeregel, dass andere Teammitglieder bei jedem Termin (nativ oder synchronisiert) standardmäßig nur "privat/beschäftigt" plus den abgeleiteten Verfügbarkeits-Status sehen, nie den echten Titel oder Inhalt. Das Tool selbst liest im Hintergrund die vollen Termindaten (Titel, Teilnehmer, Dauer); der Privat-Default ist eine Anzeigeregel, keine Datenzugriffsgrenze (siehe Addendum).
- **Verfügbarkeits-Status** — Der pro Termin abgeleitete oder manuell gesetzte Kontext-Wert: **unterbrechbar** oder **bitte-nicht-stören**. Ersetzt binäres Frei/Beschäftigt für alle Termine, nativ wie synchronisiert.
- **Status-Heuristik** — Die Regel, die aus Dauer, Teilnehmerzahl und (nachrangig) Tageszeit eines Termins automatisch den Verfügbarkeits-Status ableitet (siehe FR-10).
- **Status-Override** — Die manuelle Übersteuerung des abgeleiteten Verfügbarkeits-Status durch das betroffene Teammitglied selbst.
- **Mehrpersonen-Ansicht** — Die Darstellung mehrerer ausgewählter Teammitglieder-Kalender nebeneinander in Spalten, verfügbar in Tages- und Wochenansicht.

## 4. Features

### 4.1 Kalenderansichten

**Beschreibung:** Monats-, Wochen- und Tagesansicht wie ein gewohnter Kalender, für den eigenen Kalender sowie als Mehrpersonen-Ansicht für ausgewählte Teammitglieder nebeneinander. Realisiert UJ-1.

#### FR-1: Eigene Kalenderansicht
Ein Teammitglied kann seinen eigenen Kalender in Monats-, Wochen- und Tagesansicht anzeigen.

**Consequences (testable):**
- Alle drei Ansichten zeigen sowohl native als auch synchronisierte Termine des eingeloggten Nutzers.
- Wechsel zwischen den drei Ansichten ist ohne Neuladen der Seite möglich.

#### FR-2: Mehrpersonen-Ansicht
Ein Teammitglied kann eine Teilmenge der übrigen Teammitglieder auswählen und deren Kalender nebeneinander in Spalten sehen — in Tages- und Wochenansicht gleichermaßen. Realisiert UJ-1.

**Consequences (testable):**
- Vor der Mehrpersonen-Ansicht wählt der Nutzer die anzuzeigenden Personen explizit aus (kein automatisches Anzeigen aller 5-10 Personen gleichzeitig).
- Jede Spalte zeigt für fremde Termine ausschließlich "privat/beschäftigt" + Verfügbarkeits-Status (Privat-Default, FR-9), nie den echten Titel.
- Die Auswahl bleibt bestehen, wenn zwischen Tages- und Wochenansicht gewechselt wird.

**Out of Scope:**
- Eine überlagerte ("übereinandergelegte") Darstellung mehrerer Kalender in einer einzigen Spalte ist nicht Teil dieser Anforderung — Personen werden nebeneinander, nicht überlagert dargestellt.

### 4.2 Termine anlegen und einsehen

**Beschreibung:** Eigene Termine werden ab jetzt direkt im Tool angelegt; ein Klick auf einen Termin öffnet die Detailansicht.

#### FR-3: Termin anlegen
Ein Teammitglied kann einen neuen (nativen) Termin mit Titel, Datum/Uhrzeit, Dauer und optionalen Teilnehmern anlegen.

**Consequences (testable):**
- Ein neu angelegter nativer Termin erscheint sofort in der eigenen Kalenderansicht.
- Nativen Terminen wird automatisch ein Verfügbarkeits-Status nach der Status-Heuristik (FR-10) zugewiesen.

#### FR-4: Termin-Detailansicht
Ein Teammitglied kann per Klick auf einen eigenen Termin dessen volle Details öffnen (Titel, Zeit, Teilnehmer).

**Consequences (testable):**
- Die Detailansicht zeigt bei eigenen Terminen immer die vollen Daten, unabhängig vom Privat-Default (der nur für die Ansicht durch andere gilt).
- Bei fremden Terminen zeigt ein Klick nur "privat/beschäftigt" + Verfügbarkeits-Status, keine Detaildaten — **außer**, wenn das Teammitglied selbst als Teilnehmer eingetragen ist (siehe FR-9, Ausnahme).

**Out of Scope:**
- Eine Ortsanzeige ist erst mit FR-15 (Should-Have) Teil der Detailansicht. Für MVP-Termine (FR-3) gibt es kein Ort-Feld, die Detailansicht zeigt entsprechend keinen Ort.

### 4.3 Kalender-Synchronisation

**Beschreibung:** Einseitiger Import-Sync aus Outlook und Google Calendar, alle paar Minuten aktualisiert, damit extern ausgelöste Termine (z. B. Kundeneinladungen) ins Tool gelangen.

#### FR-5: Outlook-Import
Das System importiert regelmäßig alle Kalendertermine eines verbundenen Outlook-Kontos.

**Consequences (testable):**
- Ein neuer oder geänderter Termin in Outlook erscheint spätestens nach dem nächsten Sync-Zyklus (Ziel: wenige Minuten) im Tool.
- Der Import umfasst volle Termindaten (Titel, Teilnehmer, Dauer) für die interne Verarbeitung — unabhängig vom Privat-Default, der nur die Anzeige für andere betrifft.
- Jeder importierte Termin wird anhand einer stabilen, providerseitigen Event-ID identifiziert. Eine Verschiebung oder inhaltliche Änderung am selben Termin aktualisiert den bestehenden Eintrag, statt einen zusätzlichen Termin anzulegen.
- Wird ein Termin an der Quelle gelöscht oder abgesagt, wird der entsprechende Eintrag spätestens im nächsten Sync-Zyklus aus dem Tool entfernt (inkl. eines eventuell davon abgeleiteten Verfügbarkeits-Status).

**Out of Scope:**
- Dieselbe Person mit zwei parallel verbundenen Konten (z. B. privates Google- und dienstliches Outlook-Konto) ist nicht abgedeckt — siehe Assumptions-Index.
- Ausnahmen innerhalb importierter Serientermine (einzelne verschobene/abgesagte Instanzen) sind nicht spezifiziert.

#### FR-6: Google-Calendar-Import
Das System importiert regelmäßig alle Kalendertermine eines verbundenen Google-Kontos, analog zu FR-5 (inkl. Dedup-/Lösch-Verhalten über Provider-Event-ID).

**Consequences (testable):**
- Gleiche Aktualisierungsfrequenz, Datenumfang und Dedup-/Lösch-Verhalten wie FR-5.

#### FR-7: Kein Zurückschreiben
Das System schreibt zu keinem Zeitpunkt Termindaten zurück nach Outlook oder Google.

**Consequences (testable):**
- Ein im Tool angelegter, geänderter oder gelöschter nativer Termin erzeugt keine Schreiboperation gegen die Outlook- oder Google-API.

**Feature-spezifische NFRs:**
- Polling statt Webhook, sowie die Google-/Microsoft-Scope- und Consent-Anforderungen: siehe Abschnitt 8 (Integrations-Constraints) für Details und Begründung.

### 4.4 Privat-Default und Sichtbarkeit

**Beschreibung:** Die Trennung zwischen dem, was das Tool intern liest, und dem, was es anderen Teammitgliedern zeigt, ist die Grundlage des Vertrauens in das Tool.

#### FR-8: Interner Vollzugriff
Das System liest und speichert für jeden Termin (nativ wie synchronisiert) die vollen Daten: Titel, Teilnehmer, Dauer, Ort.

**Consequences (testable):**
- Volle Termindaten sind Voraussetzung für die Status-Heuristik (FR-10) und stehen dem Termin-Eigentümer in der eigenen Detailansicht zur Verfügung (FR-4).

#### FR-9: Privat-Default gegenüber anderen
Für jeden Termin, der nicht dem eigenen Account gehört, zeigt das System anderen Teammitgliedern ausschließlich "privat/beschäftigt" plus den Verfügbarkeits-Status — nie Titel, Teilnehmer oder Ort. **Ausnahme:** Ist das anfragende Teammitglied selbst als Teilnehmer in einem fremden nativen Termin eingetragen, sieht es dessen volle Details, auch ohne Eigentümer zu sein.

**Consequences (testable):**
- Kein UI-Pfad (Kalenderansicht, Mehrpersonen-Ansicht, Slot-Finder) zeigt einem Teammitglied den echten Titel oder Teilnehmer eines fremden Termins, bei dem es weder Eigentümer noch eingetragener Teilnehmer ist.
- Diese Regel gilt unabhängig davon, ob der Termin nativ oder synchronisiert ist.
- Die Filterung nach Eigentümerschaft/Teilnahme erfolgt serverseitig pro Anfrage anhand der Identität des anfragenden Nutzers — die API liefert für einen fremden Termin ohne eigene Teilnahme zu keinem Zeitpunkt die vollen Felder (Titel, Teilnehmer, Ort) aus, unabhängig davon, was das Frontend anzeigt. Eine rein clientseitige Ausblendung erfüllt diese Anforderung nicht.

**Notes:** Namen/E-Mail-Adressen externer Teilnehmer aus importierten Kundenterminen landen dadurch im Backend, ohne separate Schutzmaßnahme über die bestehende Privat-Default-Regel hinaus — bewusst akzeptiertes Risiko auf Basis des grundsätzlichen Vertrauens im Team, siehe Abschnitt 8.

### 4.5 Kontext-bewusste Verfügbarkeit

**Beschreibung:** Das Kernstück des Tools: statt binärem Frei/Beschäftigt zeigt jeder Termin einen abgeleiteten Verfügbarkeits-Status, der Kollegen hilft einzuschätzen, ob eine Unterbrechung angemessen ist. Realisiert UJ-1.

#### FR-10: Automatische Status-Ableitung
Das System leitet für jeden Termin (nativ wie synchronisiert) automatisch einen Verfügbarkeits-Status ab, basierend auf Dauer, Teilnehmerzahl und Tageszeit.

**Consequences (testable):**
- Termine ganz ohne weitere Teilnehmer gelten **immer** als unterbrechbar, unabhängig von ihrer Dauer (z. B. selbst geblockte Fokuszeit oder ein ganztägiger Solo-Block — kippt zu keinem Zeitpunkt auf bitte-nicht-stören). Diese Regel hat Vorrang vor allen anderen Regeln dieser FR.
- Kurze Termine mit mindestens einem Teilnehmer (`[ASSUMPTION]`: Richtwert bis ca. 30-45 Minuten) gelten unabhängig von Teilnehmerzahl und Tageszeit als **unterbrechbar**.
- Lange Termine mit mindestens einem Teilnehmer (`[ASSUMPTION]`: Richtwert ab ca. 1,5-2 Stunden mit 3+ Teilnehmern, sowie ganztägige Termine mit mindestens einem Teilnehmer) gelten als **bitte-nicht-stören**.
- Tageszeit ist nachrangig und beeinflusst die Einstufung in der Praxis kaum — ein kurzer Termin außerhalb der Kernarbeitszeit bleibt unterbrechbar.
- Die Status-Ableitung läuft mit derselben Sync-Verzögerung wie der übrige Kalender (wenige Minuten), kein Echtzeit-Anspruch.
- Hat ein Teammitglied zum aktuellen Zeitpunkt keinen laufenden Termin, gilt es standardmäßig als **unterbrechbar** (konsistent mit der Kein-Teilnehmer-Regel: kein Termin ist kein Anlass für bitte-nicht-stören).

**Out of Scope:**
- Exakte numerische Schwellenwerte sind hier als Richtwerte markiert, nicht als feste Spezifikation. Der Bereich zwischen "kurz" und "lang" (ca. 45 Minuten bis 1,5 Stunden — in der Praxis die häufigste Meeting-Dauer) ist absichtlich nicht abschließend spezifiziert; die endgültige Kalibrierung dieses Übergangsbereichs erfolgt in Architektur/Umsetzung (siehe Assumptions-Index, Offene Fragen).
- Nachträgliche Teilnehmer-Änderungen an einem bereits laufenden/eingestuften Termin sowie sich überschneidende Termine mit unterschiedlichem Status lösen keine spezifizierte Neubewertung aus — Verhalten in diesen Fällen ist architekturseitig zu definieren.

#### FR-11: Manueller Status-Override
Ein Teammitglied kann seinen eigenen, aktuell angezeigten Verfügbarkeits-Status jederzeit manuell übersteuern. Der Override bezieht sich auf den Nutzer als Ganzes (aktueller Status "gerade jetzt"), nicht auf eine einzelne Termininstanz.

**Consequences (testable):**
- Ein manuell gesetzter Status bleibt bestehen, bis der Nutzer ihn aktiv zurücksetzt oder ändert — er wird nicht beim nächsten Sync-Zyklus automatisch von der Heuristik überschrieben, und er endet nicht automatisch mit dem Ende des Termins, der zum Zeitpunkt der Übersteuerung aktiv war.
- Andere Teammitglieder sehen keinen Unterschied zwischen automatisch abgeleitetem und manuell gesetztem Status (gleiche Darstellung).

**Out of Scope:**
- Die Interaktion mit Serienterminen (FR-14, nicht MVP) ist nicht spezifiziert — ob ein Override eine einzelne Instanz oder die ganze Serie betrifft, wird bei Umsetzung von FR-14 festgelegt.

### 4.6 Should-Have-Features

**Beschreibung:** Funktionen, die den Kernnutzen erweitern, aber für den ersten Prototyp nicht zwingend erforderlich sind. Werden nach MVP-Validierung priorisiert (siehe Abschnitt 6.2).

#### FR-12: Gemeinsamer Slot-Finder
Ein Teammitglied kann mehrere Teammitglieder auswählen; das System schlägt automatisch gemeinsame freie Zeitfenster vor.

**Consequences (testable):**
- Der Slot-Finder berücksichtigt ausschließlich reines Frei/Beschäftigt der ausgewählten Personen, nicht den abgeleiteten Verfügbarkeits-Status — ein technisch freier, aber als "bitte-nicht-stören" markierter Slot wird trotzdem vorgeschlagen.

#### FR-13: Termin-Erinnerungen
Das System kann ein Teammitglied an einen bevorstehenden eigenen Termin erinnern.

**Consequences (testable):**
- Eine Erinnerung wird für einen eigenen Termin ausgelöst, bevor dieser beginnt (konkreter Vorlauf architekturseitig festzulegen, z. B. konfigurierbar oder fester Standardwert).
- Erinnerungen beziehen sich ausschließlich auf eigene Termine (nativ oder synchronisiert), nie auf fremde.

**Out of Scope:**
- Ein Hinweis, sobald ein wartender Kollege frei wird, ist explizit **nicht** Teil dieser Anforderung — das Sync-Intervall von mehreren Minuten macht ein solches Signal zu unzuverlässig, um nützlich zu sein.

#### FR-14: Serientermine
Ein Teammitglied kann wiederkehrende native Termine anlegen (täglich/wöchentlich/monatlich).

**Consequences (testable):**
- Jede erzeugte Instanz einer Serie erscheint einzeln in Kalenderansicht und Mehrpersonen-Ansicht und erhält einzeln einen Verfügbarkeits-Status nach FR-10.

**Notes:** Die Bearbeitungs-/Löschgranularität (nur diese Instanz / diese und folgende / ganze Serie) sowie die Interaktion mit dem Status-Override (FR-11) sind bei Umsetzung dieser Funktion festzulegen.

#### FR-15: Ortsangabe mit Kartenanzeige
Ein Teammitglied kann einem nativen Termin einen Ort mit Autovervollständigung hinzufügen; die Detailansicht zeigt eine Kartendarstellung.

**Consequences (testable):**
- Ein Termin ohne angegebenen Ort zeigt in der Detailansicht keine Kartendarstellung (kein Pflichtfeld).
- Die Ortsangabe unterliegt demselben Privat-Default wie andere Termindetails (FR-9) — bei fremden Terminen ohne Teilnahme nicht sichtbar.

## 5. Non-Goals (Explizit)

- Kein delegierter/stellvertretender Zugriff — niemand legt Termine im Namen eines anderen Teammitglieds an.
- Keine Unterstützung mehrerer Teams oder Projektgruppen — das Tool ist für genau ein festes Team gebaut.
- Kein Echtzeit-Sync — Aktualisierung alle paar Minuten ist explizit ausreichend.
- Kein Zurückschreiben nach Outlook/Google — Sync bleibt dauerhaft einseitig (siehe FR-7).
- Keine aktive Kollisionswarnung beim Anlegen eines Termins — die gemeinsame Sichtbarkeit macht Kollisionen sichtbar, das Tool warnt aber nicht proaktiv davor.
- Kein Mechanismus, der erzwingt oder durch Anreize fördert, dass Termine tatsächlich im Tool statt weiter in Outlook/Google angelegt werden — das Fragmentierungsrisiko wird als Verhaltensrisiko akzeptiert, nicht durch ein Feature adressiert (operationalisiert als Counter-Metric SM-C1, Abschnitt 9).
- Kein Benachrichtigungs- oder Einladungsversand an externe Teilnehmer eines nativen Termins — extern eingetragene Personen (z. B. Kunden) erfahren von einem im Tool angelegten Termin nur auf anderem Weg (z. B. mündlich, separate E-Mail), nicht durch das Tool selbst.
- Keine Bearbeitung synchronisierter (nicht-nativer) Termine im Tool — sie sind nur lesend dargestellt; jede Änderung an einem Termin mit Outlook/Google-Ursprung erfolgt weiterhin in der jeweiligen Quelle.
- Kein spezifizierter Deprovisionierungs-Workflow für ausscheidende Teammitglieder (Datenverbleib, Entzug der OAuth-Verbindung) in dieser ersten Runde — siehe Offene Fragen.

## 6. MVP-Scope

### 6.1 In Scope (Must Have)
- Kalenderansichten: Monat, Woche, Tag, inkl. Mehrpersonen-Ansicht mit Personenauswahl (FR-1, FR-2)
- Termine anlegen und per Klick einsehen (FR-3, FR-4)
- Einseitiger Sync (Import) aus Outlook und Google, alle paar Minuten (FR-5, FR-6, FR-7)
- Privat-Default für alle Termine gegenüber anderen Teammitgliedern (FR-8, FR-9)
- Kontext-bewusste Verfügbarkeit mit Status-Heuristik und manuellem Override (FR-10, FR-11)
- Mobil/responsive Nutzung
- Selbst gehostet
- Backup/Wiederherstellungsfähigkeit für native Termine (NFR, Abschnitt 7)

### 6.2 Out of Scope für MVP
- **Slot-Finder** (FR-12) — naheliegender nächster Schritt nach MVP-Validierung.
- **Termin-Erinnerungen** (FR-13) — Should-Have, nicht blockierend für den Kernnutzen.
- **Serientermine** (FR-14) — Should-Have.
- **Ortsangabe mit Karte** (FR-15) — Should-Have.
- **Farbcodierung nach Quelle** (Outlook/Google/manuell) — Could-Have.
- **Notizfeld pro Termin** für team-interne Infos — Could-Have.
- **Schnellsuche** über Termine/Orte/Personen — Could-Have.
- **Anfahrtszeit-Schätzung** mit Warnung bei knapper Taktung — Could-Have.
- **Abwesenheiten/Urlaub** als eigene Kalender-Kategorie — Could-Have.

*Priorisierungshinweis: Slot-Finder und Serientermine sind laut Brief-Diskussion die beiden Should-Haves mit dem größten Alltagsnutzen — bei begrenzter Umsetzungszeit zuerst prüfen.*

## 7. Cross-Cutting NFRs

- **Sync-Aktualität:** Alle Sync-Zyklen (Outlook, Google, Status-Ableitung) laufen im Bereich weniger Minuten. Kein Echtzeit-Anspruch — validiert gegen SM-2.
- **Sync-Transparenz (Must-Have):** Die Oberfläche zeigt erkennbar an, wann ein Kalender zuletzt erfolgreich synchronisiert wurde. Bleibt ein Sync-Zyklus für ein Konto wiederholt aus (z. B. wegen abgelaufenem/widerrufenem Token), muss das für den Betreiber sichtbar werden — ein stiller, unbemerkter Sync-Ausfall ist bei Bus-Faktor-1-Betrieb sonst nicht erkennbar.
- **Datenhaltung nativer Termine (Must-Have):** Native Termine existieren ausschließlich in der Tool-Datenbank und sind — anders als synchronisierte Termine, die im Notfall erneut aus Outlook/Google geladen werden können — bei Datenverlust unwiederbringlich. Das System muss daher eine grundlegende Backup-/Wiederherstellungsfähigkeit für native Termine bereitstellen. Konkrete Umsetzung (Frequenz, Aufbewahrungsdauer, Restore-Prozess) folgt in der Architekturphase.
- **Zugriffsschutz (Must-Have):** Zugriff auf das Tool erfordert eine Anmeldung (Mechanismus in der Architekturphase festzulegen) — das Tool ist kein anonym erreichbares System, da es die vollen Kalenderdaten aller Teammitglieder aggregiert. Gespeicherte OAuth-Tokens (Zugriff auf Outlook/Google jedes Teammitglieds) müssen verschlüsselt abgelegt werden — ein Datenbankzugriff allein darf keinen direkten Zugriff auf die verbundenen Kalenderkonten ermöglichen.
- **Mobil/Responsiv:** Alle Kernfunktionen (Ansichten, Termin anlegen, Verfügbarkeits-Status, Detailansicht) sind auf mobilen Endgeräten per Browser nutzbar. Für die Mehrpersonen-Ansicht (FR-2) ist das konkrete Verhalten bei vielen ausgewählten Spalten auf kleinen Bildschirmen (horizontales Scrollen vs. Obergrenze der Auswahl) architekturseitig zu lösen.
- **Betrieb:** Selbst gehostet, ein Betreiber (Bus-Faktor 1) für Wartung und OAuth-Token-Pflege — bewusst akzeptiertes Betriebsrisiko für diesen Prototyp, kein Anspruch auf Hochverfügbarkeit oder definierte Recovery-Zeiten über die native-Termine-Backup-Anforderung hinaus.

## 8. Constraints und Guardrails

**Privacy**
- Privat-Default ist eine Anzeigeregel, keine Datenzugriffsgrenze; externe Teilnehmerdaten werden ohne zusätzliche Schutzmaßnahme gespeichert (akzeptiertes Risiko) — Details siehe Glossar (Privat-Default) und FR-9 Notes.

**Integrations-Constraints (Outlook/Google)**
- Volle Kalender-API statt nur Frei/Busy-API ist technisch zwingend, damit die Status-Heuristik (FR-10) auch für synchronisierte Termine funktioniert.
- Google: sensibler Scope `calendar.readonly` — als Internal-App innerhalb einer Google-Workspace-Organisation ohne OAuth-Verifizierung/CASA-Prüfung nutzbar. `[ASSUMPTION]`: Team nutzt ein gemeinsames Workspace.
- Microsoft Graph: `Calendars.Read` benötigt je nach Tenant-Konfiguration Admin-Consent; Tenant-Admins können Self-Consent organisationsweit sperren — externe Abhängigkeit, siehe Offene Fragen.
- Beide Provider garantieren keine zuverlässige Echtzeit-Zustellung von Änderungen (Google-Push-Kanäle laufen ab, Microsoft-Graph-Benachrichtigungen ohne Zeitgarantie) — das gewählte Polling-Intervall von wenigen Minuten (FR-5, FR-6) umgeht dieses Problem, statt es zu lösen.

## 9. Success Metrics

**Primary**
- **SM-1**: Tägliche aktive Nutzung — das Tool wird im Alltag geöffnet, um Verfügbarkeit zu prüfen, statt Outlook direkt zu öffnen. Validiert FR-1, FR-2, FR-10.

**Secondary**
- **SM-2**: Wahrgenommene Zeitersparnis beim Verfügbarkeits-Check — spürbar kürzer als vorheriges Nachfragen/Kalender-Wechseln. Validiert FR-10, FR-11.
- **SM-3**: Häufigkeit von Doppelbuchungen sinkt, weil Kollisionen durch gemeinsame Sichtbarkeit vor dem Anlegen auffallen (keine aktive Warnung, siehe Non-Goals). Validiert FR-1, FR-2, FR-9.

**Counter-metrics (nicht optimieren)**
- **SM-C1**: Anteil der Termine, die trotz täglicher Tool-Nutzung weiterhin extern in Outlook/Google statt im Tool angelegt werden. Hohe Nutzung bei gleichzeitig hohem Anteil zeigt, dass das Tool nur zum Lesen dient, aber keinen Verhaltenswechsel beim Anlegen bewirkt — genau das Fragmentierungsrisiko aus Abschnitt 5/8. Balanciert SM-1.

## 10. Offene Fragen

1. Bestätigen, ob das Team ein gemeinsames Google-Workspace nutzt (Voraussetzung für den Internal-App-Typ ohne OAuth-Verifizierung) — sonst wird eine zusätzliche Verifizierungsphase bei Google nötig.
2. Klären, ob die Microsoft-Tenant-Konfiguration der Teammitglieder Admin-Consent für `Calendars.Read` zulässt bzw. ob Self-Consent organisationsweit gesperrt ist — betrifft die Onboarding-Fähigkeit neuer Teammitglieder.
3. Exakte numerische Schwellenwerte der Status-Heuristik (FR-10) sind als Richtwerte gesetzt, nicht final kalibriert, insbesondere der Übergangsbereich ca. 45 Min. bis 1,5 Std. — Feinjustierung erfolgt in der Architektur-/Umsetzungsphase, ggf. nach erster Nutzung nachjustieren.
4. Konkrete Backup-Frequenz und Restore-Prozess für native Termine (Abschnitt 7) sind noch nicht spezifiziert — Teil der Architekturphase.
5. Deprovisionierung ausscheidender Teammitglieder: Bleiben ihre nativen Termine und ihre Teilnahme an fremden Terminen erhalten, oder werden sie entfernt? Wird ihr OAuth-Zugriff aktiv widerrufen? Verschwinden sie aus der Personenauswahl (FR-2)? Owner: Dennis. Zu klären, sobald ein erster Team-Wechsel ansteht oder spätestens in der Architekturphase.
6. Widerruft ein ausgewähltes Teammitglied seinen Kalenderzugriff (OAuth) oder bricht die Verbindung technisch ab: Soll das Tool das für Betrachter erkennbar machen (z. B. "Daten evtl. veraltet"), oder reicht die allgemeine Sync-Transparenz-Anforderung (Abschnitt 7)?
7. Zeitzonen: Diese PRD geht von einem Team in derselben Zeitzone aus (siehe Assumptions-Index) — trifft das zu, oder braucht die Mehrpersonen-Ansicht (FR-2) eine explizite Zeitzonen-Achse?
8. Externe Teilnehmerdaten (Abschnitt 4.4/8) sind aktuell ohne Aufbewahrungs- oder Löschregel akzeptiert. Owner: Dennis. Revisit-Anlass: sobald ein Kundentermin mit sensiblen externen Daten regelmäßig vorkommt, ein Teammitglied mit weniger Grundvertrauen hinzukommt, oder ein Cloud-Hosting-Modell mit mehr Beteiligten in Betracht gezogen wird (vgl. Brief-Addendum) — dann mindestens eine Löschregel (z. B. "externe Teilnehmerdaten verfallen mit dem Quelltermin") ergänzen.

## 11. Assumptions-Index

- Aus Abschnitt 4.3/8: Das Team nutzt ein gemeinsames Google-Workspace, sodass der Internal-App-Typ ohne OAuth-Verifizierung/CASA-Prüfung anwendbar ist.
- Aus FR-10: Numerische Richtwerte der Status-Heuristik (~30-45 Min. Grenze für "kurz", ~1,5-2h + 3+ Teilnehmer für "bitte-nicht-stören") sind illustrativ, nicht final spezifiziert.
- Aus FR-2/Glossar: Alle Teammitglieder befinden sich in derselben Zeitzone — die Mehrpersonen-Ansicht spezifiziert keine Zeitzonen-Achse für ein geografisch verteiltes Team.
- Aus Glossar "Teammitglied": Jede Person nutzt genau ein Kalenderkonto (Outlook oder Google), nicht beide parallel (z. B. privates Google- + dienstliches Outlook-Konto).
- Aus FR-5/FR-6: Zeitumstellungen (Sommer-/Winterzeit) werden von der zugrundeliegenden Kalender-Infrastruktur der Provider korrekt gehandhabt; kein eigenes PRD-Requirement dazu.
