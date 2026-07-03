import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Appointment } from '../appointment.model';
import { MonthView } from './month-view';

describe('MonthView', () => {
  let fixture: ComponentFixture<MonthView>;
  const focusDate = new Date(2026, 6, 6);

  const appointment: Appointment = {
    id: 'appt-1',
    title: 'Standup',
    startUtc: new Date(2026, 6, 6, 9, 0).toISOString(),
    endUtc: new Date(2026, 6, 6, 9, 30).toISOString(),
    status: 'Unterbrechbar',
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MonthView],
    }).compileComponents();

    fixture = TestBed.createComponent(MonthView);
    fixture.componentRef.setInput('focusDate', focusDate);
    fixture.componentRef.setInput('appointments', [appointment]);
    fixture.detectChanges();
  });

  it('emits appointmentActivated with the id when a rendered title is clicked', () => {
    let emitted: string | undefined;
    fixture.componentInstance.appointmentActivated.subscribe((id) => (emitted = id));

    const title = fixture.nativeElement.querySelector('.month-view__title') as HTMLElement;
    title.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(emitted).toBe('appt-1');
  });

  it('emits appointmentActivated on Enter/Space for a focused title', () => {
    let emitCount = 0;
    fixture.componentInstance.appointmentActivated.subscribe(() => emitCount++);

    const title = fixture.nativeElement.querySelector('.month-view__title') as HTMLElement;
    title.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
    title.dispatchEvent(new KeyboardEvent('keydown', { key: ' ', bubbles: true }));

    expect(emitCount).toBe(2);
  });

  it('the "+N more" overflow indicator has no click handler', () => {
    const manyAppointments: Appointment[] = Array.from({ length: 4 }, (_, i) => ({
      id: `appt-${i}`,
      title: `Termin ${i}`,
      startUtc: new Date(2026, 6, 6, 9 + i, 0).toISOString(),
      endUtc: new Date(2026, 6, 6, 9 + i, 30).toISOString(),
      status: 'Unterbrechbar',
    }));
    fixture.componentRef.setInput('appointments', manyAppointments);
    fixture.detectChanges();

    let emitCount = 0;
    fixture.componentInstance.appointmentActivated.subscribe(() => emitCount++);

    const more = fixture.nativeElement.querySelector('.month-view__more') as HTMLElement;
    expect(more).not.toBeNull();
    more.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(emitCount).toBe(0);
  });
});
