import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminCalendarConnectionRow } from './sync-overview.model';

@Injectable({ providedIn: 'root' })
export class SyncOverviewService {
  private readonly http = inject(HttpClient);

  getOverview(): Observable<AdminCalendarConnectionRow[]> {
    return this.http.get<AdminCalendarConnectionRow[]>('/api/admin/calendar-connections');
  }
}
