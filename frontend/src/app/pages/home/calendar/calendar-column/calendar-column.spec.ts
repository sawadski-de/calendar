import { ComponentFixture, TestBed } from '@angular/core/testing';
import { getTranslocoTestingModule } from '../../../../testing/transloco-testing';
import { Appointment } from '../appointment.model';
import { CalendarColumn } from './calendar-column';

describe('CalendarColumn', () => {
  let fixture: ComponentFixture<CalendarColumn>;
  const date = new Date(2026, 6, 6); // Monday, local midnight

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CalendarColumn, getTranslocoTestingModule()],
    }).compileComponents();

    fixture = TestBed.createComponent(CalendarColumn);
    fixture.componentRef.setInput('date', date);
  });

  function slotAt(hour: number): HTMLElement {
    const topPercent = ((hour * 60) / (24 * 60)) * 100;
    const slots = Array.from(fixture.nativeElement.querySelectorAll('.calendar-column__slot')) as HTMLElement[];
    return slots.find((el) => Math.abs(parseFloat(el.style.top) - topPercent) < 0.01)!;
  }

  it('emits slotActivated with the correct start time on double-click of an empty slot', () => {
    fixture.componentRef.setInput('appointments', []);
    fixture.detectChanges();

    let emitted: Date | undefined;
    fixture.componentInstance.slotActivated.subscribe((d) => (emitted = d));

    slotAt(9).dispatchEvent(new MouseEvent('dblclick', { bubbles: true }));

    expect(emitted?.getHours()).toBe(9);
    expect(emitted?.getMinutes()).toBe(0);
  });

  it('emits slotActivated on Enter/Space for a focused empty slot', () => {
    fixture.componentRef.setInput('appointments', []);
    fixture.detectChanges();

    let emitCount = 0;
    fixture.componentInstance.slotActivated.subscribe(() => emitCount++);

    const slot = slotAt(9);
    slot.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
    slot.dispatchEvent(new KeyboardEvent('keydown', { key: ' ', bubbles: true }));

    expect(emitCount).toBe(2);
  });

  it('does not render a slot covered by an existing appointment', () => {
    const appointment: Appointment = {
      id: '1',
      title: 'Standup',
      startUtc: new Date(2026, 6, 6, 9, 0).toISOString(),
      endUtc: new Date(2026, 6, 6, 9, 30).toISOString(),
      status: 'Unterbrechbar',
    };
    fixture.componentRef.setInput('appointments', [appointment]);
    fixture.detectChanges();

    expect(slotAt(9)).toBeUndefined();
    // An adjacent, still-empty slot remains interactive.
    expect(slotAt(10)).not.toBeUndefined();
  });
});
