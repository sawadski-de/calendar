import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { getTranslocoTestingModule } from '../../testing/transloco-testing';
import { CalendarColumn } from './calendar/calendar-column/calendar-column';
import { addDays, getMonthGridDays } from './calendar/date-utils';
import { Home } from './home';

describe('Home', () => {
  let fixture: ComponentFixture<Home>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Home, getTranslocoTestingModule()],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(Home);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  // Home now also loads calendar-connections status (Story 2.1, AC 12) and the current person's role
  // (Story 2.3, Admin nav gating) on init — every test's first detectChanges() triggers both requests
  // alongside /api/appointments.
  function flushConnections(connections: unknown[] = [{ provider: 'Google', connected: true }]): void {
    httpMock.expectOne('/api/calendar-connections').flush(connections);
  }

  function flushMe(role: 'Admin' | 'Member' = 'Member'): void {
    httpMock.expectOne('/api/auth/me').flush({ id: '1', email: 'a@b.com', role });
  }

  // Story 3.1: Home now also fetches the team roster once on init (reused by both the person-selector
  // and colleague-column labels) — every test's first detectChanges() triggers this alongside the
  // existing connections/me/appointments requests.
  function flushRoster(people: { id: string; email: string }[] = []): void {
    httpMock.expectOne('/api/persons').flush(people);
  }

  it('shows the empty-state message when there are no appointments (AC 3)', () => {
    fixture.detectChanges();
    flushConnections();
    flushMe();
    flushRoster();

    const req = httpMock.expectOne((r) => r.url === '/api/appointments');
    req.flush([]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.calendar-empty')).not.toBeNull();
  });

  it('does not show the empty-state message when appointments exist', () => {
    fixture.detectChanges();
    flushConnections();
    flushMe();
    flushRoster();

    const req = httpMock.expectOne((r) => r.url === '/api/appointments');
    req.flush([
      {
        id: '1',
        title: 'Standup',
        startUtc: new Date().toISOString(),
        endUtc: new Date().toISOString(),
        status: 'Unterbrechbar',
      },
    ]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.calendar-empty')).toBeNull();
  });

  it('shows the connect-prompt instead of the plain empty message when there is no calendar connection (AC 12)', () => {
    fixture.detectChanges();
    flushConnections([
      { provider: 'Google', connected: false, lastSuccessfulSyncAt: null, hasError: false, errorCode: null },
      { provider: 'Outlook', connected: false, lastSuccessfulSyncAt: null, hasError: false, errorCode: null },
    ]);
    flushMe();
    flushRoster();

    const req = httpMock.expectOne((r) => r.url === '/api/appointments');
    req.flush([]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.calendar-empty a')).not.toBeNull();
  });

  it('shows the plain empty message (not the connect-prompt) once a calendar is connected but not yet synced (AC 12)', () => {
    fixture.detectChanges();
    flushConnections([{ provider: 'Google', connected: true, lastSuccessfulSyncAt: null, hasError: false, errorCode: null }]);
    flushMe();
    flushRoster();

    const req = httpMock.expectOne((r) => r.url === '/api/appointments');
    req.flush([]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.calendar-empty a')).toBeNull();
    expect(compiled.querySelector('.calendar-empty')).not.toBeNull();
  });

  it('shows the Admin sync-overview nav link for an Admin (Story 2.3)', () => {
    fixture.detectChanges();
    flushConnections();
    flushMe('Admin');
    flushRoster();
    httpMock.expectOne((r) => r.url === '/api/appointments').flush([]);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('a[routerLink="/admin/sync-overview"]')).not.toBeNull();
  });

  it('hides the Admin sync-overview nav link for a Member (Story 2.3, AC 2)', () => {
    fixture.detectChanges();
    flushConnections();
    flushMe('Member');
    flushRoster();
    httpMock.expectOne((r) => r.url === '/api/appointments').flush([]);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('a[routerLink="/admin/sync-overview"]')).toBeNull();
  });

  it('defaults to the Week view (AC 1)', () => {
    fixture.detectChanges();
    flushConnections();
    flushMe();
    flushRoster();
    httpMock.expectOne((r) => r.url === '/api/appointments').flush([]);
    fixture.detectChanges();

    expect(fixture.componentInstance.viewType()).toBe('week');
  });

  it('fetches the full month grid, including adjacent-month overflow days at the boundary', () => {
    // Jul 30 2026 (Thursday) sits in a week that spills into August — the fetched range must cover
    // that overflow day too, or Week/Month view boundary days silently show as empty.
    const focusDate = new Date(2026, 6, 30);
    fixture.componentInstance.focusDate.set(focusDate);
    fixture.detectChanges();
    flushConnections();
    flushMe();
    flushRoster();

    const gridDays = getMonthGridDays(focusDate);
    const expectedFrom = gridDays[0];
    const expectedTo = addDays(gridDays[gridDays.length - 1], 1);

    const req = httpMock.expectOne((r) => r.url === '/api/appointments');
    expect(req.request.params.get('from')).toBe(expectedFrom.toISOString());
    expect(req.request.params.get('to')).toBe(expectedTo.toISOString());
    req.flush([]);
  });

  it('switching views does not trigger a new HTTP request (AC 2)', () => {
    fixture.detectChanges();
    flushConnections();
    flushMe();
    flushRoster();
    httpMock.expectOne((r) => r.url === '/api/appointments').flush([]);
    fixture.detectChanges();

    fixture.componentInstance.setViewType('month');
    fixture.detectChanges();

    httpMock.expectNone((r) => r.url === '/api/appointments');
    expect(fixture.componentInstance.viewType()).toBe('month');
  });

  it('"+ Neuer Termin" opens the create form blank (AC 2)', () => {
    fixture.detectChanges();
    flushConnections();
    flushMe();
    flushRoster();
    httpMock.expectOne((r) => r.url === '/api/appointments').flush([]);
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.calendar-toolbar__create') as HTMLElement).click();
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url === '/api/persons').flush([]);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.appointment-create__panel')).not.toBeNull();
    expect(fixture.componentInstance.createFormPrefillStart()).toBeNull();
  });

  it('appends a newly created appointment locally without refetching the whole month (AC 3)', () => {
    fixture.detectChanges();
    flushConnections();
    flushMe();
    flushRoster();
    httpMock.expectOne((r) => r.url === '/api/appointments').flush([]);
    fixture.detectChanges();

    fixture.componentInstance.onAppointmentSaved({
      id: 'new-1',
      title: 'Kurzabstimmung',
      startUtc: new Date().toISOString(),
      endUtc: new Date().toISOString(),
      status: 'Unterbrechbar',
    });
    fixture.detectChanges();

    expect(fixture.componentInstance.appointments().map((a) => a.id)).toEqual(['new-1']);
    httpMock.expectNone((r) => r.url === '/api/appointments' && r.method === 'GET');
  });

  it('clicking a rendered appointment opens the detail popover with the right appointmentId (Story 1.4)', () => {
    fixture.detectChanges();
    flushConnections();
    flushMe();
    flushRoster();
    httpMock.expectOne((r) => r.url === '/api/appointments').flush([
      {
        id: 'appt-1',
        title: 'Standup',
        startUtc: new Date().toISOString(),
        endUtc: new Date().toISOString(),
        status: 'Unterbrechbar',
      },
    ]);
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.calendar-column__appointment') as HTMLElement).click();
    fixture.detectChanges();

    expect(fixture.componentInstance.detailAppointmentId()).toBe('appt-1');
    httpMock.expectOne('/api/appointments/appt-1').flush({
      id: 'appt-1',
      isFullDetail: true,
      title: 'Standup',
      startUtc: new Date().toISOString(),
      endUtc: new Date().toISOString(),
      status: 'Unterbrechbar',
      attendees: [],
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.appointment-detail__panel')).not.toBeNull();
  });

  it("the detail popover's closed output closes it", () => {
    fixture.detectChanges();
    flushConnections();
    flushMe();
    flushRoster();
    httpMock.expectOne((r) => r.url === '/api/appointments').flush([]);
    fixture.detectChanges();

    fixture.componentInstance.openDetail('appt-1');
    fixture.detectChanges();
    httpMock.expectOne('/api/appointments/appt-1').flush({
      id: 'appt-1',
      isFullDetail: true,
      title: 'Standup',
      startUtc: new Date().toISOString(),
      endUtc: new Date().toISOString(),
      status: 'Unterbrechbar',
      attendees: [],
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.appointment-detail__panel')).not.toBeNull();

    fixture.componentInstance.onDetailClosed();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.appointment-detail__panel')).toBeNull();
  });

  describe('multi-person view (Story 3.1)', () => {
    function colleagueColumns(): ReturnType<typeof fixture.debugElement.queryAll> {
      return fixture.debugElement
        .queryAll(By.directive(CalendarColumn))
        .filter((el) => el.componentInstance.mode() === 'colleague');
    }

    it('shows the person-selector empty hint when no colleague is selected (AC 8)', () => {
      fixture.detectChanges();
      flushConnections();
      flushMe();
      flushRoster();
      httpMock.expectOne((r) => r.url === '/api/appointments').flush([]);
      fixture.detectChanges();

      expect(fixture.nativeElement.textContent).toContain('Wähle Teammitglieder');
      expect(colleagueColumns().length).toBe(0);
    });

    it('selecting a colleague fetches and renders their colleague-mode columns (AC 2, 3, 7)', () => {
      fixture.detectChanges();
      flushConnections();
      flushMe();
      flushRoster([{ id: 'p1', email: 'colleague@example.com' }]);
      httpMock.expectOne((r) => r.url === '/api/appointments').flush([]);
      fixture.detectChanges();

      fixture.componentInstance.onSelectionChanged(['p1']);
      fixture.detectChanges();

      httpMock.expectOne((r) => r.url === '/api/appointments/colleagues').flush({ p1: [] });
      fixture.detectChanges();

      // Week view: one colleague column per visible weekday.
      expect(colleagueColumns().length).toBe(7);
    });

    it('preserves the selection across Week/Day view switches without a duplicate colleague fetch (AC 9)', () => {
      fixture.detectChanges();
      flushConnections();
      flushMe();
      flushRoster([{ id: 'p1', email: 'colleague@example.com' }]);
      httpMock.expectOne((r) => r.url === '/api/appointments').flush([]);
      fixture.detectChanges();

      fixture.componentInstance.onSelectionChanged(['p1']);
      fixture.detectChanges();
      httpMock.expectOne((r) => r.url === '/api/appointments/colleagues').flush({ p1: [] });
      fixture.detectChanges();

      fixture.componentInstance.setViewType('day');
      fixture.detectChanges();

      expect(fixture.componentInstance.selectedPersonIds()).toEqual(['p1']);
      httpMock.expectNone((r) => r.url === '/api/appointments/colleagues');
      expect(colleagueColumns().length).toBe(1);
    });

    it('clicking a colleague appointment block opens the same status-only-aware detail popover (AC 4, 5)', () => {
      fixture.detectChanges();
      flushConnections();
      flushMe();
      flushRoster([{ id: 'p1', email: 'colleague@example.com' }]);
      httpMock.expectOne((r) => r.url === '/api/appointments').flush([]);
      fixture.detectChanges();

      fixture.componentInstance.onSelectionChanged(['p1']);
      fixture.detectChanges();
      httpMock.expectOne((r) => r.url === '/api/appointments/colleagues').flush({
        p1: [
          {
            id: 'colleague-appt-1',
            startUtc: new Date().toISOString(),
            endUtc: new Date().toISOString(),
            status: 'BitteNichtStoeren',
          },
        ],
      });
      fixture.detectChanges();

      const colleagueBlock = colleagueColumns()
        .map((el) => el.nativeElement.querySelector('.calendar-column__appointment') as HTMLElement | null)
        .find((el) => el !== null)!;
      colleagueBlock.click();
      fixture.detectChanges();

      expect(fixture.componentInstance.detailAppointmentId()).toBe('colleague-appt-1');
      httpMock.expectOne('/api/appointments/colleague-appt-1').flush({
        id: 'colleague-appt-1',
        isFullDetail: false,
        title: null,
        startUtc: null,
        endUtc: null,
        status: 'BitteNichtStoeren',
        attendees: null,
      });
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.appointment-detail__panel')).not.toBeNull();
      expect(fixture.nativeElement.textContent).toContain('Details sind privat');
    });

    it('also shows the person-selector in Month view (Story 3.2)', () => {
      fixture.detectChanges();
      flushConnections();
      flushMe();
      flushRoster();
      httpMock.expectOne((r) => r.url === '/api/appointments').flush([]);
      fixture.detectChanges();

      fixture.componentInstance.setViewType('month');
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('app-person-selector')).not.toBeNull();
    });
  });
});
