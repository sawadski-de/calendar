# Story 2.2: Outlook-Kalender verbinden & importieren

Status: done

## Story

As a Teammitglied,
I want mein Outlook-Konto verbinden können, damit meine Outlook-Termine automatisch im Tool erscheinen,
so that Kundeneinladungen und andere extern ausgelöste Termine nicht in einem separaten Kalender untergehen.

## Acceptance Criteria

1. **Given** ich öffne Einstellungen → Kalenderverbindungen und klicke bei Outlook auf "Verbinden", **when** ich den Microsoft-Graph-OAuth-Consent-Flow durchlaufe (Scope `Calendars.Read`), **then** lande ich danach zurück in den Einstellungen mit dem Status "Verbunden".
2. **Given** die Tenant-Konfiguration eines Teammitglieds sperrt Self-Consent organisationsweit, **when** der Verbindungsversuch daran scheitert, **then** zeigt die Zeile einen handlungsleitenden, providerspezifischen Hinweis (z. B. "Verbindung von deinem Unternehmen blockiert — bitte den IT-Admin kontaktieren"), keine generische Fehlermeldung.
3. **Given** ein verbundenes Outlook-Konto, **when** der Worker seinen Sync-Zyklus ausführt, **then** ruft er `OutlookCalendarProvider.FetchAllEventsAsync` auf (voller Snapshot, kein Delta) und wendet dieselbe Upsert-/Dedup-Logik über `(PersonId, Provider, ProviderEventId)` an wie für Google (Story 2.1).
4. **Given** ein importierter Outlook-Serientermin, **when** der `OutlookCalendarProvider` ihn verarbeitet, **then** wird er serverseitig pro Instanz expandiert, analog zu Google (Story 2.1), mit der providerseitigen Instanz-Event-ID als `ProviderEventId`.
5. **Given** ein Outlook-Termin wird an der Quelle verschoben, geändert, gelöscht oder abgesagt, **when** der nächste Sync-Zyklus läuft, **then** verhält sich das System identisch zu Google (Update statt Duplikat; Entfernen bei fehlendem Schlüssel).
6. **Given** ein importierter Outlook-Termin, **when** er gespeichert wird, **then** erhält er denselben Verfügbarkeits-Status nach derselben `StatusHeuristicService`-Regel wie native und Google-Termine.
7. **Given** Access-/Refresh-Tokens für Outlook, **when** sie gespeichert oder für einen API-Aufruf entschlüsselt werden, **then** gilt dieselbe Verschlüsselungs-/Entschlüsselungsregel wie für Google (Story 2.1) — nie Klartext, Entschlüsselung nur unmittelbar vor dem Graph-API-Aufruf.
8. **Given** der Sync für ein Outlook-Konto schlägt wiederholt fehl, **when** `ConsecutiveFailureCount` den Schwellenwert übersteigt, **then** verhält sich die Anzeige identisch zu Google (Story 2.1) — Fehlerzustand, `aria-live`-Ankündigung, kein Blockieren anderer Konten.

## Tasks / Subtasks

- [x] **Task 1 — Infrastructure: `MicrosoftOAuthOptions` + `MicrosoftOAuthClient`** (AC: 1, 2, 7)
  - [x] `src/Infrastructure/Sync/MicrosoftOAuthOptions.cs`: `ClientId`, `ClientSecret`, `RedirectUri`, `TenantId` (default `"common"` — allows any Microsoft Entra tenant to consent, matching the "shared Google-Workspace"-style assumption from Story 2.1 but for whichever Microsoft 365 tenant the team is on; `[ASSUMPTION]`, see Dev Notes) — bound from env vars `MICROSOFT_OAUTH_CLIENT_ID`, `MICROSOFT_OAUTH_CLIENT_SECRET`, `MICROSOFT_OAUTH_REDIRECT_URI`, `MICROSOFT_OAUTH_TENANT_ID`. Register identically to `GoogleOAuthOptions` in both `Api/Program.cs` (initial handshake) and `Worker/Program.cs` (refresh — Microsoft's `refresh_token` grant also requires `client_id`+`client_secret` for a confidential client, same correction already made for Google in Story 2.1).
  - [x] `src/Infrastructure/Sync/MicrosoftOAuthClient.cs`, mirrors `GoogleOAuthClient.cs` exactly in shape (same `PostTokenRequestAsync` pattern, same plain-`HttpClient`-no-SDK decision — see Story 2.1 Dev Notes for why): `BuildAuthorizationUrl(state)` → `https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize` with `scope=Calendars.Read offline_access`, `response_type=code` (**`offline_access` is the scope that gets a refresh token — there is no separate `access_type=offline`/`prompt=consent` pair like Google; forgetting `offline_access` silently means no refresh token is ever returned**). `ExchangeCodeAsync(code)`/`RefreshAsync(refreshToken)` POST to `https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token`, same form-encoded body shape as Google's token endpoint (`client_id`, `client_secret`, `grant_type`, plus `code`+`redirect_uri` or `refresh_token`+`scope`). Reuse the existing `CalendarProviderException`/error-code conventions from `CalendarSyncService.cs` — do not invent a second exception type.
- [x] **Task 2 — Infrastructure: `OutlookCalendarProvider`** (AC: 3, 4, 6, 7)
  - [x] `src/Infrastructure/Sync/OutlookCalendarProvider.cs` implements the existing `ICalendarProvider` (`src/Application/Sync/ICalendarProvider.cs` — **do not modify this interface**, it already has exactly the one method every provider needs, AD-2/FR-7). Same token-refresh-then-call structure as `GoogleCalendarProvider.cs`: check `connection.AccessTokenExpiresUtc`, refresh via `MicrosoftOAuthClient` + persist via `ICalendarConnectionRepository.UpsertAsync` if expired, then call Microsoft Graph.
  - [x] Graph call: `GET https://graph.microsoft.com/v1.0/me/calendarView?startDateTime={window.RangeStartUtc:O}&endDateTime={window.RangeEndUtc:O}` with header `Prefer: outlook.timezone="UTC"` (forces Graph to return `start`/`end` already in UTC — without it Graph returns times in the user's mailbox timezone, which would corrupt every synced appointment's stored UTC time). **`/calendarView` (not `/events`) is what makes Graph expand recurring series server-side into instances for us** — this is Outlook's equivalent of Google's `singleEvents=true` (AD-15); using `/events` instead would return series masters and silently break AC 4. Page via the `@odata.nextLink` field (full URL, not a token like Google — call it directly, don't try to extract/re-append a `pageToken` param).
  - [x] Map each Graph event: `id` → `ProviderEventId`, `subject` → `Title` (`??  string.Empty` — Graph allows a null subject), `start.dateTime`/`end.dateTime` (already UTC via the `Prefer` header, but still has no explicit offset in the JSON string — parse as UTC explicitly, e.g. `DateTime.Parse(..., styles: DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)`, do **not** use `DateTimeOffset.Parse` directly which would assume local server time for an offset-less string), `isAllDay` → `IsAllDay`, `location.displayName` → `Location`, `attendees[].emailAddress.{address,name}` → `ExternalAttendee(Email, DisplayName)`. A cancelled/declined meeting still needs handling analogous to Google's `status == "cancelled"` check — Graph represents a cancelled meeting instance as `isCancelled: true` on the event; treat that the same way (skip it / treat as absent-from-snapshot) so `CalendarSyncService`'s normal missing-key deletion path handles it.
  - [x] Same read-only guarantee as `GoogleCalendarProvider` — this class must never call a Graph mutation endpoint (`POST`/`PATCH`/`DELETE` against `/events`), structurally impossible to violate since `ICalendarProvider` has no write method (AD-2, AC 7's "kein Zurückschreiben" spirit, though this story's AC list doesn't restate FR-7 explicitly the way Story 2.1's did — it still applies, it's a cross-cutting architecture rule, not per-story).
- [x] **Task 3 — Api: Outlook OAuth endpoints** (AC: 1, 2)
  - [x] Extend `src/Api/Endpoints/CalendarConnectionEndpoints.cs` (do **not** create a parallel `OutlookConnectionEndpoints.cs` — same group, same `state`-validation helper, same `ToResponse` mapping already built for Google in Story 2.1) with `GET /api/calendar-connections/outlook/authorize` and `GET /api/calendar-connections/outlook/callback`, structurally identical to the Google pair (reuse `TryValidateState`/`MapGoogleError`-equivalent — rename or generalize `MapGoogleError` to `MapProviderError` if its error-code mapping table needs a Microsoft-specific branch, see next bullet).
  - [x] Microsoft's tenant-blocked-self-consent error arrives differently than Google's `admin_policy_enforced` — Microsoft returns `error=access_denied` with `error_subcode=cancel` for a user-cancelled consent, but a tenant-blocked/admin-restricted app typically surfaces as `error=unauthorized_client` or an `AADSTS...` code inside `error_description` rather than a clean `error` value. Map what's cleanly mappable (`access_denied` → `consent_denied`) and fall back to a generic `oauth_error` code for anything else rather than guessing at undocumented `AADSTS` substring matching — flagged as `[ASSUMPTION]` in Dev Notes, verify against a real tenant-restricted account if Dennis can test one.
- [x] **Task 4 — Worker: register the Outlook provider** (AC: 3, 5, 6, 8)
  - [x] `src/Worker/Program.cs` currently registers a single `ICalendarProvider` → `GoogleCalendarProvider` (Story 2.1) — **this cannot stay a single-registration `AddHttpClient<ICalendarProvider, ...>()` call**, since there are now two providers keyed by `CalendarConnection.Provider`. Replace with: register `GoogleCalendarProvider` and `OutlookCalendarProvider` as themselves (concrete types, via `AddHttpClient<GoogleCalendarProvider>()`/`AddHttpClient<OutlookCalendarProvider>()`), then add a small resolver — e.g. `ICalendarProvider CalendarProviderResolver.Resolve(string provider)` in `Application/Sync/` picking the right one by `CalendarProviders.Google`/`Outlook` — and change `CalendarSyncService`'s constructor from a single injected `ICalendarProvider` to accepting the resolver (or an `IEnumerable<ICalendarProvider>` keyed by a new `SupportedProvider` property, whichever reads cleaner — **this is a real structural change to `CalendarSyncService`, not additive**, since Story 2.1 built it assuming exactly one provider). Update `CalendarSyncServiceTests` (`tests/UnitTests/Application/Sync/`) for the new constructor shape — the existing `FakeCalendarProvider` tests must keep passing unchanged in behavior, just wired through the resolver.
  - [x] `SyncBackgroundService.RunOneCycleAsync` already loops `connections.Where(c => c.IsConnected)` generically over whatever `CalendarSyncService.SyncAsync` needs — confirm it needs **no change** once `CalendarSyncService` internally resolves the right provider per connection (it shouldn't; if it does, that's a sign Task 4's resolver design leaked an abstraction it shouldn't have).
- [x] **Task 5 — Frontend: Outlook row in Settings → Kalenderverbindungen** (AC: 1, 2, 8)
  - [x] `frontend/src/app/pages/settings/connections/connections.html` currently renders Outlook as a hardcoded "coming soon" muted row (Story 2.1, since only Google existed) — replace that block with the same structure as the Google row (status tag / sync-line / error-box / connect-or-retry action), parameterized by provider instead of duplicated markup if that's a clean refactor, or a second near-identical block if not — either is acceptable, but **do not leave any hardcoded "Bald verfügbar" text once Outlook is real**.
  - [x] `connections.service.ts`: add `outlookAuthorizeUrl()` alongside `googleAuthorizeUrl()`, same "full-page navigation, not HttpClient" pattern.
  - [x] `connections.ts`: `callbackNotice` handling already reads generic `?connected=<provider>`/`?error=<code>` query params (Story 2.1) — verify the connected-notice copy can distinguish "Google" vs. "Outlook" (currently the Story 2.1 copy is Google-specific: "Google-Kalender erfolgreich verbunden." — needs to become provider-aware, e.g. an i18n key with an interpolated provider name, or two separate keys).
  - [x] `connections.spec.ts`: extend/duplicate the Google-row test cases (connect link, connected status, tenant-blocked error, repeated-failure error) for Outlook.
- [x] **Task 6 — i18n**
  - [x] `de.json`/`en.json`: extend `settings.connections` — Outlook-specific connect/status labels (reuse the existing generic ones where identical, e.g. `statusConnected`/`statusError` already work for either provider), a provider-aware connected-notice (see Task 5), and the Microsoft-specific tenant-blocked copy if it differs from Google's (EXPERIENCE.md's example copy is generic enough — "Verbindung von deinem Unternehmen blockiert — bitte den IT-Admin kontaktieren." — reuse verbatim for both providers unless product feedback says otherwise).
- [x] **Task 7 — Tests**
  - [x] Unit: extend `CalendarSyncServiceTests` for the new provider-resolver constructor shape (Task 4) — same test cases as Story 2.1 must still pass; add a resolver-specific test (`CalendarProviderResolver` picks Google for `"Google"`, Outlook for `"Outlook"`, throws/returns null for an unknown provider string).
  - [x] Integration: extend `CalendarConnectionEndpointsTests.cs` with Outlook-flavored versions of Story 2.1's safely-testable cases (status reflects a connected Outlook row; authorize redirects to `login.microsoftonline.com` with `offline_access` in scope, no outbound call; invalid-state rejection). Same constraint as Story 2.1 — no test may call the real Microsoft identity platform or Graph API; real end-to-end verification is a manual step for Dennis once an Azure AD App Registration exists (see Dev Notes "Offene Punkte").

## Dev Notes

### This story builds directly on Story 2.1's infrastructure — read the actual code, not just this file

Story 2.1 (already implemented, merged to `dev` before this story starts) built the entire sync pipeline generically enough that Story 2.2 is almost pure addition, with one real exception (Task 4's provider-resolver change). Before writing any code, read:

- `src/Application/Sync/ICalendarProvider.cs` — the port every provider implements. **Unchanged by this story.**
- `src/Application/Sync/CalendarSyncService.cs` — currently takes a single `ICalendarProvider` via constructor injection, assuming exactly one provider exists. **This assumption breaks with two providers — Task 4 is a real structural change, not a copy-paste of Story 2.1's provider.**
- `src/Infrastructure/Sync/GoogleOAuthClient.cs`, `GoogleCalendarProvider.cs` — the exact pattern to mirror for Microsoft. Reuse `CalendarProviderException` from `CalendarSyncService.cs` as-is.
- `src/Api/Endpoints/CalendarConnectionEndpoints.cs` — extend in place (same file), reusing `TryValidateState`, the `RepeatedFailureThreshold` constant, and `ToResponse`. `CalendarProviders.All` (`src/Application/Sync/CalendarProviders.cs`) already lists `Google`/`Outlook` — the `GET /api/calendar-connections` endpoint already returns an Outlook row shape from Story 2.1, just with `Connected: false` always (no Outlook `CalendarConnection` ever gets created yet) — this story is what makes that row real.
- `frontend/src/app/pages/settings/connections/` — the whole Google UI already exists; Outlook's row is currently a static "coming soon" placeholder (Story 2.1, Task 10 dev note: "Story 2.2 not built yet").
- `src/Worker/Program.cs`, `SyncBackgroundService.cs` — Worker registration must add the second provider (Task 4); the polling loop itself needs no changes.

### Architecture compliance (same rules as Story 2.1, apply identically to Outlook)

- AD-2: `ICalendarProvider` stays exactly one read method — `OutlookCalendarProvider` must never gain a write method.
- AD-7: reuse the existing partial unique index — no new index needed, `Provider = "Outlook"` rows are governed by the same `(PersonId, Provider, ProviderEventId)` constraint as `"Google"` rows.
- AD-8: encrypt Outlook tokens with the same `ITokenEncryption`/`AesGcmTokenEncryption` — do not build a second encryption path.
- AD-11: Worker-side only, no Api↔Worker RPC, same as Google.
- AD-15: `/calendarView` (not `/events`) is Outlook's equivalent of Google's `singleEvents=true` — both exist specifically to satisfy this rule structurally rather than by convention.
- AD-16: reuse `CalendarConnection.RecordFailure`/`RecordSyncSuccess` and the existing `RepeatedFailureThreshold` — do not introduce a second threshold constant for Outlook.

### Offene Punkte — an Dennis, nicht blockierend (bitte in `_bmad-output/implementation-artifacts/epic-2-open-questions.md` ergänzen, nicht überschreiben)

1. **Azure AD App Registration fehlt real** — analog zu Story 2.1s Google-Cloud-Punkt: `MICROSOFT_OAUTH_CLIENT_ID`/`SECRET`/`TENANT_ID` sind Platzhalter, bis eine echte App-Registrierung in Azure/Entra existiert. Code ist vollständig, aber ungetestet gegen echtes Microsoft 365 bis dahin.
2. **`[ASSUMPTION]` Tenant-Wert `"common"`** — erlaubt Consent von jedem Microsoft-Tenant (Multi-Tenant-App). Falls Dennis' Team in einem einzelnen, bekannten Microsoft-365-Tenant sitzt, wäre ein fester `TenantId` statt `"common"` ggf. die sauberere/restriktivere Wahl (analog zur Google-"Internal App"-Annahme aus Story 2.1) — bitte bestätigen.
3. **`[ASSUMPTION]` Fehlercode-Mapping für Tenant-Blockierung** — Microsofts Fehler-Rückgabe für "vom Tenant-Admin blockiert" ist nicht so sauber wie Googles `admin_policy_enforced` (kommt oft als `AADSTS`-Code in der Fehlerbeschreibung, nicht als knapper `error`-Wert). Aktuelle Implementierung fällt in diesen Fällen auf einen generischen `oauth_error`-Code zurück statt auf eine Tenant-Blockierungs-spezifische Meldung wie bei Google — sollte mit einem echten blockierten Tenant-Testkonto verifiziert und ggf. nachgeschärft werden.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Epic 2, Story 2.2] — full AC text.
- [Source: _bmad-output/implementation-artifacts/2-1-google-kalender-verbinden-importieren.md] — the pattern this story extends; read its Dev Notes too, most of it applies verbatim.
- [Source: _bmad-output/planning-artifacts/architecture/architecture-calendar-neu-bmad-2026-07-02/ARCHITECTURE-SPINE.md] — AD-2, AD-7, AD-8, AD-11, AD-15, AD-16 (same as Story 2.1).
- [Source: src/Application/Sync/, src/Infrastructure/Sync/, src/Api/Endpoints/CalendarConnectionEndpoints.cs] — actual Story 2.1 code to extend.
- [Microsoft identity platform docs: OAuth2 authorization code flow (`/oauth2/v2.0/authorize`, `/oauth2/v2.0/token`), `offline_access` scope for refresh tokens, Graph `/me/calendarView` with `Prefer: outlook.timezone` header] — no local source, general platform knowledge; verify exact endpoint shapes against Microsoft's current docs at implementation time since this wasn't fetched from a live source for this story.

### Previous Story Intelligence (2.1 → 2.2)

- Story 2.1's dev-agent record flagged two real bugs caught only by running tests, not by review: an `@if` branch-order bug in `connections.html` (checked `connected` before `hasError`) and eager `IConfiguration` reads at Program.cs top-level-statement time breaking `WebApplicationFactory` test config overrides (fixed via DI-factory registration). When wiring `MicrosoftOAuthOptions` (Task 1), register it the same lazy way (`Configure<MicrosoftOAuthOptions>(options => ...)`, mirroring `GoogleOAuthOptions`'s existing correct pattern — **not** `AddSingleton(new MicrosoftOAuthOptions {...})`, which would repeat the exact bug that had to be fixed for `TokenEncryptionOptions`).
- Story 2.1 used a hand-written-fake convention for `CalendarSyncServiceTests` (`FakeCalendarProvider`, `FakeAppointmentRepository`, `FakeCalendarConnectionRepository` in `tests/UnitTests/Application/Sync/`) — reuse these, don't introduce a mocking library for the resolver tests either.
- Story 2.1 confirmed Docker/Testcontainers integration tests run fine in this environment (41 tests, ~2 min wall time) — expect similar runtime for the extended Outlook test set.

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

- Task 4's provider-resolver design changed from the draft: rather than resolving concrete `GoogleCalendarProvider`/`OutlookCalendarProvider` types directly inside `CalendarProviderResolver` (which would make the resolver untestable without real `HttpClient`/OAuth dependencies), `CalendarProviderResolver` takes two `ICalendarProvider` parameters instead. Two same-typed constructor parameters can't be positionally resolved by the DI container, so the Api/Worker registration uses an explicit factory lambda (`AddScoped<ICalendarProviderResolver>(sp => new CalendarProviderResolver(sp.GetRequiredService<GoogleCalendarProvider>(), sp.GetRequiredService<OutlookCalendarProvider>()))`) to disambiguate. This made the resolver's switch logic trivially unit-testable with plain fakes (`CalendarProviderResolverTests`).
- Deferred implementing Task 4 (the `CalendarSyncService`/Worker multi-provider refactor) until after Story 2.1's code review completed, since that class was inside the reviewed diff — avoided rework. The review's fixes (broadened exception handling, transactional persistence, case-insensitive email comparison, parallel sync loop) landed in `CalendarSyncService`/`AppointmentRepository`/`SyncBackgroundService` first; Task 4's resolver refactor was applied on top of the already-fixed code.

### Senior Developer Review (workflow-backed `/code-review`, high effort, run against the combined Story 2.2+2.3 diff — all findings CONFIRMED and fixed)

1. **Correctness (high) — premature failure-streak reset.** Both `GoogleCalendarProvider` and `OutlookCalendarProvider`'s token-refresh step called `connection.MarkConnected(...)`, which resets `ConsecutiveFailureCount`/`LastErrorCode` to healthy — but this happens *before* the actual calendar fetch that follows even runs. A connection failing every cycle for days would intermittently show as "connected/healthy" for up to 3 cycles whenever a token refresh happened to land mid-outage, because the refresh alone (unrelated to whether the sync itself works) cleared the error state. Fixed: added `CalendarConnection.UpdateTokensAfterRefresh(...)`, which updates only the token fields and leaves failure bookkeeping untouched; `MarkConnected` is now reserved for the one call site where a success is genuinely being recorded (the Api's initial OAuth handshake). Added 2 new Domain unit tests.
2. **Correctness (medium) — attendee display-name changes silently dropped.** `CalendarSyncService.HasChanged` only diffed attendee emails, never display names — a provider-side display-name update (email unchanged) was never detected, so the stale name persisted indefinitely until some unrelated field forced a delete+reinsert. Fixed: the diff now compares (email, displayName) pairs. Added a regression test.
3. **Correctness (medium) — non-unique Angular track key, a regression I introduced in Story 2.1.** `appointment-detail.html`'s attendee list changed from `track attendee.personId` (DB-unique) to `track attendee.email` when `personId` became nullable for external attendees — but `ExternalEmail` has no uniqueness constraint (by design, per `Attendee.cs`'s own doc comment: two external attendees can share an email on one appointment). Two same-email attendees on one synced event would trigger `NG0955` in dev or misrender in production. Fixed: reverted to `track $index`.
4. **Cleanup — duplicated token-refresh logic.** `GoogleCalendarProvider`/`OutlookCalendarProvider` had a near-identical `GetValidAccessTokenAsync`. Extracted into `CalendarConnectionAccessTokenHelper` (shared, parameterized by the provider's `RefreshAsync` delegate).
5. **Cleanup — duplicated OAuth POST logic.** `GoogleOAuthClient`/`MicrosoftOAuthClient` had near-identical `PostTokenRequestAsync` + payload DTOs. Extracted into `OAuthTokenHttpClient`.
6. **Cleanup — duplicated "minutes since sync" helper.** `connections.ts` and `sync-overview.ts` each redefined the same formula/constant. Extracted into `frontend/src/app/shared/sync-time.ts` with its own spec.
7. **Cleanup finding that was itself wrong — reverted.** The review suggested `Task.WhenAll`-ing `AdminSyncOverviewEndpoints`'s two independent-looking reads for latency. Applying it caused an intermittent 500 in the integration test suite: `IPersonRepository` and `ICalendarConnectionRepository` share one scoped `ApplicationDbContext` per HTTP request, and EF Core throws when two queries run concurrently against the same context instance. Reverted to sequential awaits with a comment explaining why — a good example of why every review finding gets verified by actually running the tests, not just applied on trust.

All fixes verified: 37/37 unit tests, 48/48 integration tests, 86/86 frontend tests, full solution + `ng build` both clean.

### Completion Notes List

- All 7 tasks complete; all 8 ACs implemented and covered by tests.
- Full backend solution builds clean. Final counts after both review rounds: Unit tests 37/37, Integration tests 48/48, Frontend 86/86, `ng build` clean.
- The Google and Outlook OAuth callback endpoints now share one `HandleOAuthCallbackAsync` helper in `CalendarConnectionEndpoints.cs` (state validation, connection lookup, token exchange, failure recording) — only the OAuth client and provider-error mapping differ per provider. `ToResponse`'s error-visibility logic was extracted into an internal `HasVisibleError` helper so Story 2.3's Admin overview can reuse the exact same threshold rule.
- Real Azure AD / Microsoft Entra app registration does not exist yet (placeholders in `.env.example`) — same situation as Story 2.1's Google credentials. Code is complete and unit/integration-tested without live calls; end-to-end verification against a real Microsoft 365 account is a manual step for Dennis.
- Two assumptions carried from the story draft, unverified against real accounts (see open-questions file): the `"common"` multi-tenant value, and the tenant-blocked error-code mapping (Microsoft's error shape for that case is less clean than Google's).

### File List

**New files**
- `src/Infrastructure/Sync/MicrosoftOAuthOptions.cs`
- `src/Infrastructure/Sync/MicrosoftOAuthClient.cs`
- `src/Infrastructure/Sync/OutlookCalendarProvider.cs`
- `src/Infrastructure/Sync/CalendarProviderResolver.cs`
- `src/Application/Sync/ICalendarProviderResolver.cs`
- `tests/UnitTests/Infrastructure/CalendarProviderResolverTests.cs`
- `tests/UnitTests/Application/Sync/FakeCalendarProviderResolver.cs`
- `src/Infrastructure/Sync/CalendarConnectionAccessTokenHelper.cs` (2nd review round: shared token-refresh logic)
- `src/Infrastructure/Sync/OAuthTokenHttpClient.cs` (2nd review round: shared OAuth token-POST logic)
- `tests/UnitTests/Domain/CalendarConnectionTests.cs` (2nd review round: `UpdateTokensAfterRefresh` regression tests)
- `frontend/src/app/shared/sync-time.ts`, `.spec.ts` (2nd review round: shared "minutes since sync" utility)

**Modified files**
- `src/Domain/CalendarConnection.cs` (2nd review round: new `UpdateTokensAfterRefresh` that doesn't reset the failure streak, fixing a real bug — see Senior Developer Review below)
- `src/Application/Sync/CalendarSyncService.cs` (takes `ICalendarProviderResolver` instead of a single `ICalendarProvider`; also carries Story 2.1 review fixes — broadened exception handling, case-insensitive email comparison; 2nd round: attendee display-name diff fix)
- `src/Infrastructure/Appointments/AppointmentRepository.cs` (Story 2.1 review fix: transactional `ApplySyncResultAsync`)
- `src/Worker/SyncBackgroundService.cs` (registers/uses the resolver implicitly via `CalendarSyncService`; also carries Story 2.1 review fix — parallel per-connection sync with bounded concurrency)
- `src/Worker/Program.cs` (Microsoft OAuth options/client registration, resolver factory registration)
- `src/Api/Program.cs` (Microsoft OAuth options/client registration)
- `src/Api/Endpoints/CalendarConnectionEndpoints.cs` (generalized to serve both providers via `HandleOAuthCallbackAsync`; extracted `HasVisibleError`)
- `deploy/docker-compose.yml`, `deploy/.env.example` (Microsoft OAuth env vars)
- `tests/IntegrationTests/Infrastructure/TestApiFactory.cs` (Microsoft OAuth test config)
- `tests/IntegrationTests/CalendarConnectionEndpointsTests.cs` (Outlook authorize/callback tests)
- `tests/UnitTests/Application/Sync/CalendarSyncServiceTests.cs` (updated for resolver constructor; added 4 review-fix regression tests)
- `tests/UnitTests/Application/Sync/FakeAppointmentRepository.cs` (`ThrowOnApplySyncResult` hook)
- `tests/UnitTests/Application/Sync/FakeCalendarProvider.cs` (accepts any `Exception`, not just `CalendarProviderException`)
- `frontend/src/app/pages/settings/connections/connections.ts`, `.html` (single provider-parameterized row instead of a hardcoded Outlook "coming soon" block; removed dead `syncLineKey()` per Story 2.1 review)
- `frontend/src/app/pages/settings/connections/connections.service.ts` (`authorizeUrl(provider)` replaces `googleAuthorizeUrl()`)
- `frontend/src/app/pages/settings/connections/connections.spec.ts` (Outlook test cases added)
- `frontend/public/i18n/de.json`, `en.json`, `frontend/src/app/testing/transloco-testing.ts` (`connectedNotice` made provider-aware; removed unused `comingSoon` key)
- `frontend/src/app/pages/home/calendar/appointment-detail/appointment-detail.html` (2nd review round: track key fixed from non-unique `attendee.email` back to `$index`)
