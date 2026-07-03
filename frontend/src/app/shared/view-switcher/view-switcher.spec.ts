import { ComponentFixture, TestBed } from '@angular/core/testing';
import { getTranslocoTestingModule } from '../../testing/transloco-testing';
import { ViewSwitcher } from './view-switcher';

describe('ViewSwitcher', () => {
  let fixture: ComponentFixture<ViewSwitcher>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ViewSwitcher, getTranslocoTestingModule()],
    }).compileComponents();

    fixture = TestBed.createComponent(ViewSwitcher);
    fixture.componentRef.setInput('active', 'week');
    fixture.detectChanges();
  });

  it('exposes radiogroup/radio semantics (AC 4)', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[role="radiogroup"]')).not.toBeNull();
    expect(compiled.querySelectorAll('[role="radio"]').length).toBe(3);
  });

  it('marks the active option as aria-checked and the others as not', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const radios = Array.from(compiled.querySelectorAll('[role="radio"]'));
    const checked = radios.filter((r) => r.getAttribute('aria-checked') === 'true');
    expect(checked).toHaveLength(1);
    expect(checked[0].textContent?.trim().length).toBeGreaterThan(0);
  });

  it('emits activeChange with the newly selected view when a different option is clicked', () => {
    const emitted: string[] = [];
    fixture.componentInstance.activeChange.subscribe((v) => emitted.push(v));

    const compiled = fixture.nativeElement as HTMLElement;
    const dayButton = Array.from(compiled.querySelectorAll('[role="radio"]')).at(-1) as HTMLButtonElement;
    dayButton.click();

    expect(emitted).toEqual(['day']);
  });

  it('does not emit when clicking the already-active option', () => {
    const emitted: string[] = [];
    fixture.componentInstance.activeChange.subscribe((v) => emitted.push(v));

    const compiled = fixture.nativeElement as HTMLElement;
    const weekButton = Array.from(compiled.querySelectorAll('[role="radio"]'))[1] as HTMLButtonElement;
    weekButton.click();

    expect(emitted).toEqual([]);
  });
});
