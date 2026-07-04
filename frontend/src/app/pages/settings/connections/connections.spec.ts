import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { getTranslocoTestingModule } from '../../../testing/transloco-testing';
import { CalendarConnectionStatus } from './connection.model';
import { Connections } from './connections';

describe('Connections', () => {
  let fixture: ComponentFixture<Connections>;
  let httpMock: HttpTestingController;

  const notConnected: CalendarConnectionStatus[] = [
    { provider: 'Google', connected: false, lastSuccessfulSyncAt: null, hasError: false, errorCode: null },
    { provider: 'Outlook', connected: false, lastSuccessfulSyncAt: null, hasError: false, errorCode: null },
  ];

  function createComponent(queryParams: Record<string, string> = {}): void {
    TestBed.configureTestingModule({
      imports: [Connections, getTranslocoTestingModule()],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap(queryParams) } } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Connections);
    httpMock = TestBed.inject(HttpTestingController);
  }

  afterEach(() => httpMock.verify());

  it('shows a "Verbinden" link pointing at the OAuth authorize endpoint when Google is not connected', () => {
    createComponent();
    fixture.detectChanges();
    httpMock.expectOne('/api/calendar-connections').flush(notConnected);
    fixture.detectChanges();

    const connectLink = fixture.nativeElement.querySelector('.ghost-btn--primary') as HTMLAnchorElement;
    expect(connectLink).not.toBeNull();
    expect(connectLink.getAttribute('href')).toBe('/api/calendar-connections/google/authorize');
  });

  it('shows the connected status and last-sync caption once Google is connected (AC 9)', () => {
    createComponent();
    fixture.detectChanges();
    httpMock.expectOne('/api/calendar-connections').flush([
      {
        provider: 'Google',
        connected: true,
        lastSuccessfulSyncAt: new Date(Date.now() - 3 * 60_000).toISOString(),
        hasError: false,
        errorCode: null,
      },
      notConnected[1],
    ]);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.status-tag--ok')).not.toBeNull();
    expect(el.textContent).toContain('Zuletzt synchronisiert vor 3 Min.');
  });

  it('shows the explicit tenant-blocked error text (not a generic failure message) using dnd-text styling (AC 10)', () => {
    createComponent();
    fixture.detectChanges();
    httpMock.expectOne('/api/calendar-connections').flush([
      { provider: 'Google', connected: false, lastSuccessfulSyncAt: null, hasError: true, errorCode: 'tenant_blocked' },
      notConnected[1],
    ]);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Verbindung von deinem Unternehmen blockiert');
    expect(el.querySelector('.sync-line--err')).not.toBeNull();
  });

  it('shows the repeated-sync-failure error state for a previously-connected account (AC 11)', () => {
    createComponent();
    fixture.detectChanges();
    httpMock.expectOne('/api/calendar-connections').flush([
      {
        provider: 'Google',
        connected: true,
        lastSuccessfulSyncAt: new Date(Date.now() - 60 * 60_000).toISOString(),
        hasError: true,
        errorCode: 'token_refresh_failed',
      },
      notConnected[1],
    ]);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.status-tag--err')).not.toBeNull();
    expect(el.querySelector('.sync-line--err')).not.toBeNull();
  });

  it('shows a one-time success notice after redirecting back with ?connected=google', () => {
    createComponent({ connected: 'google' });
    fixture.detectChanges();
    httpMock.expectOne('/api/calendar-connections').flush(notConnected);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Google-Kalender erfolgreich verbunden.');
  });

  it('shows the mapped error notice after redirecting back with ?error=consent_denied', () => {
    createComponent({ error: 'consent_denied' });
    fixture.detectChanges();
    httpMock.expectOne('/api/calendar-connections').flush(notConnected);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Verbindung abgelehnt');
  });

  it('shows a translated message (not a raw i18n key) for the unknown_error code (code review regression)', () => {
    createComponent();
    fixture.detectChanges();
    httpMock.expectOne('/api/calendar-connections').flush([
      {
        provider: 'Google',
        connected: true,
        lastSuccessfulSyncAt: new Date(Date.now() - 60 * 60_000).toISOString(),
        hasError: true,
        errorCode: 'unknown_error',
      },
      notConnected[1],
    ]);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).not.toContain('settings.connections.error.unknown_error');
    expect(el.textContent).toContain('Synchronisierung fehlgeschlagen');
  });

  it('shows a "Verbinden" link pointing at the Outlook OAuth authorize endpoint when Outlook is not connected (Story 2.2)', () => {
    createComponent();
    fixture.detectChanges();
    httpMock.expectOne('/api/calendar-connections').flush(notConnected);
    fixture.detectChanges();

    const connectLinks = fixture.nativeElement.querySelectorAll('.ghost-btn--primary') as NodeListOf<HTMLAnchorElement>;
    const outlookLink = Array.from(connectLinks).find((a) => a.href.includes('/outlook/'));
    expect(outlookLink).toBeDefined();
    expect(outlookLink!.getAttribute('href')).toBe('/api/calendar-connections/outlook/authorize');
  });

  it('shows the connected status for Outlook once connected (Story 2.2)', () => {
    createComponent();
    fixture.detectChanges();
    httpMock.expectOne('/api/calendar-connections').flush([
      notConnected[0],
      {
        provider: 'Outlook',
        connected: true,
        lastSuccessfulSyncAt: new Date(Date.now() - 5 * 60_000).toISOString(),
        hasError: false,
        errorCode: null,
      },
    ]);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Outlook Calendar');
    expect(el.textContent).toContain('Zuletzt synchronisiert vor 5 Min.');
  });

  it('shows a one-time success notice after redirecting back with ?connected=outlook (Story 2.2)', () => {
    createComponent({ connected: 'outlook' });
    fixture.detectChanges();
    httpMock.expectOne('/api/calendar-connections').flush(notConnected);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Outlook-Kalender erfolgreich verbunden.');
  });

  it('shows a "Verbindung trennen" button for a connected account, not for an unconnected one', () => {
    createComponent();
    fixture.detectChanges();
    httpMock.expectOne('/api/calendar-connections').flush([
      { provider: 'Google', connected: true, lastSuccessfulSyncAt: null, hasError: false, errorCode: null },
      notConnected[1],
    ]);
    fixture.detectChanges();

    const disconnectButtons = fixture.nativeElement.querySelectorAll('.ghost-btn--danger');
    expect(disconnectButtons.length).toBe(1);
  });

  it('disconnects and reloads the connection list after confirming', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    createComponent();
    fixture.detectChanges();
    httpMock.expectOne('/api/calendar-connections').flush([
      { provider: 'Google', connected: true, lastSuccessfulSyncAt: null, hasError: false, errorCode: null },
      notConnected[1],
    ]);
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.ghost-btn--danger') as HTMLButtonElement).click();

    httpMock.expectOne({ url: '/api/calendar-connections/google', method: 'DELETE' }).flush(null);
    fixture.detectChanges();
    httpMock.expectOne('/api/calendar-connections').flush(notConnected);
    fixture.detectChanges();

    expect(window.confirm).toHaveBeenCalled();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelectorAll('.ghost-btn--danger').length).toBe(0);
  });

  it('does not call the API when the disconnect confirmation is cancelled', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false);
    createComponent();
    fixture.detectChanges();
    httpMock.expectOne('/api/calendar-connections').flush([
      { provider: 'Google', connected: true, lastSuccessfulSyncAt: null, hasError: false, errorCode: null },
      notConnected[1],
    ]);
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.ghost-btn--danger') as HTMLButtonElement).click();

    httpMock.expectNone({ url: '/api/calendar-connections/google', method: 'DELETE' });
  });
});
