import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Appointment, AvailabilityStatus } from './appointment.model';

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
  personId: string;
  email: string;
}

export interface AppointmentDetail {
  id: string;
  title: string;
  startUtc: string;
  endUtc: string;
  status: AvailabilityStatus;
  attendees: AttendeeSummary[];
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
}
