import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthService } from '../../core/auth/auth.service';
import { LanguageSwitcher } from '../../shared/language-switcher/language-switcher';
import { CalendarViewType, ViewSwitcher } from '../../shared/view-switcher/view-switcher';
import { ConnectionsService } from '../settings/connections/connections.service';
import { AppointmentCreate } from './calendar/appointment-create/appointment-create';
import { AppointmentDetailPopover } from './calendar/appointment-detail/appointment-detail';
import { Appointment, CalendarSlot, ColleagueAppointmentSlot } from './calendar/appointment.model';
import { CalendarColumn } from './calendar/calendar-column/calendar-column';
import { CalendarService, PersonSummary } from './calendar/calendar.service';
import { buildColleagueDayIndex } from './calendar/colleague-day-status';
import { addDays, addMonths, dateKey, getMonthGridDays, getWeekDays, groupByDay, startOfDay } from './calendar/date-utils';
import { MonthView } from './calendar/month-view/month-view';
import { PersonSelector } from './calendar/person-selector/person-selector';

interface ColumnOwner {
  personId: string | null;
  label: string;
}

const HOUR_LABELS = Array.from({ length: 24 }, (_, hour) => hour);

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    TranslocoPipe,
    RouterLink,
    LanguageSwitcher,
    ViewSwitcher,
    CalendarColumn,
    MonthView,
    AppointmentCreate,
    AppointmentDetailPopover,
    PersonSelector,
  ],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly calendarService = inject(CalendarService);
  private readonly connectionsService = inject(ConnectionsService);

  readonly viewType = signal<CalendarViewType>('week');
  readonly focusDate = signal(new Date());
  readonly appointments = signal<Appointment[]>([]);
  readonly loading = signal(false);

  // Story 2.1 AC 12: a brand-new user with zero appointments AND zero calendar connections sees an
  // inviting connect-prompt instead of the plain empty message — someone who already connected but
  // hasn't had a sync cycle run yet (still zero appointments) should not be re-prompted to connect.
  readonly hasAnyConnection = signal(true);

  // Story 2.3: a convenience-only nav gate — the real enforcement is the API's "Admin" policy
  // (AD-17). A Member who navigates to /admin/sync-overview directly still gets a 403 from the API.
  readonly isAdmin = signal(false);

  readonly hourLabels = HOUR_LABELS;

  readonly weekDays = computed(() => getWeekDays(this.focusDate()));
  readonly dayDate = computed(() => startOfDay(this.focusDate()));
  // Story 3.1: a colleague selection means there's real content to show (their columns) even when the
  // viewer's own calendar is empty — the empty-state message must not hide the multi-person view.
  readonly isEmpty = computed(
    () => !this.loading() && this.appointments().length === 0 && this.selectedPersonIds().length === 0
  );
  readonly showConnectPrompt = computed(() => this.isEmpty() && !this.hasAnyConnection());

  private readonly appointmentsByDay = computed(() => groupByDay(this.appointments()));

  // Built once per `colleagueAppointments()` change (Angular memoizes `computed()` by its signal
  // dependencies) — `colleagueAppointmentsFor` below is called 7×(colleague count) times per week-view
  // change-detection cycle; querying a pre-grouped index avoids re-grouping each colleague's full
  // appointment array from scratch on every one of those calls (code review finding, Story 3.1/3.2).
  private readonly colleagueDayIndex = computed(() => buildColleagueDayIndex(this.colleagueAppointments()));

  readonly createFormOpen = signal(false);
  readonly createFormPrefillStart = signal<Date | null>(null);

  readonly detailOpen = signal(false);
  readonly detailAppointmentId = signal<string | null>(null);

  // Story 3.1: selected colleagues persist across Day/Week switches (AC 9) — plain component state,
  // untouched by setViewType.
  readonly roster = signal<PersonSummary[]>([]);
  readonly selectedPersonIds = signal<string[]>([]);
  readonly colleagueAppointments = signal<Record<string, ColleagueAppointmentSlot[]>>({});

  // Day-major, person-minor: for every visible day, "own" always comes first, then one entry per
  // selected colleague (AC 2's "eigene Spalte zuerst" applied per day-cluster). Empty selection yields
  // exactly the pre-Epic-3 single-column-per-day shape, so nothing regresses visually until a colleague
  // is actually picked.
  readonly columnOwners = computed<ColumnOwner[]>(() => [
    { personId: null, label: '' },
    ...this.selectedPersonIds().map((personId) => ({ personId, label: this.colleagueLabel(personId) })),
  ]);

  private loadedMonthKey: string | null = null;
  private loadedColleagueKey: string | null = null;

  ngOnInit(): void {
    this.loadAppointmentsForFocusMonth();
    this.loadColleagueAppointments();
    this.calendarService.getPersons().subscribe({
      next: (people) => this.roster.set(people),
      error: () => {},
    });
    this.connectionsService.getConnections().subscribe({
      next: (connections) => this.hasAnyConnection.set(connections.some((c) => c.connected)),
      // Leave hasAnyConnection at its default (true) on failure — showing the plain empty state
      // rather than an unwarranted connect-prompt is the safer failure mode here.
      error: () => {},
    });
    this.authService.me().subscribe({
      next: (person) => this.isAdmin.set(person.role === 'Admin'),
      error: () => {},
    });
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
    this.loadColleagueAppointments();
  }

  appointmentsFor(date: Date): Appointment[] {
    return this.appointmentsByDay().get(dateKey(date)) ?? [];
  }

  colleagueLabel(personId: string): string {
    return this.roster().find((person) => person.id === personId)?.email ?? '';
  }

  appointmentsForOwner(personId: string | null, date: Date): CalendarSlot[] {
    return personId === null ? this.appointmentsFor(date) : this.colleagueAppointmentsFor(personId, date);
  }

  colleagueAppointmentsFor(personId: string, date: Date): ColleagueAppointmentSlot[] {
    return this.colleagueDayIndex()[personId]?.get(dateKey(date)) ?? [];
  }

  onSelectionChanged(personIds: string[]): void {
    this.selectedPersonIds.set(personIds);
    this.loadColleagueAppointments();
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
    this.loadColleagueAppointments();
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

  /** Mirrors `loadAppointmentsForFocusMonth`'s month-grid range, but keyed on (month, selection) since
   *  a selection change must refetch even when the visible month didn't (Story 3.1, AC 7). Skips the
   *  request entirely for an empty selection (AC 8 needs no data, not an empty successful response). */
  private loadColleagueAppointments(): void {
    const personIds = this.selectedPersonIds();
    if (personIds.length === 0) {
      this.colleagueAppointments.set({});
      this.loadedColleagueKey = null;
      return;
    }

    const focus = this.focusDate();
    const monthKey = `${focus.getFullYear()}-${focus.getMonth()}`;
    const colleagueKey = `${monthKey}|${[...personIds].sort().join(',')}`;
    if (colleagueKey === this.loadedColleagueKey) {
      return;
    }

    const gridDays = getMonthGridDays(focus);
    const from = gridDays[0];
    const to = addDays(gridDays[gridDays.length - 1], 1);

    this.calendarService.getColleagueAppointments(personIds, from, to).subscribe({
      next: (result) => {
        this.colleagueAppointments.set(result);
        this.loadedColleagueKey = colleagueKey;
      },
      error: () => {},
    });
  }
}
