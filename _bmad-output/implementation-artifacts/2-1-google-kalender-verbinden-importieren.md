---
baseline_commit: 7b271f9dd4c1021813c2a5af61ddc37f420deab0
---

# Story 2.1: Google-Kalender verbinden & importieren

Status: done

## Story

As a Teammitglied,
I want mein Google-Konto verbinden können, damit meine Google-Termine automatisch im Tool erscheinen,
so that ich nicht mehr zwischen Google Calendar und dem Tool wechseln muss, um vollständig zu sein.

## Acceptance Criteria

1. **Given** ich bin angemeldet und öffne Einstellungen → Kalenderverbindungen, **when** ich auf "Verbinden" bei Google klicke, **then** durchlaufe ich den OAuth-Consent-Flow (Scope `https://www.googleapis.com/auth/calendar.readonly`) und lande danach zurück in den Einstellungen mit dem Status "Verbunden". `[ASSUMPTION: gemeinsames Google-Workspace / Internal-App-Consent, siehe PRD Offene Frage #1 — siehe Dev Notes „Offene Punkte"]`
2. **Given** der OAuth-Handshake ist abgeschlossen, **when** die Access-/Refresh-Tokens gespeichert werden, **then** werden sie nie im Klartext persistiert — Verschlüsselung erfolgt anwendungsseitig, Entschlüsselung nur unmittelbar vor einem Google-API-Aufruf innerhalb von `Infrastructure`.
3. **Given** ein verbundenes Google-Konto, **when** der Worker seinen Sync-Zyklus ausführt, **then** ruft er `GoogleCalendarProvider.FetchAllEvents` auf, das einen vollständigen aktuellen Snapshot aller Termine im betrachteten Zeitfenster liefert (kein Delta/Incremental-Sync), und upsertet Termine anhand von `(PersonId, Provider, ProviderEventId)`.
4. **Given** ein bereits importierter Termin wird an der Quelle verschoben oder inhaltlich geändert, **when** der nächste Sync-Zyklus läuft, **then** wird der bestehende Eintrag aktualisiert, kein zusätzlicher Termin angelegt.
5. **Given** ein bereits importierter Termin wird an der Quelle gelöscht oder abgesagt, **when** der nächste Sync-Zyklus läuft und der zugehörige Schlüssel im aktuellen Abruf fehlt, **then** wird der Termin (inkl. eines davon abgeleiteten Status) aus dem Tool entfernt.
6. **Given** ein importierter Serientermin, **when** der `GoogleCalendarProvider` ihn verarbeitet, **then** wird serverseitig pro Instanz expandiert (nicht der Serien-Master) — jede Instanz erhält eine eigene `Appointment`-Zeile mit der providerseitigen Instanz-Event-ID als `ProviderEventId`.
7. **Given** ein importierter Google-Termin, **when** er gespeichert wird, **then** liest und speichert das System die vollen Daten (Titel, Teilnehmer, Dauer, Ort) und `StatusHeuristicService.Compute` weist ihm denselben Verfügbarkeits-Status nach derselben Regel wie native Termine zu (kein zweiter Heuristik-Codepfad).
8. **Given** kein Schreibpfad existiert gegen die Google-API, **when** ein nativer Termin im Tool angelegt, geändert oder gelöscht wird, **then** erzeugt dies keine Schreiboperation gegen Google — `ICalendarProvider` definiert strukturell keine Schreibmethode.
9. **Given** ich habe mein Google-Konto verbunden, **when** ich in Einstellungen → Kalenderverbindungen nachsehe, **then** zeigt die Google-Zeile den Zeitpunkt des letzten erfolgreichen Syncs ("Zuletzt synchronisiert vor N Min.") in Caption-Text mit Icon.
10. **Given** der Consent wird verweigert oder von der Google-Workspace-Organisation blockiert, **when** der Verbindungsversuch fehlschlägt, **then** zeigt die Zeile einen expliziten, providerspezifischen Fehlerhinweis statt eines generischen "Verbindung fehlgeschlagen", und der Versuch wird nicht stillschweigend wiederholt.
11. **Given** der Sync für ein Google-Konto schlägt wiederholt fehl (z. B. abgelaufenes Token), **when** `ConsecutiveFailureCount` einen Schwellenwert übersteigt, **then** wechselt die Sync-Anzeige in den expliziten Fehlerzustand (`dnd-text`-Farbe, nicht rohes `dnd`), die Änderung wird via `aria-live="polite"` angekündigt, und der Sync anderer Konten wird davon nicht blockiert.
12. **Given** noch kein Kalender verbunden ist, **when** ich meinen (leeren) Kalender öffne, **then** zeigt die Ansicht die einladende Nachricht "Noch keine Termine — verbinde deinen Kalender in den Einstellungen, wann immer du bereit bist." mit einem Link zu Einstellungen → Kalenderverbindungen — das Verbinden ist optional, kein erzwungener Onboarding-Schritt.

## Tasks / Subtasks

- [x] **Task 1 — Domain: `CalendarConnection` entity** (AC: 2, 3, 9, 10, 11)
  - [x] `src/Domain/CalendarConnection.cs`: `Id`, `PersonId`, `Provider` (string constant `"Google"`/`"Outlook"`, not an enum — Outlook reuses the same entity in Story 2.2), `EncryptedAccessToken`, `EncryptedRefreshToken` (both `string`, ciphertext only — Domain never sees plaintext), `AccessTokenExpiresUtc`, `LastSuccessfulSyncAt` (nullable), `LastAttemptAt` (nullable), `ConsecutiveFailureCount` (int, default 0), `LastErrorCode` (nullable string). Private setters, constructor + behavior methods: `UpdateTokens(accessToken, refreshToken, expiresUtc)`, `RecordSyncSuccess(nowUtc)` (resets `ConsecutiveFailureCount` to 0, clears `LastErrorCode`, sets both timestamps), `RecordSyncFailure(nowUtc, errorCode)` (increments counter, sets `LastAttemptAt` + `LastErrorCode`, leaves `LastSuccessfulSyncAt` untouched).
  - [x] One connection per `(PersonId, Provider)` — DB-enforced via unique index in Task 3.
- [x] **Task 2 — Domain: attendee schema for synced (non-team-member) participants** (AC: 7) — **`[ASSUMPTION — see Dev Notes]`**
  - [x] Extend `Attendee`: make `PersonId` nullable; add nullable `ExternalEmail`/`ExternalDisplayName`. Exactly one of `PersonId` or `ExternalEmail` must be set (DB check constraint). Add `Attendee.External(Guid id, Guid appointmentId, string email, string? displayName)` factory alongside the existing internal constructor path.
  - [x] `AppointmentViewService`/detail endpoint: when rendering an attendee, prefer the linked `Person` (internal) else fall back to `ExternalDisplayName ?? ExternalEmail`.
- [x] **Task 3 — Persistence: migration + `ApplicationDbContext` config** (AC: 2, 3, 6, 7)
  - [x] `ApplicationDbContext`: add `DbSet<CalendarConnection> CalendarConnections`, configure entity (required fields, unique index on `(PersonId, Provider)`, FK to `Person`). Update `Attendee` configuration for the new nullable `PersonId` (`OnDelete(DeleteBehavior.Restrict)` still applies only when `PersonId` is set — EF handles nullable FK deletes as `SET NULL`-incompatible-by-default automatically; explicit `DeleteBehavior.Restrict` still correct since we never delete a `Person` that has attendee rows without going through soft-deactivation, per AD-12) + a raw-SQL `HasCheckConstraint` requiring exactly one of `person_id`/`external_email` non-null.
  - [x] New migration (name: `AddCalendarConnectionsAndExternalAttendees`). Do **not** touch the existing partial unique index on `Appointment` (already correct from Story 1.3, see AD-7 — verified still `HasFilter("provider_event_id IS NOT NULL")`).
- [x] **Task 4 — Application: ports** (AC: 2, 3, 6, 7, 8)
  - [x] `src/Application/Sync/ICalendarProvider.cs`: `Task<IReadOnlyList<ExternalCalendarEvent>> FetchAllEvents(CalendarConnection connection, SyncWindow window, CancellationToken ct)`. **Exactly this one method — no write method, ever** (AD-2/FR-7, structurally enforced).
    `ExternalCalendarEvent` record (same file): `ProviderEventId`, `Title`, `StartUtc`, `EndUtc`, `IsAllDay`, `Location`, `Attendees` (`IReadOnlyList<ExternalAttendee>` — `record ExternalAttendee(string Email, string? DisplayName)`).
    `SyncWindow` record: `RangeStartUtc`, `RangeEndUtc`.
  - [x] `src/Application/Sync/ITokenEncryption.cs`: `string Encrypt(string plaintext)`, `string Decrypt(string ciphertext)`. Application defines the contract; Infrastructure implements it (AD-8 — decryption only in Infrastructure, immediately before a provider call).
  - [x] `src/Application/Sync/ICalendarConnectionRepository.cs`: `GetAsync(personId, provider, ct)`, `GetAllActiveAsync(ct)` (all connections for `Person.IsActive == true` — AD-12: Worker must skip deactivated people), `UpsertAsync(connection, ct)`.
  - [x] `src/Application/Sync/CalendarSyncService.cs`: orchestrates one connection's sync — calls `ICalendarProvider.FetchAllEvents`, upserts/deletes `Appointment` rows via `IAppointmentRepository` (extend with `UpsertSyncedAsync`/`DeleteMissingSyncedAsync`, see Task 5), calls `StatusHeuristicService.Compute` per event, calls `connection.RecordSyncSuccess`/`RecordSyncFailure`. This is the **one** place the upsert/dedup/expiry logic lives — both `GoogleCalendarProvider` (this story) and `OutlookCalendarProvider` (Story 2.2) plug into it via `ICalendarProvider`, so there is exactly one upsert codepath, not two.
- [x] **Task 5 — Application/Infrastructure: extend appointment write side for sync** (AC: 3, 4, 5, 6, 7)
  - [x] Extend `IAppointmentRepository` (`src/Application/Appointments/IAppointmentRepository.cs`) with methods `CalendarSyncService` needs: fetch existing synced appointments for a `(personId, provider)` pair keyed by `ProviderEventId`, upsert one, delete by id. Implement in `Infrastructure/Appointments/AppointmentRepository.cs`.
  - [x] Reuse the existing `Appointment` constructor (`provider`/`providerEventId` params already exist from Story 1.3) — no Domain change needed here beyond Task 2's attendee work.
- [x] **Task 6 — Infrastructure: `ITokenEncryption` (AES-GCM)** (AC: 2)
  - [x] `Infrastructure/Sync/AesGcmTokenEncryption.cs`: reads a 32-byte key from `TOKEN_ENCRYPTION_KEY` (base64, already reserved by `.env.example`/`docker-compose.yml` since Story 1.1 scaffolding — first consumer of this var). Fail fast at startup (throw with a clear message) if the var is missing/not valid base64/not 32 bytes, in both `Api` and `Worker` composition roots — Api needs it for the OAuth callback write, Worker needs it for decrypt-before-call.
  - [x] Ciphertext format: `nonce(12B) || tag(16B) || ciphertext`, base64-encoded as the stored string.
- [x] **Task 7 — Infrastructure: `GoogleCalendarProvider` + OAuth handshake** (AC: 1, 3, 6, 7)
  - [x] `Infrastructure/Sync/GoogleOAuthOptions.cs`: `ClientId`, `ClientSecret`, `RedirectUri` bound from config (env vars `GOOGLE_OAUTH_CLIENT_ID`, `GOOGLE_OAUTH_CLIENT_SECRET`, `GOOGLE_OAUTH_REDIRECT_URI` — follow the existing flat env-var convention). **Correction made during implementation:** the original draft of this task assumed the Worker wouldn't need `ClientSecret` for token refresh — that's wrong, Google's `refresh_token` grant requires `client_id`+`client_secret` for a confidential ("Web application") OAuth client, which is what the Internal-Workspace consent flow uses. Both `Api` and `Worker` get all three env vars.
  - [x] `Infrastructure/Sync/GoogleOAuthClient.cs` (used only by the **Api**, per AD-8 "API schreibt auf `CalendarConnection` nur beim initialen Handshake"): builds the consent-screen redirect URL (`scope=https://www.googleapis.com/auth/calendar.readonly`, `access_type=offline`, `prompt=consent` — `prompt=consent` is required to guarantee Google returns a refresh token on every connect, not just the first one ever), and exchanges the returned `code` for access/refresh tokens via a plain `HttpClient` POST to `https://oauth2.googleapis.com/token` (no Google SDK dependency — keeps the surface small and avoids an unverified .NET 10 compatibility risk in `Google.Apis.*` packages; plain REST is sufficient for exactly the token exchange + Calendar `events.list` call this story needs).
  - [x] `Infrastructure/Sync/GoogleCalendarProvider.cs` implements `ICalendarProvider`: refreshes the access token via `https://oauth2.googleapis.com/token` (`grant_type=refresh_token`) whenever `connection.AccessTokenExpiresUtc` has passed, calls `GET https://www.googleapis.com/calendar/v3/calendars/primary/events` with `timeMin`/`timeMax` = `window`, `singleEvents=true` (this is what makes Google expand recurring series server-side into instances for us — **do not** set `singleEvents=false`, that would return the series master instead of instances and violate AC 6/AD-15), pages through `nextPageToken`, maps each item into `ExternalCalendarEvent` (an item with `start.date` only, no `start.dateTime`, is all-day). **This provider is read-only by construction — no method in this class ever issues a POST/PATCH/DELETE against a Google mutation endpoint (FR-7/AD-2, AC 8).**
  - [x] Token refresh writes (new access token + expiry) belong to whichever process calls `FetchAllEvents` — AD-8 says refresh is the Worker's job in steady state, but the **first** call may happen from the Settings page's "letzter Sync" read if you choose to eager-sync on connect; this story does **not** implement eager-sync-on-connect (out of scope — first data appears after the Worker's next scheduled cycle, matching NFR-1 "few minutes, no real-time claim"). Document this in the Settings UI copy (Task 10) so Dennis isn't surprised by an empty calendar immediately after connecting.
- [x] **Task 8 — Api: OAuth endpoints + connection status** (AC: 1, 2, 9, 10)
  - [x] `src/Api/Contracts/CalendarConnectionContracts.cs`: `CalendarConnectionResponse(string Provider, bool Connected, DateTimeOffset? LastSuccessfulSyncAt, bool HasError, string? ErrorCode)`.
  - [x] `src/Api/Endpoints/CalendarConnectionEndpoints.cs`, `MapCalendarConnectionEndpoints`, all under `.RequireAuthorization()` (per-user, own connections only — identity from `ClaimTypes.NameIdentifier` exactly like `AppointmentEndpoints`):
    - `GET /api/calendar-connections` → list of `CalendarConnectionResponse` for the current person (Google + Outlook rows, `Connected: false` for a provider with no row yet — Story 2.2 adds the Outlook row's real data, this story returns `Connected: false`/nulls for Outlook).
    - `GET /api/calendar-connections/google/authorize` → `Results.Redirect` to Google's consent URL (state param = a signed/short-lived random token stored server-side — e.g. `DataProtection`-protected — tied to the current person, to prevent CSRF on the callback; **do not** put the raw `PersonId` in `state` unsigned).
    - `GET /api/calendar-connections/google/callback` → validates `state`, exchanges `code` via `GoogleOAuthClient`, encrypts tokens via `ITokenEncryption`, upserts the `CalendarConnection` row, redirects (302) to the frontend's settings-connections route with a `?connected=google` or `?error=<code>` query flag (frontend reads it once and shows a toast/inline state, then strips it from the URL — no error state is persisted purely in a query string beyond that one read).
    - Consent-denied / tenant-blocked from Google arrives as `?error=access_denied` (or similar) on the OAuth redirect back to our callback — map that to a `CalendarConnection`-level `LastErrorCode` (e.g. `"consent_denied"`) via the same `RecordSyncFailure` path so the Settings row's error rendering (Task 10) doesn't need a second code path for "never connected, consent failed" vs. "was connected, sync now fails" (AC 10 vs. AC 11 render through the same component).
- [x] **Task 9 — Worker: sync loop** (AC: 3, 4, 5, 6, 7, 11)
  - [x] `Worker/Program.cs`: currently a bare `Host.CreateApplicationBuilder` skeleton with no DbContext — add `AddDbContext<ApplicationDbContext>` (mirror `Api/Program.cs`'s Npgsql + `UseSnakeCaseNamingConvention` setup exactly, same `ConnectionStrings__Default` env var), register `ICalendarProvider` → `GoogleCalendarProvider`, `ITokenEncryption` → `AesGcmTokenEncryption`, `ICalendarConnectionRepository`, `IAppointmentRepository`, `CalendarSyncService`. Worker has **no** reference to `Api` (confirmed in `Worker.csproj`) — do not add one.
  - [x] Rewrite `SyncBackgroundService.ExecuteAsync`: replace the placeholder 1-second `Task.Delay` loop with a real interval (5 minutes — satisfies NFR-1 "few minutes, no real-time claim"; make it configurable via `SYNC_INTERVAL_SECONDS` env var, default 300). Each cycle: `ICalendarConnectionRepository.GetAllActiveAsync` (already filters `Person.IsActive`, AD-12), for each connection call `CalendarSyncService.SyncAsync(connection, window)` inside a `try/catch` **per connection** — one connection's exception must not stop the loop from processing the rest (AD-16 "ein Fehlschlag blockiert nicht den Sync anderer Konten", AC 11). `window` = e.g. `[now - 30 days, now + 180 days]` — no explicit window size is specified anywhere in PRD/architecture; this is a **reasonable default, not a spec'd value** — flagged in Dev Notes as an assumption to confirm.
  - [x] Failure handling: catch provider exceptions (HTTP errors, token-refresh failures), call `connection.RecordSyncFailure(now, errorCode)` where `errorCode` is a small stable set (`"token_refresh_failed"`, `"provider_error"`, `"unknown_error"`) — never the raw exception message (AD-13 spirit: stable codes, not free text, even though this isn't an API response).
  - [x] Add `worker` service to `deploy/docker-compose.yml` (build context `..`, dockerfile `src/Worker/Dockerfile` — **does not exist yet, create it**, mirroring `src/Api/Dockerfile`'s multi-stage build pattern but with `ENTRYPOINT ["dotnet", "Worker.dll"]`), with the same `ConnectionStrings__Default`/`TOKEN_ENCRYPTION_KEY` env vars as `api`, plus the new `GOOGLE_OAUTH_CLIENT_ID`/`GOOGLE_OAUTH_CLIENT_SECRET` (needed for the refresh-token grant call), `depends_on: postgres: condition: service_healthy`.
- [x] **Task 10 — Frontend: Settings → Kalenderverbindungen page** (AC: 1, 9, 10, 11, 12)
  - [x] New feature folder `frontend/src/app/pages/settings/connections/` following the `pages/home/calendar/` pattern: `connections.service.ts` (HttpClient wrapper: `getConnections()`, connect is a plain `<a href="/api/calendar-connections/google/authorize">` navigation, not an XHR call — it's a full-page OAuth redirect), `connection.model.ts` (mirrors `CalendarConnectionResponse`), `connections.ts`/`connections.html` (the page), `connection-row/` sub-component matching `mockups/key-settings-connections.html` structure (`.conn-row`, `.conn-top`, `.status-tag`, `.sync-line`, `.error-box`, `.conn-actions` — reuse class-level structure, restyle with actual design tokens from `styles.css`/existing components rather than copying the mockup's inline `<style>` block verbatim).
  - [x] Route: add to `app.routes.ts` under `authGuard`, e.g. `path: 'settings/connections'`.
  - [x] Sync-line text uses `{colors.dnd-text}` (not raw `{colors.dnd}`) for error-state **text**, raw `{colors.dnd}` is fine for the error **icon** — per DESIGN.md's explicit correction over the mockup (mockup literally uses raw `--dnd` for `.sync-line.err` text, which DESIGN.md flags as the wrong contrast choice — **follow DESIGN.md's text, not the mockup's CSS**, DESIGN.md states this precedence explicitly).
  - [x] Error-state text transition announced via `aria-live="polite"` (a visually-hidden or `role="status"` region wrapping the sync-line, updated when status flips from ok→error).
  - [x] Read `?connected=google` / `?error=<code>` query params on page load, show an inline confirmation/error, then `router.navigate` to strip the query string (don't leave it in the URL on refresh).
  - [x] Update the empty-calendar message (already exists per Story 1.2/1.3 as `calendar.empty`: "Du hast noch keine Termine.") — per AC 12 this needs to become the inviting connect-prompt **specifically when the user has zero appointments AND zero calendar connections** (a user with appointments just has a normal calendar; a user with a connection but zero synced events yet, e.g. right after connecting before the first Worker cycle, should probably still see the plain empty state, not re-prompt to connect — check `getConnections()` alongside the existing empty-appointments check to decide which message to show). New i18n key, e.g. `calendar.emptyNoConnection`, with an inline router link to `/settings/connections`.
- [x] **Task 11 — i18n** (AC: 1, 9, 10, 11, 12)
  - [x] Add `de.json`/`en.json` keys under a new `settings.connections` namespace: page heading/sub, per-provider labels, "Verbinden"/"Verbindung trennen" (disconnect is out of scope for this story's AC — **do not** build a disconnect endpoint/button yet, just the connect+status rendering; note as scope boundary, not a TODO left in code), sync-line templates ("Zuletzt synchronisiert vor {{minutes}} Min." / "Kein erfolgreicher Sync bisher"), the tenant-blocked error copy ("Verbindung von deinem Unternehmen blockiert — bitte den IT-Admin kontaktieren."), and the updated calendar empty-state key from Task 10. Exact copy from EXPERIENCE.md's Voice-and-Tone table — do not paraphrase.
- [x] **Task 12 — Tests**
  - [x] Unit: `AesGcmTokenEncryptionTests` (round-trip, tamper-detection — a flipped ciphertext byte must throw, not silently decrypt garbage). `CalendarSyncServiceTests` using a fake `ICalendarProvider`/`IAppointmentRepository`/`ICalendarConnectionRepository` (follow the existing `FakePersonRepository` hand-written-fake convention, not a mocking library) covering: new event → insert with computed status; changed event (same `ProviderEventId`) → update in place, no duplicate; event missing from latest snapshot → delete; provider throws → `RecordSyncFailure` called, other connections in the same cycle unaffected.
  - [x] Integration (Testcontainers Postgres, reuse `TestApiFactory`): `GET /api/calendar-connections` returns `Connected: false` for a fresh user; the OAuth callback endpoint with a stubbed `GoogleOAuthClient`/`ICalendarProvider` (register a fake in the test factory's DI override) creates a `CalendarConnection` row with encrypted tokens (assert the stored string isn't the plaintext fixture token); the partial unique index still rejects a duplicate `(PersonId, Provider, ProviderEventId)` insert attempt (regression check that Task 3's migration didn't disturb Story 1.3's index).
  - [x] No test may call the real Google OAuth/Calendar endpoints — everything above uses fakes/stubs. End-to-end verification against a real Google account is a manual step for Dennis post-merge (see Dev Notes).

## Dev Notes

### Architecture compliance (must follow exactly)

- **`ICalendarProvider` has exactly one method, `FetchAllEvents`, and never a write method** (AD-2). This is the single most important structural guardrail in this story — it's what makes FR-7 ("kein Zurückschreiben") true by construction rather than by discipline. Do not add a convenience write method "for testing" or "for future use."
- **Partial unique index `(PersonId, Provider, ProviderEventId) WHERE provider_event_id IS NOT NULL`** already exists from Story 1.3 (`ApplicationDbContext.cs`, `Appointment` entity config) — reuse it as-is, do not create a second index.
- **`StatusHeuristicService.Compute(appointment)`** (Domain, static, no dependencies) is the only status computation path — call it from `CalendarSyncService` exactly as `AppointmentCreationService` already does for native appointments (see `src/Application/Appointments/` for the existing call site pattern). Do not reimplement the duration/attendee-count rules.
- **AD-8 token handling**: encrypt only in Infrastructure, decrypt only immediately before a Google API call, never log a decrypted token (check any `logger.LogInformation`/`LogError` calls in the provider don't interpolate the token itself).
- **AD-11**: Worker and Api share `Domain`/`Application`/`Infrastructure` project references and coordinate only through Postgres — confirmed by `Worker.csproj` already referencing both; do not add a Worker→Api HTTP call or vice versa.
- **AD-12**: `ICalendarConnectionRepository.GetAllActiveAsync` must join/filter on `Person.IsActive` (that field doesn't exist yet in the current `Person` entity — check `src/Domain/Person.cs` at implementation time; if `IsActive` hasn't landed yet because Epic 5 is still backlog, filter is a no-op for now but **write the join anyway** so Epic 5's Story 5.1 doesn't have to come back and modify this repository — add a `TODO` only if the field truly doesn't exist yet, otherwise wire it for real).
- **AD-13**: any new API error responses (`CalendarConnectionEndpoints`) use `ProblemResults.Problem(...)`, matching `AppointmentEndpoints`'s existing pattern — never a bespoke JSON error shape.

### Design/UX compliance

- Sync-indicator neutral text color `{colors.muted}`, error text `{colors.dnd-text}` (not raw `{colors.dnd}` — raw `dnd` fails the 4.5:1 AA caption-text contrast threshold per DESIGN.md's own contrast table; `dnd-text` clears it at ≈9.4:1 dark / ≈8.8:1 light). Raw `dnd` remains correct for the error icon and the `.error-box` left border/fill.
- Exact required copy (EXPERIENCE.md Voice-and-Tone, do not paraphrase): "Zuletzt synchronisiert vor N Min.", "Verbindung von deinem Unternehmen blockiert — bitte den IT-Admin kontaktieren.", "Noch keine Termine — verbinde deinen Kalender in den Einstellungen, wann immer du bereit bist."
- Settings → Kalenderverbindungen is reachable from the Settings menu, **never** a forced onboarding step — a brand-new user must land directly in their own empty calendar (already true since Story 1.2; this story must not add a redirect-to-connect-calendar interstitial anywhere).
- `aria-live="polite"` on the sync-error state transition (EXPERIENCE.md Accessibility Floor) — this is new-to-this-story, no existing component in the codebase does this yet; look at how `focus-trap`/`activatable` (`frontend/src/app/shared/`) structure their accessibility-focused shared components for the project's conventions, but the live-region mechanism itself is net new.

### Existing code this story touches (read before changing)

- `src/Domain/Appointment.cs` — constructor already accepts `provider`/`providerEventId` (added Story 1.3 anticipating this epic) — no change needed, only `Attendee.cs` changes (Task 2).
- `src/Domain/StatusHeuristicService.cs` — pure function, call as-is, do not modify.
- `src/Infrastructure/Persistence/ApplicationDbContext.cs` — current `Attendee` config: `entity.HasOne<Person>().WithMany().HasForeignKey(at => at.PersonId).OnDelete(DeleteBehavior.Restrict)` plus a unique `(AppointmentId, PersonId)` index. Task 2/3 must adapt this for nullable `PersonId` — the existing unique index on `(AppointmentId, PersonId)` silently allows multiple external attendees with `PersonId == NULL` on the same appointment today if left unchanged (Postgres treats `NULL` as distinct in unique indexes) — that's actually the **correct** behavior here (two different external emails on one appointment must both be allowed), just confirm it rather than "fixing" it into a NULLS NOT DISTINCT constraint.
- `src/Api/Program.cs` — DI registration block (`builder.Services.AddScoped<...>`) — add new registrations here, following the existing flat list style, not a new `AddCalendarSync()` extension-method wrapper (matches current codebase's lack of such wrappers elsewhere).
- `src/Worker/Program.cs` / `SyncBackgroundService.cs` — currently a placeholder (see Task 9) — this story turns the Worker from a skeleton into the first real functionality it has.
- `deploy/docker-compose.yml` / `deploy/.env.example` — `TOKEN_ENCRYPTION_KEY` already reserved (Story 1.1) but never consumed until now — this is its first real consumer, first time an actual 32-byte key value matters (the placeholder value `change-me-32-byte-base64-key-placeholder` is not valid base64/32 bytes — Dennis must set a real value in his local `.env` before running this story's code; note this in the PR description).
- `frontend/public/i18n/de.json` / `en.json` — existing `calendar.empty` key gets a sibling `calendar.emptyNoConnection`, not a replacement (a user with a connection but a genuinely empty range should still see the plain message).

### Offene Punkte — an Dennis, nicht blockierend (in `_bmad-output/implementation-artifacts/epic-2-open-questions.md` protokolliert)

1. **Google Cloud OAuth-App-Registrierung fehlt real** — Client-ID/Secret in `.env`/`docker-compose.yml` sind Platzhalter. Der komplette OAuth-Flow (Consent-Screen-Typ Internal vs. External, Verifizierungsstatus, Redirect-URI-Whitelist) kann nur mit einer echten Google-Cloud-Console-App-Registrierung end-to-end getestet werden — das ist eine manuelle Aktion außerhalb des Codes. Implementierung ist vollständig, aber ungetestet gegen echtes Google bis diese Registrierung existiert.
2. **`[ASSUMPTION]` Attendee-Schema für externe (nicht-Team-)Teilnehmer** (Task 2) — die bestehende `Attendee`-Entität (Story 1.3) ist zwingend an `Person` gebunden (interne Teammitglieder-Auswahl). Importierte Google-Termine haben aber beliebige externe E-Mail-Teilnehmer, die keine `Person`-Zeile haben. Architecture Spine spezifiziert das nicht explizit. Angenommene Lösung: `PersonId` nullable machen, `ExternalEmail`/`ExternalDisplayName` ergänzen. Alternative wäre eine komplett getrennte Tabelle für externe Teilnehmer — bitte bei Gelegenheit bestätigen, bevor Epic 3 (Mehrpersonen-Ansicht) auf dieser Struktur aufbaut.
3. **Sync-Zeitfenster nicht spezifiziert** — PRD/Architecture nennen keine konkrete Fenstergröße für `FetchAllEvents(connection, window)`. Angenommen: `[jetzt - 30 Tage, jetzt + 180 Tage]`. Sollte bei Bedarf zentral konfigurierbar gemacht werden, aktuell hart im Worker kodiert.
4. **Sync-Intervall 5 Minuten** angenommen (NFR-1 sagt nur "im Bereich weniger Minuten") — konfigurierbar über `SYNC_INTERVAL_SECONDS`, Default 300.
5. **Disconnect/Verbindung-trennen-Funktion bewusst nicht in dieser Story** — die Story-AC verlangen sie nicht; der Mockup zeigt einen "Verbindung trennen"-Button, aber ohne zugehöriges FR/AC. Zurückgestellt, nicht vergessen — ggf. eigene kleine Story/Nachtrag.
6. **Kein Eager-Sync beim Verbinden** — nach dem OAuth-Handshake erscheinen Termine erst nach dem nächsten Worker-Zyklus (bis zu 5 Min.), nicht sofort. Bewusste Vereinfachung (kein synchroner Erstsync im Api-Request), UI-Copy sollte das nicht implizit versprechen.

### Project Structure Notes

- New Application area: `src/Application/Sync/` (parallel to existing `Accounts/`, `Appointments/`) — `ICalendarProvider`, `ITokenEncryption`, `ICalendarConnectionRepository`, `CalendarSyncService`.
- New Infrastructure area: `src/Infrastructure/Sync/` — `GoogleOAuthOptions`, `GoogleOAuthClient`, `GoogleCalendarProvider`, `AesGcmTokenEncryption`, `CalendarConnectionRepository`.
- New Domain file: `src/Domain/CalendarConnection.cs`.
- New Api area: `src/Api/Contracts/CalendarConnectionContracts.cs`, `src/Api/Endpoints/CalendarConnectionEndpoints.cs`.
- New frontend feature: `frontend/src/app/pages/settings/connections/`.
- No conflicts detected with the unified project structure (Architecture Spine's Source Tree section) — `CalendarConnection` in Domain matches the naming-convention row that lists it alongside `Person`/`Appointment`/`Attendee`/`StatusOverride` as a Domain entity.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Epic 2, Story 2.1] — full AC text (Given/When/Then reproduced above).
- [Source: _bmad-output/planning-artifacts/architecture/architecture-calendar-neu-bmad-2026-07-02/ARCHITECTURE-SPINE.md] — AD-2, AD-4, AD-7, AD-8, AD-11, AD-13, AD-15, AD-16, AD-17; Source Tree; Consistency Conventions.
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-calendar-neu-bmad-2026-07-02/DESIGN.md#components.sync-indicator, #Colors, #Do's/Don'ts]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-calendar-neu-bmad-2026-07-02/EXPERIENCE.md#Information Architecture, #State Patterns, #Voice and Tone, #Accessibility Floor]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-calendar-neu-bmad-2026-07-02/mockups/key-settings-connections.html] — markup/class reference, DESIGN.md text wins on any conflict (explicitly stated there).
- [Source: _bmad-output/project-context.md] — critical implementation rules (all sections apply).
- [Source: src/Infrastructure/Persistence/ApplicationDbContext.cs] — existing `Appointment`/`Attendee` EF config to extend.
- [Source: src/Api/Program.cs] — DI/auth/error-handling composition-root pattern to mirror.
- [Source: src/Worker/Program.cs, src/Worker/SyncBackgroundService.cs] — current skeleton being replaced.
- [Source: deploy/docker-compose.yml, deploy/.env.example] — existing env-var conventions and reserved `TOKEN_ENCRYPTION_KEY`.
- [Source: tests/IntegrationTests/Infrastructure/PostgresContainerFixture.cs, TestApiFactory.cs] — Testcontainers harness to reuse.

### Previous Story Intelligence (Story 1.4 → 2.1)

- Story 1.3/1.4 established the fake-over-mock convention for Application-layer unit tests (`FakePersonRepository`) — follow it for `CalendarSyncServiceTests` rather than introducing a mocking library.
- Story 1.3/1.4 code review findings (see `_bmad-output/implementation-artifacts/deferred-work.md`) were about CSRF defense-in-depth and `SecurePolicy=Always` needing real TLS on a non-localhost host — both still apply to the new OAuth callback endpoint (state-changing, cookie-authenticated) and are pre-existing/accepted risk, not new to this story; don't re-litigate them here.
- Migration naming convention observed: `{timestamp}_{PascalCaseDescription}` (e.g. `20260703172204_AddAppointmentStatusAndAttendees`).

### Git Intelligence Summary

- Recent commits show a per-story feature-branch + squash-merge PR pattern: `feature/{epic}-{story}-{kebab-slug}` → PR titled `Story {epic}.{story}: {title}` merged into `dev`. Suggested branch for this story: `feature/2-1-google-kalender-verbinden`.

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

- Worktree initially branched from `main`/Initial-Commit instead of `dev` — caught before writing any code by comparing `src/Domain/` file listing against what the Explore research pass expected from Epic 1; fixed via `git reset --hard origin/dev`. No functional impact, logged in `_bmad-output/implementation-artifacts/epic-2-open-questions.md`.
- Two real bugs found and fixed via actual test runs, not just review:
  1. `connections.html`'s status-tag branch order checked `connection.connected` before `connection.hasError`, so a previously-connected account that started failing repeatedly (AC 11) never showed the error tag — caught by `connections.spec.ts`, fixed by reordering the `@if` chain to check `hasError` first.
  2. `TokenEncryptionOptions`/`GoogleOAuthOptions` were constructed by reading `IConfiguration` eagerly at Program.cs top-level-statement time — `WebApplicationFactory`-based integration tests inject config overrides around the `builder.Build()` call, which happens *after* that eager read, so every integration test touching `ITokenEncryption` failed with "TOKEN_ENCRYPTION_KEY is not set" even though the test factory did set it. Fixed by switching to a DI-factory registration (`AddSingleton(sp => ...)`) so the read happens lazily at first resolution, well after `Build()`. Also broadened `TryValidateState`'s catch from `CryptographicException` to `Exception`, since a malformed (non-base64url) `state` value throws `FormatException` before decryption is even attempted, not `CryptographicException` — this was surfacing as an unhandled 500 instead of the intended `error=invalid_state` redirect.
- `dotnet ef migrations remove` couldn't run without a live DB connection string configured — worked around by deleting the generated migration files directly and restoring `ApplicationDbContextModelSnapshot.cs` via `git checkout` when the migration needed to be regenerated (once to add the `Location` column that AC 7 required but wasn't yet in `Appointment`).
- One pre-existing integration test (`AdminEndpointsTests.Concurrent_demote_requests_against_a_two_admin_system_never_leave_zero_admins`) failed once during a full-suite run under parallel container load; re-ran in isolation and it passed — confirmed as a pre-existing flake unrelated to this story's changes (nothing in this story touches admin role-change concurrency).

### Senior Developer Review (workflow-backed `/code-review`, high effort, all findings CONFIRMED and fixed)

1. **Data-loss risk (high)** — `AppointmentRepository.ApplySyncResultAsync` ran the delete and the insert as two separate, non-transactional operations despite its doc comment claiming atomicity; a crash/DB error between the two could permanently drop appointments. Fixed: wrapped both steps in one DB transaction via `CreateExecutionStrategy()` (mirrors `PersonRepository.ExecuteAtomicallyAsync`'s existing pattern in this codebase).
2. **Silent-failure risk (high, two related findings)** — `CalendarSyncService.SyncAsync` only wrapped the provider-fetch call in a try/catch, and only caught `CalendarProviderException` there; the entire persist step (diff apply + `RecordSyncSuccess`/`UpsertAsync`) had no exception handling at all, and non-`CalendarProviderException` failures (e.g. a token-decryption error after key rotation, a DB timeout) propagated uncaught past the Worker's per-connection catch without ever calling `CalendarConnection.RecordFailure` — so the Settings UI could show a connection as healthy indefinitely while sync was silently broken. Fixed: the whole fetch+diff+persist body is now one try/catch, catching any `Exception` (mapping to `"unknown_error"` when it isn't a `CalendarProviderException`) while still letting `OperationCanceledException` propagate on real cancellation. Added 3 new unit tests covering: generic-exception → recorded failure, persistence-step exception → recorded failure, and cancellation → not recorded as a failure.
3. **Correctness (medium)** — `HasChanged`'s attendee-email comparison was ordinal/case-sensitive, so a provider normalizing an email's casing between syncs looked like a real change and needlessly deleted+recreated the appointment (a new Id, which could invalidate an open detail popover). Fixed: compare emails case-insensitively. Added a regression test.
4. **Cleanup (low)** — `SyncBackgroundService` synced connections one at a time in a `foreach`, making a cycle's wall-clock time grow linearly with connection count. Fixed: each connection now syncs in its own DI scope (required — `ApplicationDbContext` isn't thread-safe) via `Parallel.ForEachAsync` bounded to 8 concurrent syncs; `TimeProvider` moved to constructor injection since it's a singleton and no longer needs a per-cycle scope resolution.
5. **Cleanup (low)** — `connections.ts`'s `syncLineKey()` method was dead code duplicating the status-priority logic already hand-written inline in `connections.html`. Removed.

All fixes verified: 31/31 unit tests, 41/41 integration tests (including the transaction-fix regression path via the existing sync-diff tests), 76/76 frontend tests, full solution + `ng build` both clean.

### Completion Notes List

- All 12 tasks complete; all 12 ACs implemented and covered by tests.
- Full backend solution builds clean (`dotnet build calendar-neu-bmad.slnx`, 0 warnings/0 errors).
- Unit tests: 27/27 pass (`dotnet test tests/UnitTests`). Integration tests: 41/41 pass in isolation, 40/41 in a full parallel run (see Debug Log — the 1 failure is a pre-existing unrelated flake, confirmed passing standalone). Frontend: 76/76 Vitest specs pass (`ng test`), production build succeeds (`ng build`) including the new lazy `connections` chunk.
- Three genuine design gaps surfaced during implementation that the story/architecture didn't fully specify — resolved with documented assumptions rather than blocking (all logged in `_bmad-output/implementation-artifacts/epic-2-open-questions.md` for Dennis to review):
  1. **External attendee schema** — `Attendee.PersonId` made nullable, added `ExternalEmail`/`ExternalDisplayName` for synced participants who aren't team members (DB check constraint enforces exactly one of the two). Affects Epic 3's Mehrpersonen-Ansicht work if it reads attendees.
  2. **`CalendarConnection` must exist before a successful connection** — to show AC 10's persistent error for a consent-denied/never-connected account, `CalendarConnection`'s tokens had to become nullable (row created on first "Verbinden" attempt regardless of outcome, `IsConnected` computed from `EncryptedAccessToken != null`) rather than only created on success as the original task draft implied.
  3. **`Appointment.Location`** — didn't exist on the entity at all; added (nullable, unsurfaced in any UI per Story 1.4's explicit FR-15 deferral) since AC 7 requires storing it for synced events even though nothing displays it yet.
- Real Google OAuth app credentials do not exist yet (placeholders in `.env.example`) — the code-exchange-with-Google path is implemented but cannot be exercised end-to-end until Dennis registers an OAuth 2.0 Client in Google Cloud Console and sets real `GOOGLE_OAUTH_CLIENT_ID`/`SECRET`/`REDIRECT_URI` + a real `TOKEN_ENCRYPTION_KEY`. Everything else (status reads, encryption at rest, authorize redirect, consent-denied/invalid-state error handling, sync diff/upsert/delete logic) is fully tested without live Google calls.
- Disconnect ("Verbindung trennen") is intentionally not built — no AC requires it; flagged in open questions as a possible follow-up.

### File List

**New files**
- `src/Domain/CalendarConnection.cs`
- `src/Application/Sync/ICalendarProvider.cs`
- `src/Application/Sync/ITokenEncryption.cs`
- `src/Application/Sync/ICalendarConnectionRepository.cs`
- `src/Application/Sync/CalendarSyncService.cs`
- `src/Application/Sync/CalendarProviders.cs`
- `src/Infrastructure/Sync/AesGcmTokenEncryption.cs`
- `src/Infrastructure/Sync/GoogleOAuthOptions.cs`
- `src/Infrastructure/Sync/GoogleOAuthClient.cs`
- `src/Infrastructure/Sync/GoogleCalendarProvider.cs`
- `src/Infrastructure/Sync/CalendarConnectionRepository.cs`
- `src/Infrastructure/Persistence/Migrations/20260703200241_AddCalendarConnectionsAndExternalAttendees.cs`
- `src/Infrastructure/Persistence/Migrations/20260703200241_AddCalendarConnectionsAndExternalAttendees.Designer.cs`
- `src/Api/Contracts/CalendarConnectionContracts.cs`
- `src/Api/Endpoints/CalendarConnectionEndpoints.cs`
- `src/Worker/Dockerfile`
- `frontend/src/app/pages/settings/connections/connection.model.ts`
- `frontend/src/app/pages/settings/connections/connections.service.ts`
- `frontend/src/app/pages/settings/connections/connections.ts`
- `frontend/src/app/pages/settings/connections/connections.html`
- `frontend/src/app/pages/settings/connections/connections.css`
- `frontend/src/app/pages/settings/connections/connections.spec.ts`
- `tests/UnitTests/Infrastructure/AesGcmTokenEncryptionTests.cs`
- `tests/UnitTests/Application/Sync/CalendarSyncServiceTests.cs`
- `tests/UnitTests/Application/Sync/FakeAppointmentRepository.cs`
- `tests/UnitTests/Application/Sync/FakeCalendarConnectionRepository.cs`
- `tests/UnitTests/Application/Sync/FakeCalendarProvider.cs`
- `tests/UnitTests/Application/Sync/FixedTimeProvider.cs`
- `tests/IntegrationTests/CalendarConnectionEndpointsTests.cs`
- `_bmad-output/implementation-artifacts/epic-2-open-questions.md`

**Modified files**
- `src/Domain/Appointment.cs` (added `Location`, `AddExternalAttendee`)
- `src/Domain/Attendee.cs` (nullable `PersonId`, `ExternalEmail`/`ExternalDisplayName`, `External(...)` factory)
- `src/Infrastructure/Persistence/ApplicationDbContext.cs` (`CalendarConnection` mapping, `Attendee` nullable-FK + check constraint)
- `src/Infrastructure/Persistence/Migrations/ApplicationDbContextModelSnapshot.cs`
- `src/Application/Appointments/IAppointmentRepository.cs` (sync read/write methods)
- `src/Infrastructure/Appointments/AppointmentRepository.cs` (sync read/write implementation)
- `src/Api/Contracts/AppointmentContracts.cs` (`AttendeeSummaryResponse.PersonId` nullable)
- `src/Api/Endpoints/AppointmentEndpoints.cs` (detail endpoint handles external attendees)
- `src/Api/Program.cs` (DI registrations, DataProtection, lazy options)
- `src/Worker/Program.cs` (DbContext + sync service DI wiring, was a bare skeleton)
- `src/Worker/SyncBackgroundService.cs` (real polling loop, was a placeholder)
- `src/Worker/Worker.csproj` (`Microsoft.Extensions.Http` package)
- `deploy/docker-compose.yml` (`worker` service, new env vars on `api`)
- `deploy/.env.example` (Google OAuth + sync-interval env vars)
- `tests/UnitTests/UnitTests.csproj` (Infrastructure project reference)
- `tests/IntegrationTests/Infrastructure/TestApiFactory.cs` (test config for new env vars)
- `frontend/src/app/app.routes.ts` (new `settings/connections` route)
- `frontend/src/app/pages/home/home.ts`, `.html`, `.css` (settings nav link, connect-prompt empty state)
- `frontend/src/app/pages/home/home.spec.ts` (flush the new calendar-connections request; 2 new AC 12 tests)
- `frontend/src/app/pages/home/calendar/calendar.service.ts` (`AttendeeSummary.personId` nullable)
- `frontend/src/app/pages/home/calendar/appointment-detail/appointment-detail.html` (track by email, not personId)
- `frontend/src/app/testing/transloco-testing.ts` (new i18n keys for tests)
- `frontend/public/i18n/de.json`, `en.json` (new `settings.connections` namespace, empty-state keys)
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
