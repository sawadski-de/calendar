import { Component, computed, input, output } from '@angular/core';
import { Appointment } from '../appointment.model';
import { dateKey, getMonthGridDays, groupByDay } from '../date-utils';

interface MonthDayCell {
  date: Date;
  dayNumber: number;
  isCurrentMonth: boolean;
  appointments: Appointment[];
}

const MAX_TITLES_PER_CELL = 2;

@Component({
  selector: 'app-month-view',
  standalone: true,
  templateUrl: './month-view.html',
  styleUrl: './month-view.css',
})
export class MonthView {
  readonly focusDate = input.required<Date>();
  readonly appointments = input<Appointment[]>([]);
  readonly appointmentActivated = output<string>();

  readonly maxTitlesPerCell = MAX_TITLES_PER_CELL;

  // Grouped by the appointment's start day only — a start-spanning-past-midnight appointment shows
  // on the day it starts. Multi-day/all-day spanning is out of this story's scope (no AC covers it).
  readonly cells = computed<MonthDayCell[]>(() => {
    const focus = this.focusDate();
    const days = getMonthGridDays(focus);

    const byDay = groupByDay(this.appointments());

    return days.map((date) => ({
      date,
      dayNumber: date.getDate(),
      isCurrentMonth: date.getMonth() === focus.getMonth(),
      appointments: byDay.get(dateKey(date)) ?? [],
    }));
  });

  activateAppointment(appointmentId: string): void {
    this.appointmentActivated.emit(appointmentId);
  }
}
