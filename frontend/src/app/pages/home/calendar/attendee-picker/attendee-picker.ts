import { Component, OnInit, computed, inject, model, signal } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { CalendarService, PersonSummary } from '../calendar.service';

/**
 * A small, form-scoped attendee picker for the appointment-create form — not Epic 3's general
 * person-selector (different cap/scroll/persistence needs, different surface). Displays email, not a
 * name, since `Person` has no display-name field (see Story 1.3 Dev Notes).
 */
@Component({
  selector: 'app-attendee-picker',
  standalone: true,
  imports: [TranslocoPipe],
  templateUrl: './attendee-picker.html',
  styleUrl: './attendee-picker.css',
})
export class AttendeePicker implements OnInit {
  private readonly calendarService = inject(CalendarService);

  readonly selectedPersonIds = model<string[]>([]);

  readonly roster = signal<PersonSummary[]>([]);
  readonly dropdownOpen = signal(false);
  readonly filterText = signal('');

  readonly selectedPeople = computed(() =>
    this.roster().filter((person) => this.selectedPersonIds().includes(person.id))
  );

  readonly filteredAvailable = computed(() => {
    const filter = this.filterText().toLowerCase();
    return this.roster()
      .filter((person) => !this.selectedPersonIds().includes(person.id))
      .filter((person) => person.email.toLowerCase().includes(filter));
  });

  ngOnInit(): void {
    this.calendarService.getPersons().subscribe((people) => this.roster.set(people));
  }

  openDropdown(): void {
    this.dropdownOpen.set(true);
    this.filterText.set('');
  }

  closeDropdown(): void {
    this.dropdownOpen.set(false);
  }

  addAttendee(personId: string): void {
    if (!this.selectedPersonIds().includes(personId)) {
      this.selectedPersonIds.update((ids) => [...ids, personId]);
    }
  }

  removeAttendee(personId: string): void {
    this.selectedPersonIds.update((ids) => ids.filter((id) => id !== personId));
  }
}
