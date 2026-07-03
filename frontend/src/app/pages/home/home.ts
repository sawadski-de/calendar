import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthService } from '../../core/auth/auth.service';
import { LanguageSwitcher } from '../../shared/language-switcher/language-switcher';
import { CalendarViewType, ViewSwitcher } from '../../shared/view-switcher/view-switcher';
import { AppointmentCreate } from './calendar/appointment-create/appointment-create';
import { AppointmentDetailPopover } from './calendar/appointment-detail/appointment-detail';
import { Appointment } from './calendar/appointment.model';
import { CalendarColumn } from './calendar/calendar-column/calendar-column';
import { CalendarService } from './calendar/calendar.service';
import { addDays, addMonths, dateKey, getMonthGridDays, getWeekDays, groupByDay, startOfDay } from './calendar/date-utils';
import { MonthView } from './calendar/month-view/month-view';

const HOUR_LABELS = Array.from({ length: 24 }, (_, hour) => hour);

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [TranslocoPipe, LanguageSwitcher, ViewSwitcher, CalendarColumn, MonthView, AppointmentCreate, AppointmentDetailPopover],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly calendarService = inject(CalendarService);

  readonly viewType = signal<CalendarViewType>('week');
  readonly focusDate = signal(new Date());
  readonly appointments = signal<Appointment[]>([]);
  readonly loading = signal(false);

  readonly hourLabels = HOUR_LABELS;

  readonly weekDays = computed(() => getWeekDays(this.focusDate()));
  readonly dayDate = computed(() => startOfDay(this.focusDate()));
  readonly isEmpty = computed(() => !this.loading() && this.appointments().length === 0);

  private readonly appointmentsByDay = computed(() => groupByDay(this.appointments()));

  readonly createFormOpen = signal(false);
  readonly createFormPrefillStart = signal<Date | null>(null);

  readonly detailOpen = signal(false);
  readonly detailAppointmentId = signal<string | null>(null);

  private loadedMonthKey: string | null = null;

  ngOnInit(): void {
    this.loadAppointmentsForFocusMonth();
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => void this.router.navigateByUrl('/login'),
      error: () => void this.router.navigateByUrl('/login'),
    });
  }

  setViewType(viewType: CalendarViewType): void {
    // Pure client-side state change — the whole focus month is already loaded (see
    // loadAppointmentsForFocusMonth), so switching the view type never triggers a new request (AC 2).
    this.viewType.set(viewType);
  }

  navigatePrevious(): void {
    this.moveFocusDate(-1);
  }

  navigateNext(): void {
    this.moveFocusDate(1);
  }

  navigateToday(): void {
    this.focusDate.set(new Date());
    this.loadAppointmentsForFocusMonth();
  }

  appointmentsFor(date: Date): Appointment[] {
    return this.appointmentsByDay().get(dateKey(date)) ?? [];
  }

  openCreateBlank(): void {
    this.createFormPrefillStart.set(null);
    this.createFormOpen.set(true);
  }

  openCreateFromSlot(start: Date): void {
    this.createFormPrefillStart.set(start);
    this.createFormOpen.set(true);
  }

  onAppointmentSaved(appointment: Appointment): void {
    // Insert locally rather than refetching the whole month — the appointment must appear
    // immediately (AC 3) without an extra round-trip.
    this.appointments.update((list) => [...list, appointment]);
    this.createFormOpen.set(false);
  }

  onAppointmentCreateCancelled(): void {
    this.createFormOpen.set(false);
  }

  openDetail(appointmentId: string): void {
    this.detailAppointmentId.set(appointmentId);
    this.detailOpen.set(true);
  }

  onDetailClosed(): void {
    this.detailOpen.set(false);
  }

  private moveFocusDate(direction: 1 | -1): void {
    const current = this.focusDate();
    const next =
      this.viewType() === 'month'
        ? addMonths(current, direction)
        : this.viewType() === 'week'
          ? addDays(current, 7 * direction)
          : addDays(current, direction);

    this.focusDate.set(next);
    this.loadAppointmentsForFocusMonth();
  }

  /** Always loads the full Mon–Sun grid covering focusDate's month, including the leading/trailing
   *  days of adjacent months that Week/Month views render at the grid's edges — so view-type
   *  switching never needs to refetch (AC 2). Only re-fetches when navigation actually moves the
   *  focus date into a different month. */
  private loadAppointmentsForFocusMonth(): void {
    const focus = this.focusDate();
    const monthKey = `${focus.getFullYear()}-${focus.getMonth()}`;
    if (monthKey === this.loadedMonthKey) {
      return;
    }

    const gridDays = getMonthGridDays(focus);
    const from = gridDays[0];
    const to = addDays(gridDays[gridDays.length - 1], 1);

    this.loading.set(true);
    this.calendarService.getAppointments(from, to).subscribe({
      next: (appointments) => {
        this.appointments.set(appointments);
        this.loadedMonthKey = monthKey;
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
