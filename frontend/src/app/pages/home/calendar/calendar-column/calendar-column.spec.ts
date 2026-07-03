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

  describe('appointment activation (Story 1.4)', () => {
    const appointment: Appointment = {
      id: 'appt-1',
      title: 'Standup',
      startUtc: new Date(2026, 6, 6, 9, 0).toISOString(),
      endUtc: new Date(2026, 6, 6, 9, 30).toISOString(),
      status: 'Unterbrechbar',
    };

    function appointmentBlock(): HTMLElement {
      return fixture.nativeElement.querySelector('.calendar-column__appointment');
    }

    it('emits appointmentActivated with the appointment id on click', () => {
      fixture.componentRef.setInput('appointments', [appointment]);
      fixture.detectChanges();

      let emitted: string | undefined;
      fixture.componentInstance.appointmentActivated.subscribe((id) => (emitted = id));

      appointmentBlock().dispatchEvent(new MouseEvent('click', { bubbles: true }));

      expect(emitted).toBe('appt-1');
    });

    it('emits appointmentActivated on Enter/Space for a focused appointment block', () => {
      fixture.componentRef.setInput('appointments', [appointment]);
      fixture.detectChanges();

      let emitCount = 0;
      fixture.componentInstance.appointmentActivated.subscribe(() => emitCount++);

      const block = appointmentBlock();
      block.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
      block.dispatchEvent(new KeyboardEvent('keydown', { key: ' ', bubbles: true }));

      expect(emitCount).toBe(2);
    });

    it('clicking an appointment does not also emit slotActivated', () => {
      fixture.componentRef.setInput('appointments', [appointment]);
      fixture.detectChanges();

      let slotEmitCount = 0;
      let appointmentEmitCount = 0;
      fixture.componentInstance.slotActivated.subscribe(() => slotEmitCount++);
      fixture.componentInstance.appointmentActivated.subscribe(() => appointmentEmitCount++);

      appointmentBlock().dispatchEvent(new MouseEvent('click', { bubbles: true }));

      expect(appointmentEmitCount).toBe(1);
      expect(slotEmitCount).toBe(0);
    });

    it('double-clicking an empty slot does not also emit appointmentActivated', () => {
      fixture.componentRef.setInput('appointments', [appointment]);
      fixture.detectChanges();

      let slotEmitCount = 0;
      let appointmentEmitCount = 0;
      fixture.componentInstance.slotActivated.subscribe(() => slotEmitCount++);
      fixture.componentInstance.appointmentActivated.subscribe(() => appointmentEmitCount++);

      slotAt(14).dispatchEvent(new MouseEvent('dblclick', { bubbles: true }));

      expect(slotEmitCount).toBe(1);
      expect(appointmentEmitCount).toBe(0);
    });
  });
});
