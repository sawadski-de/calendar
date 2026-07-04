import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { getTranslocoTestingModule } from '../../../../testing/transloco-testing';
import { AppointmentDetail } from '../calendar.service';
import { AppointmentDetailPopover } from './appointment-detail';

describe('AppointmentDetailPopover', () => {
  let fixture: ComponentFixture<AppointmentDetailPopover>;
  let httpMock: HttpTestingController;

  const detail: AppointmentDetail = {
    id: 'a1',
    isFullDetail: true,
    title: 'Kurzabstimmung',
    startUtc: new Date(2026, 6, 2, 9, 0).toISOString(),
    endUtc: new Date(2026, 6, 2, 9, 30).toISOString(),
    status: 'Unterbrechbar',
    attendees: [{ personId: 'p1', email: 'jonas@example.com' }],
  };

  const statusOnlyDetail: AppointmentDetail = {
    id: 'a1',
    isFullDetail: false,
    title: null,
    startUtc: null,
    endUtc: null,
    status: 'BitteNichtStoeren',
    attendees: null,
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppointmentDetailPopover, getTranslocoTestingModule()],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(AppointmentDetailPopover);
    httpMock = TestBed.inject(HttpTestingController);
    fixture.componentRef.setInput('appointmentId', 'a1');
  });

  afterEach(() => httpMock.verify());

  it('renders title, attendee email, and status badge from the fetched detail', () => {
    fixture.detectChanges();
    httpMock.expectOne('/api/appointments/a1').flush(detail);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Kurzabstimmung');
    expect(el.textContent).toContain('jonas@example.com');
    expect(el.querySelector('app-status-badge')).not.toBeNull();
  });

  it('renders no location text anywhere', () => {
    fixture.detectChanges();
    httpMock.expectOne('/api/appointments/a1').flush(detail);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent?.toLowerCase()).not.toContain('ort');
    expect(el.textContent?.toLowerCase()).not.toContain('location');
  });

  it('emits closed on Esc with no further HTTP calls', () => {
    fixture.detectChanges();
    httpMock.expectOne('/api/appointments/a1').flush(detail);
    fixture.detectChanges();

    let closedCount = 0;
    fixture.componentInstance.closed.subscribe(() => closedCount++);

    fixture.nativeElement
      .querySelector('.appointment-detail__panel')
      .dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));

    expect(closedCount).toBe(1);
    httpMock.expectNone(() => true);
  });

  it('emits closed on a backdrop click', () => {
    fixture.detectChanges();
    httpMock.expectOne('/api/appointments/a1').flush(detail);
    fixture.detectChanges();

    let closedCount = 0;
    fixture.componentInstance.closed.subscribe(() => closedCount++);

    (fixture.nativeElement.querySelector('.appointment-detail__backdrop') as HTMLElement).click();

    expect(closedCount).toBe(1);
  });

  it('shows an error message and a close button instead of a blank overlay on a 404 (Story 1.4 code review)', () => {
    fixture.detectChanges();
    httpMock.expectOne('/api/appointments/a1').flush(
      { code: 'appointment-not-found' },
      { status: 404, statusText: 'Not Found' }
    );
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.appointment-detail__close')).not.toBeNull();
    expect(el.textContent?.trim().length).toBeGreaterThan(0);
  });

  it('closes via the close button after a load error', () => {
    fixture.detectChanges();
    httpMock.expectOne('/api/appointments/a1').flush(
      { code: 'appointment-not-found' },
      { status: 404, statusText: 'Not Found' }
    );
    fixture.detectChanges();

    let closedCount = 0;
    fixture.componentInstance.closed.subscribe(() => closedCount++);
    (fixture.nativeElement.querySelector('.appointment-detail__close') as HTMLElement).click();

    expect(closedCount).toBe(1);
  });

  it('does not emit closed for a click inside the panel', () => {
    fixture.detectChanges();
    httpMock.expectOne('/api/appointments/a1').flush(detail);
    fixture.detectChanges();

    let closedCount = 0;
    fixture.componentInstance.closed.subscribe(() => closedCount++);

    (fixture.nativeElement.querySelector('.appointment-detail__panel') as HTMLElement).click();

    expect(closedCount).toBe(0);
  });

  it('renders only the status badge and a privacy note for a status-only (colleague) response (Story 3.1)', () => {
    fixture.detectChanges();
    httpMock.expectOne('/api/appointments/a1').flush(statusOnlyDetail);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('app-status-badge')).not.toBeNull();
    expect(el.textContent).toContain('Details sind privat');
    expect(el.textContent).not.toContain('Kurzabstimmung');
    expect(el.textContent).not.toContain('jonas@example.com');
  });
});
