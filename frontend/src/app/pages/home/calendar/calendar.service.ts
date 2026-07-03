import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Appointment } from './appointment.model';

@Injectable({ providedIn: 'root' })
export class CalendarService {
  private readonly http = inject(HttpClient);

  getAppointments(fromUtc: Date, toUtc: Date): Observable<Appointment[]> {
    const params = new HttpParams().set('from', fromUtc.toISOString()).set('to', toUtc.toISOString());
    return this.http.get<Appointment[]>('/api/appointments', { params });
  }
}
