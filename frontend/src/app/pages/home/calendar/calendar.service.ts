import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Appointment, AvailabilityStatus, ColleagueAppointmentSlot } from './appointment.model';

export interface CreateAppointmentRequest {
  title: string;
  startUtc: string;
  endUtc: string;
  attendeePersonIds: string[];
}

export interface PersonSummary {
  id: string;
  email: string;
}

export interface AttendeeSummary {
  personId: string | null;
  email: string;
}

/**
 * `isFullDetail` decides which of the other fields are populated (Story 3.1, FR-9): `false` means the
 * viewer is neither owner nor a listed attendee, so `title`/`startUtc`/`endUtc`/`attendees` are `null`
 * and only `status` is meaningful — the server enforces this, the client never receives more.
 */
export interface AppointmentDetail {
  id: string;
  isFullDetail: boolean;
  title: string | null;
  startUtc: string | null;
  endUtc: string | null;
  status: AvailabilityStatus;
  attendees: AttendeeSummary[] | null;
}

@Injectable({ providedIn: 'root' })
export class CalendarService {
  private readonly http = inject(HttpClient);

  getAppointments(fromUtc: Date, toUtc: Date): Observable<Appointment[]> {
    const params = new HttpParams().set('from', fromUtc.toISOString()).set('to', toUtc.toISOString());
    return this.http.get<Appointment[]>('/api/appointments', { params });
  }

  createAppointment(request: CreateAppointmentRequest): Observable<Appointment> {
    return this.http.post<Appointment>('/api/appointments', request);
  }

  getPersons(): Observable<PersonSummary[]> {
    return this.http.get<PersonSummary[]>('/api/persons');
  }

  getAppointmentDetail(id: string): Observable<AppointmentDetail> {
    return this.http.get<AppointmentDetail>(`/api/appointments/${id}`);
  }

  getColleagueAppointments(
    personIds: string[],
    fromUtc: Date,
    toUtc: Date
  ): Observable<Record<string, ColleagueAppointmentSlot[]>> {
    let params = new HttpParams().set('from', fromUtc.toISOString()).set('to', toUtc.toISOString());
    for (const personId of personIds) {
      params = params.append('personIds', personId);
    }
    return this.http.get<Record<string, ColleagueAppointmentSlot[]>>('/api/appointments/colleagues', { params });
  }
}
