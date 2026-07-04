import { ComponentFixture, TestBed } from '@angular/core/testing';
import { getTranslocoTestingModule } from '../../../../testing/transloco-testing';
import { Appointment, ColleagueAppointmentSlot } from '../appointment.model';
import { PersonSummary } from '../calendar.service';
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
      imports: [MonthView, getTranslocoTestingModule()],
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

  describe('colleague aggregate marker + popover (Story 3.2)', () => {
    const roster: PersonSummary[] = [
      { id: 'p1', email: 'bjoern@example.com' },
      { id: 'p2', email: 'katharina@example.com' },
    ];

    function setColleagues(colleagueAppointments: Record<string, ColleagueAppointmentSlot[]>): void {
      fixture.componentRef.setInput('roster', roster);
      fixture.componentRef.setInput('selectedPersonIds', ['p1', 'p2']);
      fixture.componentRef.setInput('colleagueAppointments', colleagueAppointments);
      fixture.detectChanges();
    }

    function markerOn(day: number): HTMLElement | null {
      const cells = Array.from(fixture.nativeElement.querySelectorAll('.month-view__cell')) as HTMLElement[];
      const cell = cells.find((c) => c.querySelector('.month-view__day-number')?.textContent?.trim() === String(day));
      return cell?.querySelector('.month-view__aggregate-marker') ?? null;
    }

    it('shows no marker when no selected colleague is BitteNichtStoeren that day (AC 2)', () => {
      setColleagues({
        p1: [{ id: 'a', startUtc: new Date(2026, 6, 6, 9, 0).toISOString(), endUtc: new Date(2026, 6, 6, 10, 0).toISOString(), status: 'Unterbrechbar' }],
      });

      expect(markerOn(6)).toBeNull();
    });

    it('shows a marker when at least one selected colleague is BitteNichtStoeren that day (AC 1)', () => {
      setColleagues({
        p1: [{ id: 'a', startUtc: new Date(2026, 6, 6, 9, 0).toISOString(), endUtc: new Date(2026, 6, 6, 10, 0).toISOString(), status: 'BitteNichtStoeren' }],
      });

      expect(markerOn(6)).not.toBeNull();
    });

    it('activating the marker opens a popover listing every selected colleague (AC 3, 4)', () => {
      setColleagues({
        p1: [{ id: 'a', startUtc: new Date(2026, 6, 6, 9, 0).toISOString(), endUtc: new Date(2026, 6, 6, 10, 0).toISOString(), status: 'BitteNichtStoeren' }],
      });

      markerOn(6)!.dispatchEvent(new MouseEvent('click', { bubbles: true }));
      fixture.detectChanges();

      const popover = fixture.nativeElement.querySelector('.month-view__popover') as HTMLElement;
      expect(popover).not.toBeNull();
      expect(popover.textContent).toContain('bjoern@example.com');
      // p2 has zero appointments that day but is still listed (defaults to Unterbrechbar, AC 3).
      expect(popover.textContent).toContain('katharina@example.com');
    });

    it('Enter/Space on the marker also opens the popover (keyboard equivalence, AC 4)', () => {
      setColleagues({
        p1: [{ id: 'a', startUtc: new Date(2026, 6, 6, 9, 0).toISOString(), endUtc: new Date(2026, 6, 6, 10, 0).toISOString(), status: 'BitteNichtStoeren' }],
      });

      markerOn(6)!.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.month-view__popover')).not.toBeNull();
    });

    it('Esc closes the popover', () => {
      setColleagues({
        p1: [{ id: 'a', startUtc: new Date(2026, 6, 6, 9, 0).toISOString(), endUtc: new Date(2026, 6, 6, 10, 0).toISOString(), status: 'BitteNichtStoeren' }],
      });
      markerOn(6)!.dispatchEvent(new MouseEvent('click', { bubbles: true }));
      fixture.detectChanges();

      fixture.nativeElement
        .querySelector('.month-view__popover')
        .dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.month-view__popover')).toBeNull();
    });

    it('clicking the backdrop closes the popover (AC 5)', () => {
      setColleagues({
        p1: [{ id: 'a', startUtc: new Date(2026, 6, 6, 9, 0).toISOString(), endUtc: new Date(2026, 6, 6, 10, 0).toISOString(), status: 'BitteNichtStoeren' }],
      });
      markerOn(6)!.dispatchEvent(new MouseEvent('click', { bubbles: true }));
      fixture.detectChanges();

      (fixture.nativeElement.querySelector('.month-view__popover-backdrop') as HTMLElement).click();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.month-view__popover')).toBeNull();
    });

    it('closing the popover returns focus to the triggering marker (AC 5)', () => {
      setColleagues({
        p1: [{ id: 'a', startUtc: new Date(2026, 6, 6, 9, 0).toISOString(), endUtc: new Date(2026, 6, 6, 10, 0).toISOString(), status: 'BitteNichtStoeren' }],
      });
      document.body.appendChild(fixture.nativeElement);
      const marker = markerOn(6)!;
      // Mirrors focus-trap.spec.ts's own pattern: a real click also focuses the element in a browser,
      // but jsdom's synthetic click doesn't — focus it explicitly so FocusTrap captures the right
      // "previously focused" element to restore on close.
      marker.focus();
      marker.dispatchEvent(new MouseEvent('click', { bubbles: true }));
      fixture.detectChanges();

      fixture.componentInstance.closePopover();
      fixture.detectChanges();

      expect(document.activeElement).toBe(marker);
    });
  });
});
