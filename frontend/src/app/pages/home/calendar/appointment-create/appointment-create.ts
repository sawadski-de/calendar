import { Component, ElementRef, OnInit, ViewChild, computed, inject, input, output, signal } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { FocusTrap } from '../../../../shared/focus-trap/focus-trap';
import { Appointment } from '../appointment.model';
import { AttendeePicker } from '../attendee-picker/attendee-picker';
import { CalendarService } from '../calendar.service';

function toDateInputValue(date: Date): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

function toTimeInputValue(date: Date): string {
  return `${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`;
}

const DEFAULT_DURATION_MINUTES = 30;

/**
 * The appointment-create form (Story 1.3). Two open modes: pre-filled (an empty time slot was
 * activated — `prefillStart` set) and blank (the toolbar button — `prefillStart` null/absent, AC 2).
 * Sharing the popover shape/behavior via `appFocusTrap` (backdrop click + Esc both cancel and,
 * through the trap's own teardown, restore focus to whatever triggered the form — see focus-trap.ts).
 */
@Component({
  selector: 'app-appointment-create',
  standalone: true,
  imports: [TranslocoPipe, FocusTrap, AttendeePicker],
  templateUrl: './appointment-create.html',
  styleUrl: './appointment-create.css',
})
export class AppointmentCreate implements OnInit {
  private readonly calendarService = inject(CalendarService);

  readonly prefillStart = input<Date | null>(null);
  readonly saved = output<Appointment>();
  readonly cancelled = output<void>();

  @ViewChild('titleInput') private titleInputRef?: ElementRef<HTMLInputElement>;

  readonly title = signal('');
  readonly dateValue = signal('');
  readonly startTime = signal('');
  readonly durationMinutes = signal(DEFAULT_DURATION_MINUTES);
  readonly attendeePersonIds = signal<string[]>([]);

  readonly titleError = signal(false);
  readonly timeRangeError = signal(false);
  readonly attendeeError = signal(false);

  readonly endTimeLabel = computed(() => {
    const end = this.composeEnd();
    return end ? toTimeInputValue(end) : '—';
  });

  ngOnInit(): void {
    const prefill = this.prefillStart();
    if (prefill) {
      this.dateValue.set(toDateInputValue(prefill));
      this.startTime.set(toTimeInputValue(prefill));
    }
  }

  setAttendeePersonIds(ids: string[]): void {
    this.attendeePersonIds.set(ids);
  }

  save(): void {
    if (!this.title().trim()) {
      this.titleError.set(true);
      this.timeRangeError.set(false);
      this.attendeeError.set(false);
      this.titleInputRef?.nativeElement.focus();
      return;
    }

    const start = this.composeStart();
    const end = this.composeEnd();
    if (!start || !end || end <= start) {
      this.timeRangeError.set(true);
      this.titleError.set(false);
      this.attendeeError.set(false);
      return;
    }

    this.titleError.set(false);
    this.timeRangeError.set(false);
    this.attendeeError.set(false);

    this.calendarService
      .createAppointment({
        title: this.title().trim(),
        startUtc: start.toISOString(),
        endUtc: end.toISOString(),
        attendeePersonIds: this.attendeePersonIds(),
      })
      .subscribe({
        next: (appointment) => this.saved.emit(appointment),
        error: (err: { error?: { code?: string } }) => {
          const code = err.error?.code;
          if (code === 'title-required') {
            this.titleError.set(true);
            this.titleInputRef?.nativeElement.focus();
          } else if (code === 'invalid-time-range') {
            this.timeRangeError.set(true);
          } else if (code === 'attendee-not-found') {
            // Rare race — a selected attendee left the roster between picking and saving.
            this.attendeeError.set(true);
          }
        },
      });
  }

  cancel(): void {
    this.cancelled.emit();
  }

  private composeStart(): Date | null {
    if (!this.dateValue() || !this.startTime()) {
      return null;
    }

    const [year, month, day] = this.dateValue().split('-').map(Number);
    const [hours, minutes] = this.startTime().split(':').map(Number);
    return new Date(year, month - 1, day, hours, minutes);
  }

  private composeEnd(): Date | null {
    const start = this.composeStart();
    return start ? new Date(start.getTime() + this.durationMinutes() * 60_000) : null;
  }
}
