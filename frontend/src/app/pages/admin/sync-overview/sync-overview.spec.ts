import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { getTranslocoTestingModule } from '../../../testing/transloco-testing';
import { SyncOverview } from './sync-overview';

describe('SyncOverview', () => {
  let fixture: ComponentFixture<SyncOverview>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SyncOverview, getTranslocoTestingModule()],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(SyncOverview);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('renders one row per person/provider combination, including a not-connected person (AC 1, AC 5)', () => {
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/calendar-connections').flush([
      {
        personId: 'p1',
        personEmail: 'dennis@example.com',
        provider: 'Google',
        connected: true,
        lastSuccessfulSyncAt: new Date(Date.now() - 2 * 60_000).toISOString(),
        hasError: false,
        errorCode: null,
      },
      { personId: 'p1', personEmail: 'dennis@example.com', provider: 'Outlook', connected: false, lastSuccessfulSyncAt: null, hasError: false, errorCode: null },
      { personId: 'p2', personEmail: 'jonas@example.com', provider: 'Google', connected: false, lastSuccessfulSyncAt: null, hasError: false, errorCode: null },
      { personId: 'p2', personEmail: 'jonas@example.com', provider: 'Outlook', connected: false, lastSuccessfulSyncAt: null, hasError: false, errorCode: null },
    ]);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('dennis@example.com');
    expect(el.textContent).toContain('jonas@example.com');
    expect(el.querySelectorAll('tbody tr').length).toBe(4);
    expect(el.querySelector('.status-tag--ok')).not.toBeNull();
  });

  it('highlights a repeatedly-failing account with the explicit error state, not just a stale timestamp (AC 4)', () => {
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/calendar-connections').flush([
      {
        personId: 'p1',
        personEmail: 'dennis@example.com',
        provider: 'Google',
        connected: true,
        lastSuccessfulSyncAt: new Date(Date.now() - 60 * 60_000).toISOString(),
        hasError: true,
        errorCode: 'token_refresh_failed',
      },
    ]);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.status-tag--err')).not.toBeNull();
    expect(el.querySelector('.sync-overview-table__row--error')).not.toBeNull();
  });

  it('shows a forbidden notice instead of a blank table when the API returns 403 (AC 3)', () => {
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/calendar-connections').flush(
      { code: 'forbidden' },
      { status: 403, statusText: 'Forbidden' }
    );
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('table')).toBeNull();
    expect(el.textContent).toContain('Admins');
  });
});
