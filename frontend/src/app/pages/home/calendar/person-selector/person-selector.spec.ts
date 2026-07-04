import { ComponentFixture, TestBed } from '@angular/core/testing';
import { getTranslocoTestingModule } from '../../../../testing/transloco-testing';
import { PersonSummary } from '../calendar.service';
import { PersonSelector } from './person-selector';

describe('PersonSelector (Story 3.1)', () => {
  let fixture: ComponentFixture<PersonSelector>;

  const roster: PersonSummary[] = [
    { id: 'p1', email: 'bjoern@example.com' },
    { id: 'p2', email: 'katharina@example.com' },
    { id: 'p3', email: 'mara@example.com' },
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PersonSelector, getTranslocoTestingModule()],
    }).compileComponents();

    fixture = TestBed.createComponent(PersonSelector);
    fixture.componentRef.setInput('roster', roster);
    fixture.detectChanges();
  });

  function addChip(): HTMLElement {
    return fixture.nativeElement.querySelector('.person-selector__add');
  }

  function searchInput(): HTMLInputElement {
    return fixture.nativeElement.querySelector('.person-selector__search');
  }

  function resultRows(): HTMLElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('.person-selector__result'));
  }

  it('shows the invitational hint when nothing is selected (AC 8)', () => {
    expect(fixture.nativeElement.textContent).toContain('Wähle Teammitglieder');
  });

  it('opens the dropdown with the full roster on clicking the "+" chip (AC 1)', () => {
    addChip().click();
    fixture.detectChanges();

    expect(fixture.componentInstance.dropdownOpen()).toBe(true);
    expect(resultRows().length).toBe(3);
  });

  it('filters the dropdown list as the user types (AC 1)', () => {
    addChip().click();
    fixture.detectChanges();

    const input = searchInput();
    input.value = 'kath';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(resultRows().length).toBe(1);
    expect(resultRows()[0].textContent).toContain('katharina@example.com');
  });

  it('arrow keys move the virtual focus and Enter adds the focused person while keeping the dropdown open (AC 1)', () => {
    addChip().click();
    fixture.detectChanges();

    const input = searchInput();
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
    fixture.detectChanges();
    expect(fixture.componentInstance.focusedIndex()).toBe(1);

    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
    fixture.detectChanges();

    expect(fixture.componentInstance.selectedPersonIds()).toEqual(['p2']);
    expect(fixture.componentInstance.dropdownOpen()).toBe(true);
  });

  it('Esc closes the dropdown without selecting anyone', () => {
    addChip().click();
    fixture.detectChanges();

    searchInput().dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    fixture.detectChanges();

    expect(fixture.componentInstance.dropdownOpen()).toBe(false);
    expect(fixture.componentInstance.selectedPersonIds()).toEqual([]);
  });

  it('removes a selected person immediately when "×" is clicked, no confirmation (AC 10)', () => {
    fixture.componentRef.setInput('selectedPersonIds', ['p1', 'p2']);
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.person-selector__chip-remove') as HTMLElement).click();
    fixture.detectChanges();

    expect(fixture.componentInstance.selectedPersonIds()).toEqual(['p2']);
  });

  it('has no cap on the number of selected people (AC 11, DESIGN.md)', () => {
    const allIds = roster.map((p) => p.id);
    fixture.componentRef.setInput('selectedPersonIds', allIds);
    fixture.detectChanges();

    expect(fixture.componentInstance.selectedPeople().length).toBe(roster.length);
    expect(fixture.nativeElement.querySelectorAll('.person-selector__chip').length).toBe(roster.length);
  });
});
