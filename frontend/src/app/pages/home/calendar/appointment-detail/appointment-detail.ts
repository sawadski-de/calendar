import { Component, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { FocusTrap } from '../../../../shared/focus-trap/focus-trap';
import { AppointmentDetail, CalendarService } from '../calendar.service';
import { formatTime } from '../date-utils';
import { StatusBadge } from '../status-badge/status-badge';

/**
 * Read-only appointment detail popover (Story 1.4), extended in Story 3.1 with a second content mode:
 * `detail().isFullDetail` decides between full detail (own appointment, or a colleague's where the
 * viewer is a listed attendee, FR-9) and status-only + privacy note (every other colleague appointment)
 * — the server decides which fields are populated, this component just renders what it's given.
 * Shares the exact popover shape/behavior as `appointment-create` (backdrop + `appFocusTrap` panel) —
 * reuses `shared/focus-trap` and `status-badge`, both built in Story 1.3 specifically so this story
 * wouldn't need to rebuild them. No location field: `Appointment` has no location property at all
 * (FR-15 not built).
 */
@Component({
  selector: 'app-appointment-detail',
  standalone: true,
  imports: [TranslocoPipe, FocusTrap, StatusBadge],
  templateUrl: './appointment-detail.html',
  styleUrl: './appointment-detail.css',
})
export class AppointmentDetailPopover implements OnInit {
  private readonly calendarService = inject(CalendarService);

  readonly appointmentId = input.required<string>();
  readonly closed = output<void>();

  readonly detail = signal<AppointmentDetail | null>(null);
  readonly loadError = signal(false);

  readonly dateLabel = computed(() => {
    const detail = this.detail();
    if (!detail?.startUtc) {
      return '';
    }
    return new Date(detail.startUtc).toLocaleDateString(undefined, {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  });

  readonly timeRangeLabel = computed(() => {
    const detail = this.detail();
    if (!detail?.startUtc || !detail.endUtc) {
      return '';
    }
    return `${formatTime(new Date(detail.startUtc))}–${formatTime(new Date(detail.endUtc))}`;
  });

  ngOnInit(): void {
    this.calendarService.getAppointmentDetail(this.appointmentId()).subscribe({
      next: (detail) => this.detail.set(detail),
      // The endpoint 404s identically whether the appointment was deleted or never belonged to the
      // caller (no existence leak, see backend Dev Notes) — either way, tell the user rather than
      // leaving a blank overlay.
      error: () => this.loadError.set(true),
    });
  }

  close(): void {
    this.closed.emit();
  }
}
