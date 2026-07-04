import { Component, computed, input, output, signal } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { Activatable } from '../../../../shared/activatable/activatable';
import { FocusTrap } from '../../../../shared/focus-trap/focus-trap';
import { Appointment, ColleagueAppointmentSlot } from '../appointment.model';
import { PersonSummary } from '../calendar.service';
import { buildColleagueDayIndex, ColleagueDayStatus, computeColleagueDayStatusesFromIndex } from '../colleague-day-status';
import { StatusBadge } from '../status-badge/status-badge';
import { dateKey, getMonthGridDays, groupByDay, isSameDay } from '../date-utils';

interface MonthDayCell {
  date: Date;
  dayNumber: number;
  isCurrentMonth: boolean;
  appointments: Appointment[];
  hasDndAggregate: boolean;
  dndCount: number;
}

const MAX_TITLES_PER_CELL = 2;

@Component({
  selector: 'app-month-view',
  standalone: true,
  imports: [Activatable, FocusTrap, StatusBadge, TranslocoPipe],
  templateUrl: './month-view.html',
  styleUrl: './month-view.css',
})
export class MonthView {
  readonly focusDate = input.required<Date>();
  readonly appointments = input<Appointment[]>([]);
  readonly roster = input<PersonSummary[]>([]);
  readonly selectedPersonIds = input<string[]>([]);
  readonly colleagueAppointments = input<Record<string, ColleagueAppointmentSlot[]>>({});
  readonly appointmentActivated = output<string>();

  readonly maxTitlesPerCell = MAX_TITLES_PER_CELL;

  readonly openPopoverDate = signal<Date | null>(null);

  // Built once per `colleagueAppointments()` change (Angular memoizes `computed()` by its signal
  // dependencies) — `cells()` below queries this per visible day instead of re-grouping each
  // colleague's full appointment array from scratch ~40 times per recompute (code review finding).
  private readonly colleagueDayIndex = computed(() => buildColleagueDayIndex(this.colleagueAppointments()));

  // Grouped by the appointment's start day only — a start-spanning-past-midnight appointment shows
  // on the day it starts. Multi-day/all-day spanning is out of this story's scope (no AC covers it).
  readonly cells = computed<MonthDayCell[]>(() => {
    const focus = this.focusDate();
    const days = getMonthGridDays(focus);

    const byDay = groupByDay(this.appointments());
    const personIds = this.selectedPersonIds();
    const roster = this.roster();
    const index = this.colleagueDayIndex();

    return days.map((date) => {
      const dndCount = computeColleagueDayStatusesFromIndex(personIds, roster, index, date).filter(
        (entry) => entry.status === 'BitteNichtStoeren'
      ).length;

      return {
        date,
        dayNumber: date.getDate(),
        isCurrentMonth: date.getMonth() === focus.getMonth(),
        appointments: byDay.get(dateKey(date)) ?? [],
        hasDndAggregate: dndCount > 0,
        dndCount,
      };
    });
  });

  readonly popoverStatuses = computed<ColleagueDayStatus[]>(() => {
    const date = this.openPopoverDate();
    if (!date) {
      return [];
    }
    return computeColleagueDayStatusesFromIndex(this.selectedPersonIds(), this.roster(), this.colleagueDayIndex(), date);
  });

  activateAppointment(appointmentId: string): void {
    this.appointmentActivated.emit(appointmentId);
  }

  isPopoverCell(date: Date): boolean {
    const open = this.openPopoverDate();
    return !!open && isSameDay(open, date);
  }

  openPopover(date: Date): void {
    this.openPopoverDate.set(date);
  }

  closePopover(): void {
    this.openPopoverDate.set(null);
  }
}
