import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { getTranslocoTestingModule } from '../../testing/transloco-testing';
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

  it('shows the empty-state message when there are no appointments (AC 3)', () => {
    fixture.detectChanges();

    const req = httpMock.expectOne((r) => r.url === '/api/appointments');
    req.flush([]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.calendar-empty')).not.toBeNull();
  });

  it('does not show the empty-state message when appointments exist', () => {
    fixture.detectChanges();

    const req = httpMock.expectOne((r) => r.url === '/api/appointments');
    req.flush([
      { id: '1', title: 'Standup', startUtc: new Date().toISOString(), endUtc: new Date().toISOString() },
    ]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.calendar-empty')).toBeNull();
  });

  it('defaults to the Week view (AC 1)', () => {
    fixture.detectChanges();
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
    httpMock.expectOne((r) => r.url === '/api/appointments').flush([]);
    fixture.detectChanges();

    fixture.componentInstance.setViewType('month');
    fixture.detectChanges();

    httpMock.expectNone((r) => r.url === '/api/appointments');
    expect(fixture.componentInstance.viewType()).toBe('month');
  });
});
