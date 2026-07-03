import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { minutesSince } from '../../../shared/sync-time';
import { CalendarConnectionStatus, CalendarProviderId } from './connection.model';
import { ConnectionsService } from './connections.service';

const PROVIDER_DISPLAY_NAMES: Record<CalendarProviderId, string> = {
  Google: 'Google',
  Outlook: 'Outlook',
};

/**
 * Settings → Kalenderverbindungen (Story 2.1 Google, Story 2.2 Outlook). "Verbinden" is a full-page
 * navigation to `/api/calendar-connections/{provider}/authorize`, not an HttpClient call — the browser
 * has to follow the provider's own redirect chain, which an XHR can't do. The page picks the flow back
 * up via the `connected`/`error` query params the callback endpoint redirects back with.
 */
@Component({
  selector: 'app-connections',
  standalone: true,
  imports: [TranslocoPipe, RouterLink],
  templateUrl: './connections.html',
  styleUrl: './connections.css',
})
export class Connections implements OnInit {
  private readonly connectionsService = inject(ConnectionsService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly connections = signal<CalendarConnectionStatus[]>([]);
  readonly loading = signal(true);
  readonly callbackNotice = signal<{ kind: 'connected' | 'error'; provider?: string; errorCode?: string } | null>(null);

  ngOnInit(): void {
    this.consumeCallbackQueryParams();
    this.loadConnections();
  }

  displayName(provider: CalendarProviderId): string {
    return PROVIDER_DISPLAY_NAMES[provider];
  }

  authorizeUrl(provider: CalendarProviderId): string {
    return this.connectionsService.authorizeUrl(provider);
  }

  syncLineParams(connection: CalendarConnectionStatus): { minutes: number } {
    return { minutes: minutesSince(connection.lastSuccessfulSyncAt) };
  }

  private loadConnections(): void {
    this.loading.set(true);
    this.connectionsService.getConnections().subscribe({
      next: (connections) => {
        this.connections.set(connections);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  private consumeCallbackQueryParams(): void {
    const params = this.route.snapshot.queryParamMap;
    const connected = params.get('connected');
    const error = params.get('error');

    if (connected) {
      // The callback redirects with a lowercase provider id (e.g. "google") — capitalize for display.
      const provider = connected.charAt(0).toUpperCase() + connected.slice(1);
      this.callbackNotice.set({ kind: 'connected', provider });
    } else if (error) {
      this.callbackNotice.set({ kind: 'error', errorCode: error });
    }

    if (connected || error) {
      // Strip the one-time callback params so a page refresh doesn't re-show the notice.
      void this.router.navigate([], { relativeTo: this.route, queryParams: {}, replaceUrl: true });
    }
  }
}
