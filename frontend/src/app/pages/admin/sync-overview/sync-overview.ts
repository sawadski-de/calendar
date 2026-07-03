import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { minutesSince } from '../../../shared/sync-time';
import { AdminCalendarConnectionRow } from './sync-overview.model';
import { SyncOverviewService } from './sync-overview.service';

/**
 * Admin → Sync-Übersicht (Story 2.3). The nav link to this page only renders for an admin
 * (Home component), but the real access control is server-side — a Member navigating here directly
 * gets a 403 from GET /api/admin/calendar-connections and sees the error state below (AD-17, AC 3).
 */
@Component({
  selector: 'app-sync-overview',
  standalone: true,
  imports: [TranslocoPipe, RouterLink],
  templateUrl: './sync-overview.html',
  styleUrl: './sync-overview.css',
})
export class SyncOverview implements OnInit {
  private readonly syncOverviewService = inject(SyncOverviewService);

  readonly rows = signal<AdminCalendarConnectionRow[]>([]);
  readonly loading = signal(true);
  readonly forbidden = signal(false);

  ngOnInit(): void {
    this.syncOverviewService.getOverview().subscribe({
      next: (rows) => {
        this.rows.set(rows);
        this.loading.set(false);
      },
      error: (err) => {
        this.forbidden.set(err?.status === 403);
        this.loading.set(false);
      },
    });
  }

  syncLineParams(row: AdminCalendarConnectionRow): { minutes: number } {
    return { minutes: minutesSince(row.lastSuccessfulSyncAt) };
  }
}
