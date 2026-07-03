import { Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { AvailabilityStatus } from '../appointment.model';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [TranslocoPipe],
  templateUrl: './status-badge.html',
  styleUrl: './status-badge.css',
})
export class StatusBadge {
  readonly status = input.required<AvailabilityStatus>();

  readonly isDnd = computed(() => this.status() === 'BitteNichtStoeren');
  readonly labelKey = computed(() =>
    this.isDnd() ? 'calendar.statusDnd' : 'calendar.statusInterruptible'
  );
}
