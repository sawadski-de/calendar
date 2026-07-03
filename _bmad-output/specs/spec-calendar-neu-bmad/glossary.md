# Glossar — Team-Terminkalender mit Synchronisation

Companion zu SPEC.md. Begriffe, wie sie in SPEC.md und functional-requirements.md verwendet werden.

- **Teammitglied** — Eine der 5-10 festen Personen im Team, jede mit eigenem Outlook- oder Google-Konto, das ins Tool synchronisiert wird.
- **Nativer Termin** — Ein Termin, der direkt im Tool angelegt wurde. Existiert ausschließlich in der Tool-Datenbank, nicht in Outlook/Google.
- **Synchronisierter Termin** — Ein Termin, der per Import-Sync aus Outlook oder Google ins Tool übernommen wurde. Kann im Notfall erneut aus der Quelle geladen werden.
- **Einseitiger Sync** — Regelmäßiger Import (alle paar Minuten) von Outlook- und Google-Kalenderdaten ins Tool. Kein Zurückschreiben in die Quellsysteme.
- **Privat-Default** — Die Anzeigeregel, dass andere Teammitglieder bei jedem Termin (nativ oder synchronisiert) standardmäßig nur "privat/beschäftigt" plus den abgeleiteten Verfügbarkeits-Status sehen, nie den echten Titel oder Inhalt. Das Tool selbst liest im Hintergrund die vollen Termindaten; der Privat-Default ist eine Anzeigeregel, keine Datenzugriffsgrenze.
- **Verfügbarkeits-Status** — Der pro Termin abgeleitete oder manuell gesetzte Kontext-Wert: **unterbrechbar** oder **bitte-nicht-stören**. Ersetzt binäres Frei/Beschäftigt für alle Termine, nativ wie synchronisiert.
- **Status-Heuristik** — Die Regel, die aus Dauer, Teilnehmerzahl und (nachrangig) Tageszeit eines Termins automatisch den Verfügbarkeits-Status ableitet (Details: `functional-requirements.md` FR-10).
- **Status-Override** — Die manuelle Übersteuerung des abgeleiteten Verfügbarkeits-Status durch das betroffene Teammitglied selbst (FR-11).
- **Mehrpersonen-Ansicht** — Die Darstellung mehrerer ausgewählter Teammitglieder-Kalender nebeneinander in Spalten, verfügbar in Tages- und Wochenansicht (FR-2).
