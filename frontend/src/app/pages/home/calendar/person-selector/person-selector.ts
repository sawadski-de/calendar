import { Component, ElementRef, ViewChild, computed, input, model, signal } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { FocusTrap } from '../../../../shared/focus-trap/focus-trap';
import { Activatable } from '../../../../shared/activatable/activatable';
import { PersonSummary } from '../calendar.service';

/**
 * Epic 3's general-purpose, page-level colleague selector — deliberately not a reuse of
 * `attendee-picker` (form-scoped, no keyboard row-navigation, no unlimited horizontal chip scroll,
 * different lifetime: that one lives inside the create form, this one lives in `Home` and persists
 * across Day/Week view switches, Story 3.1 AC 9). Roster is passed in rather than fetched here — `Home`
 * fetches it once and reuses it both for this component and for colleague-column header labels.
 */
@Component({
  selector: 'app-person-selector',
  standalone: true,
  imports: [TranslocoPipe, FocusTrap, Activatable],
  templateUrl: './person-selector.html',
  styleUrl: './person-selector.css',
})
export class PersonSelector {
  readonly roster = input<PersonSummary[]>([]);
  readonly selectedPersonIds = model<string[]>([]);

  @ViewChild('searchInput') private searchInputRef?: ElementRef<HTMLInputElement>;

  readonly dropdownOpen = signal(false);
  readonly filterText = signal('');
  readonly focusedIndex = signal(0);

  readonly selectedPeople = computed(() =>
    this.roster().filter((person) => this.selectedPersonIds().includes(person.id))
  );

  readonly filteredAvailable = computed(() => {
    const filter = this.filterText().toLowerCase();
    return this.roster()
      .filter((person) => !this.selectedPersonIds().includes(person.id))
      .filter((person) => person.email.toLowerCase().includes(filter));
  });

  openDropdown(): void {
    this.dropdownOpen.set(true);
    this.filterText.set('');
    this.focusedIndex.set(0);
  }

  closeDropdown(): void {
    this.dropdownOpen.set(false);
  }

  onFilterChange(value: string): void {
    this.filterText.set(value);
    this.focusedIndex.set(0);
  }

  addPerson(personId: string): void {
    if (!this.selectedPersonIds().includes(personId)) {
      this.selectedPersonIds.update((ids) => [...ids, personId]);
    }
    // AC 1: adding keeps the dropdown open — the filtered list shrinks by one, clamp the focus index.
    this.focusedIndex.update((index) => Math.min(index, Math.max(this.filteredAvailable().length - 1, 0)));
    this.searchInputRef?.nativeElement.focus();
  }

  removePerson(personId: string): void {
    this.selectedPersonIds.update((ids) => ids.filter((id) => id !== personId));
  }

  moveFocus(direction: 1 | -1): void {
    const count = this.filteredAvailable().length;
    if (count === 0) {
      return;
    }
    this.focusedIndex.update((index) => Math.min(Math.max(index + direction, 0), count - 1));
  }

  addFocused(): void {
    const focused = this.filteredAvailable()[this.focusedIndex()];
    if (focused) {
      this.addPerson(focused.id);
    }
  }
}
