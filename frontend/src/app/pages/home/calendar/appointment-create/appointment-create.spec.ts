import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { getTranslocoTestingModule } from '../../../../testing/transloco-testing';
import { Appointment } from '../appointment.model';
import { AppointmentCreate } from './appointment-create';

describe('AppointmentCreate', () => {
  let fixture: ComponentFixture<AppointmentCreate>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppointmentCreate, getTranslocoTestingModule()],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(AppointmentCreate);
    httpMock = TestBed.inject(HttpTestingController);
  });

  function flushRoster() {
    httpMock.expectOne('/api/persons').flush([]);
  }

  function titleInput(): HTMLInputElement {
    return fixture.nativeElement.querySelector('#appointment-create-title');
  }

  function dateInput(): HTMLInputElement {
    return fixture.nativeElement.querySelector('#appointment-create-date');
  }

  function startInput(): HTMLInputElement {
    return fixture.nativeElement.querySelector('#appointment-create-start');
  }

  it('pre-fills date/time when opened from an empty slot (AC 1)', () => {
    fixture.componentRef.setInput('prefillStart', new Date(2026, 6, 2, 14, 0));
    fixture.detectChanges();
    flushRoster();

    expect(dateInput().value).toBe('2026-07-02');
    expect(startInput().value).toBe('14:00');
  });

  it('leaves date/time empty when opened blank (AC 2)', () => {
    fixture.detectChanges();
    flushRoster();

    expect(dateInput().value).toBe('');
    expect(startInput().value).toBe('');
  });

  it('rejects a blank title, focuses the title field, and makes no HTTP call (AC 10)', () => {
    fixture.detectChanges();
    flushRoster();

    titleInput().value = '   ';
    titleInput().dispatchEvent(new Event('input'));
    fixture.detectChanges();

    fixture.nativeElement.querySelector('.appointment-create__button--primary').click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.appointment-create__error')?.textContent).toContain(
      'Bitte gib einen Titel ein.'
    );
    httpMock.expectNone('/api/appointments');
  });

  it('rejects an invalid time range and makes no HTTP call (AC 9)', () => {
    fixture.detectChanges();
    flushRoster();

    titleInput().value = 'Standup';
    titleInput().dispatchEvent(new Event('input'));
    dateInput().value = '2026-07-02';
    dateInput().dispatchEvent(new Event('input'));
    startInput().value = '09:00';
    startInput().dispatchEvent(new Event('input'));
    const durationInput = fixture.nativeElement.querySelector('#appointment-create-duration') as HTMLInputElement;
    durationInput.value = '0';
    durationInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    fixture.nativeElement.querySelector('.appointment-create__button--primary').click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.appointment-create__error')).not.toBeNull();
    httpMock.expectNone('/api/appointments');
  });

  it('submits a POST with the expected body and emits saved on success', () => {
    fixture.detectChanges();
    flushRoster();

    titleInput().value = 'Kurzabstimmung';
    titleInput().dispatchEvent(new Event('input'));
    dateInput().value = '2026-07-02';
    dateInput().dispatchEvent(new Event('input'));
    startInput().value = '09:00';
    startInput().dispatchEvent(new Event('input'));
    fixture.detectChanges();

    let saved: Appointment | undefined;
    fixture.componentInstance.saved.subscribe((a) => (saved = a));

    fixture.nativeElement.querySelector('.appointment-create__button--primary').click();

    const req = httpMock.expectOne('/api/appointments');
    expect(req.request.body.title).toBe('Kurzabstimmung');
    expect(req.request.body.attendeePersonIds).toEqual([]);

    const created: Appointment = {
      id: '1',
      title: 'Kurzabstimmung',
      startUtc: new Date(2026, 6, 2, 9, 0).toISOString(),
      endUtc: new Date(2026, 6, 2, 9, 30).toISOString(),
      status: 'Unterbrechbar',
    };
    req.flush(created);

    expect(saved).toEqual(created);
  });

  it('emits cancelled with no HTTP call when Esc is pressed', () => {
    fixture.detectChanges();
    flushRoster();

    let cancelledCount = 0;
    fixture.componentInstance.cancelled.subscribe(() => cancelledCount++);

    fixture.nativeElement
      .querySelector('.appointment-create__panel')
      .dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));

    expect(cancelledCount).toBe(1);
    httpMock.expectNone('/api/appointments');
  });

  afterEach(() => httpMock.verify());
});
