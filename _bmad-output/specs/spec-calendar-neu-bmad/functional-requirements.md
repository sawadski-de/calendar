# Functional Requirements — Team-Terminkalender mit Synchronisation

Companion zu SPEC.md. FR-Nummerierung ist stabil (referenziert von Architektur-AD-Regeln und UX) — nicht verändern oder neu vergeben. Must-Have FRs (FR-1…FR-11 plus die drei Cross-Cutting-NFRs) realisieren die MVP-Capabilities CAP-1…CAP-9 in SPEC.md; Should-Have FRs (FR-12…FR-15) sind laut SPEC.md Non-Goals auf nach dem MVP verschoben, aber hier zur Preservation vollständig erhalten.

## Must-Have (MVP)

### FR-1 — Eigene Kalenderansicht (CAP-1)
Ein Teammitglied kann seinen eigenen Kalender in Monats-, Wochen- und Tagesansicht anzeigen.
- Alle drei Ansichten zeigen sowohl native als auch synchronisierte Termine des eingeloggten Nutzers.
- Wechsel zwischen den drei Ansichten ist ohne Neuladen der Seite möglich.

### FR-2 — Mehrpersonen-Ansicht (CAP-2)
Ein Teammitglied kann eine Teilmenge der übrigen Teammitglieder auswählen und deren Kalender nebeneinander in Spalten sehen — in Tages- und Wochenansicht gleichermaßen.
- Vor der Mehrpersonen-Ansicht wählt der Nutzer die anzuzeigenden Personen explizit aus (kein automatisches Anzeigen aller 5-10 Personen gleichzeitig).
- Jede Spalte zeigt für fremde Termine ausschließlich "privat/beschäftigt" + Verfügbarkeits-Status (FR-9), nie den echten Titel.
- Die Auswahl bleibt bestehen, wenn zwischen Tages- und Wochenansicht gewechselt wird.
- Out of Scope: eine überlagerte Darstellung mehrerer Kalender in einer einzigen Spalte — Personen werden nebeneinander, nicht überlagert dargestellt.
- Erweiterung (UX-Entscheidung, siehe SPEC.md CAP-2): die Mehrpersonen-Ansicht gilt zusätzlich für die Monatsansicht (Aggregat-Marker + Detail-Popover statt Spalten), über den hier zitierten FR-2-Wortlaut hinaus.

### FR-3 — Termin anlegen (CAP-3)
Ein Teammitglied kann einen neuen (nativen) Termin mit Titel, Datum/Uhrzeit, Dauer und optionalen Teilnehmern anlegen.
- Ein neu angelegter nativer Termin erscheint sofort in der eigenen Kalenderansicht.
- Nativen Terminen wird automatisch ein Verfügbarkeits-Status nach der Status-Heuristik (FR-10) zugewiesen.

### FR-4 — Termin-Detailansicht (CAP-3)
Ein Teammitglied kann per Klick auf einen eigenen Termin dessen volle Details öffnen (Titel, Zeit, Teilnehmer).
- Die Detailansicht zeigt bei eigenen Terminen immer die vollen Daten, unabhängig vom Privat-Default (der nur für die Ansicht durch andere gilt).
- Bei fremden Terminen zeigt ein Klick nur "privat/beschäftigt" + Verfügbarkeits-Status, keine Detaildaten — außer wenn das Teammitglied selbst als Teilnehmer eingetragen ist (FR-9-Ausnahme).
- Out of Scope: Ortsanzeige ist erst mit FR-15 Teil der Detailansicht; für MVP-Termine gibt es kein Ort-Feld.

### FR-5 — Outlook-Import (CAP-4)
Das System importiert regelmäßig alle Kalendertermine eines verbundenen Outlook-Kontos.
- Ein neuer oder geänderter Termin in Outlook erscheint spätestens nach dem nächsten Sync-Zyklus (Ziel: wenige Minuten) im Tool.
- Der Import umfasst volle Termindaten (Titel, Teilnehmer, Dauer) für die interne Verarbeitung — unabhängig vom Privat-Default, der nur die Anzeige für andere betrifft.
- Jeder importierte Termin wird anhand einer stabilen, providerseitigen Event-ID identifiziert. Eine Verschiebung oder inhaltliche Änderung am selben Termin aktualisiert den bestehenden Eintrag, statt einen zusätzlichen Termin anzulegen.
- Wird ein Termin an der Quelle gelöscht oder abgesagt, wird der entsprechende Eintrag spätestens im nächsten Sync-Zyklus aus dem Tool entfernt (inkl. eines eventuell davon abgeleiteten Verfügbarkeits-Status).
- Out of Scope: dieselbe Person mit zwei parallel verbundenen Konten; Ausnahmen innerhalb importierter Serientermine (einzelne verschobene/abgesagte Instanzen) sind nicht spezifiziert.

### FR-6 — Google-Calendar-Import (CAP-4)
Das System importiert regelmäßig alle Kalendertermine eines verbundenen Google-Kontos, analog zu FR-5 (inkl. Dedup-/Lösch-Verhalten über Provider-Event-ID). Gleiche Aktualisierungsfrequenz, Datenumfang und Dedup-/Lösch-Verhalten wie FR-5.

### FR-7 — Kein Zurückschreiben (CAP-4)
Das System schreibt zu keinem Zeitpunkt Termindaten zurück nach Outlook oder Google.
- Ein im Tool angelegter, geänderter oder gelöschter nativer Termin erzeugt keine Schreiboperation gegen die Outlook- oder Google-API.

### FR-8 — Interner Vollzugriff (CAP-5)
Das System liest und speichert für jeden Termin (nativ wie synchronisiert) die vollen Daten: Titel, Teilnehmer, Dauer, Ort.
- Volle Termindaten sind Voraussetzung für die Status-Heuristik (FR-10) und stehen dem Termin-Eigentümer in der eigenen Detailansicht zur Verfügung (FR-4).

### FR-9 — Privat-Default gegenüber anderen (CAP-5)
Für jeden Termin, der nicht dem eigenen Account gehört, zeigt das System anderen Teammitgliedern ausschließlich "privat/beschäftigt" plus den Verfügbarkeits-Status — nie Titel, Teilnehmer oder Ort. Ausnahme: Ist das anfragende Teammitglied selbst als Teilnehmer in einem fremden nativen Termin eingetragen, sieht es dessen volle Details, auch ohne Eigentümer zu sein.
- Kein UI-Pfad (Kalenderansicht, Mehrpersonen-Ansicht, künftiger Slot-Finder) zeigt einem Teammitglied den echten Titel oder Teilnehmer eines fremden Termins, bei dem es weder Eigentümer noch eingetragener Teilnehmer ist.
- Diese Regel gilt unabhängig davon, ob der Termin nativ oder synchronisiert ist.
- Die Filterung nach Eigentümerschaft/Teilnahme erfolgt serverseitig pro Anfrage anhand der Identität des anfragenden Nutzers — die API liefert für einen fremden Termin ohne eigene Teilnahme zu keinem Zeitpunkt die vollen Felder aus. Eine rein clientseitige Ausblendung erfüllt diese Anforderung nicht.
- Notiz: Namen/E-Mail-Adressen externer Teilnehmer aus importierten Kundenterminen landen dadurch im Backend, ohne separate Schutzmaßnahme über diese Regel hinaus — akzeptiertes Risiko (siehe SPEC.md Open Questions).

### FR-10 — Automatische Status-Ableitung (CAP-6)
Das System leitet für jeden Termin (nativ wie synchronisiert) automatisch einen Verfügbarkeits-Status ab, basierend auf Dauer, Teilnehmerzahl und Tageszeit.
- Termine ganz ohne weitere Teilnehmer gelten **immer** als unterbrechbar, unabhängig von ihrer Dauer — Vorrang vor allen anderen Regeln dieser FR.
- Finale Kalibrierung (Architektur AD-5, löst die ursprünglich im PRD nur als Richtwert markierte Schwelle): mit mindestens einem Teilnehmer — Dauer ≤ 45 Minuten → **unterbrechbar**; ganztägig ODER (Dauer ≥ 90 Minuten UND ≥ 3 Teilnehmer) → **bitte-nicht-stören**; alle übrigen Fälle (46–89 Minuten, oder ≥ 90 Minuten mit < 3 Teilnehmern) → **unterbrechbar**.
- Tageszeit fließt nicht in die Berechnung ein.
- Hat ein Teammitglied zum aktuellen Zeitpunkt keinen laufenden Termin, gilt es standardmäßig als **unterbrechbar**.
- Die Status-Ableitung läuft mit derselben Sync-Verzögerung wie der übrige Kalender (wenige Minuten), kein Echtzeit-Anspruch.
- Out of Scope: Neubewertung bei nachträglicher Teilnehmer-Änderung an einem bereits laufenden/eingestuften Termin ist nicht spezifiziert. Bei sich überschneidenden Terminen mit unterschiedlichem Status gilt der strengere Wert (Architektur AD-6: bitte-nicht-stören sticht unterbrechbar).

### FR-11 — Manueller Status-Override (CAP-6)
Ein Teammitglied kann seinen eigenen, aktuell angezeigten Verfügbarkeits-Status jederzeit manuell übersteuern. Der Override bezieht sich auf den Nutzer als Ganzes (aktueller Status "gerade jetzt"), nicht auf eine einzelne Termininstanz.
- Ein manuell gesetzter Status bleibt bestehen, bis der Nutzer ihn aktiv zurücksetzt oder ändert — er wird nicht beim nächsten Sync-Zyklus automatisch von der Heuristik überschrieben, und er endet nicht automatisch mit dem Ende des Termins, der zum Zeitpunkt der Übersteuerung aktiv war.
- Andere Teammitglieder sehen keinen Unterschied zwischen automatisch abgeleitetem und manuell gesetztem Status (gleiche Darstellung).
- Out of Scope: Die Interaktion mit Serienterminen (FR-14) ist bei Umsetzung von FR-14 festzulegen.

## Cross-Cutting NFRs (Must-Have)

### Sync-Transparenz (CAP-7)
Die Oberfläche zeigt erkennbar an, wann ein Kalender zuletzt erfolgreich synchronisiert wurde. Bleibt ein Sync-Zyklus für ein Konto wiederholt aus (z. B. wegen abgelaufenem/widerrufenem Token), muss das für den Betreiber sichtbar werden — ein stiller, unbemerkter Sync-Ausfall ist bei Bus-Faktor-1-Betrieb sonst nicht erkennbar. Ein vom Nutzer selbst widerrufener Kalenderzugriff zeigt sich beim nächsten Poll-Versuch als regulärer Sync-Fehler über denselben Mechanismus (Architektur AD-16) — kein separates Signal nötig.

### Zugriffsschutz (CAP-8)
Zugriff auf das Tool erfordert eine Anmeldung — das Tool ist kein anonym erreichbares System, da es die vollen Kalenderdaten aller Teammitglieder aggregiert. Gespeicherte OAuth-Tokens (Zugriff auf Outlook/Google jedes Teammitglieds) müssen verschlüsselt abgelegt werden — ein Datenbankzugriff allein darf keinen direkten Zugriff auf die verbundenen Kalenderkonten ermöglichen.

### Datenhaltung nativer Termine / Backup (CAP-9)
Native Termine existieren ausschließlich in der Tool-Datenbank und sind — anders als synchronisierte Termine, die im Notfall erneut aus Outlook/Google geladen werden können — bei Datenverlust unwiederbringlich. Das System muss daher eine grundlegende Backup-/Wiederherstellungsfähigkeit für native Termine bereitstellen (Architektur AD-14: täglicher pg_dump, 14 Tage rollierend, dokumentierter manueller Restore).

## Should-Have (post-MVP, siehe SPEC.md Non-Goals)

### FR-12 — Gemeinsamer Slot-Finder
Ein Teammitglied kann mehrere Teammitglieder auswählen; das System schlägt automatisch gemeinsame freie Zeitfenster vor. Der Slot-Finder berücksichtigt ausschließlich reines Frei/Beschäftigt der ausgewählten Personen, nicht den abgeleiteten Verfügbarkeits-Status — ein technisch freier, aber als "bitte-nicht-stören" markierter Slot wird trotzdem vorgeschlagen.

### FR-13 — Termin-Erinnerungen
Das System kann ein Teammitglied an einen bevorstehenden eigenen Termin erinnern (konkreter Vorlauf architekturseitig festzulegen). Erinnerungen beziehen sich ausschließlich auf eigene Termine (nativ oder synchronisiert), nie auf fremde. Out of Scope: ein Hinweis, sobald ein wartender Kollege frei wird — das Sync-Intervall von mehreren Minuten macht ein solches Signal zu unzuverlässig.

### FR-14 — Serientermine
Ein Teammitglied kann wiederkehrende native Termine anlegen (täglich/wöchentlich/monatlich). Jede erzeugte Instanz erscheint einzeln in Kalenderansicht und Mehrpersonen-Ansicht und erhält einzeln einen Verfügbarkeits-Status nach FR-10. Die Bearbeitungs-/Löschgranularität (nur diese Instanz / diese und folgende / ganze Serie) sowie die Interaktion mit dem Status-Override (FR-11) sind bei Umsetzung dieser Funktion festzulegen. (Die Ingestion bereits importierter Serientermine aus Outlook/Google ist unabhängig davon bereits durch Architektur AD-15 geklärt — pro Instanz normalisiert.)

### FR-15 — Ortsangabe mit Kartenanzeige
Ein Teammitglied kann einem nativen Termin einen Ort mit Autovervollständigung hinzufügen; die Detailansicht zeigt eine Kartendarstellung. Ein Termin ohne angegebenen Ort zeigt keine Kartendarstellung (kein Pflichtfeld). Die Ortsangabe unterliegt demselben Privat-Default wie andere Termindetails (FR-9).
