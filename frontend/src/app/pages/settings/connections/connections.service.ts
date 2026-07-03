import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CalendarConnectionStatus, CalendarProviderId } from './connection.model';

@Injectable({ providedIn: 'root' })
export class ConnectionsService {
  private readonly http = inject(HttpClient);

  getConnections(): Observable<CalendarConnectionStatus[]> {
    return this.http.get<CalendarConnectionStatus[]>('/api/calendar-connections');
  }

  /**
   * Full-page navigation, not an HttpClient call — the browser must follow the provider's own OAuth
   * redirect chain, which an XHR can't do (see connections.ts).
   */
  authorizeUrl(provider: CalendarProviderId): string {
    return `/api/calendar-connections/${provider.toLowerCase()}/authorize`;
  }
}
