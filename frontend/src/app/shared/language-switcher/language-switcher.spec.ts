import { TestBed } from '@angular/core/testing';
import { getTranslocoTestingModule } from '../../testing/transloco-testing';
import { LanguageSwitcher } from './language-switcher';

describe('LanguageSwitcher', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('defaults to German as the active language', async () => {
    await TestBed.configureTestingModule({
      imports: [LanguageSwitcher, getTranslocoTestingModule()],
    }).compileComponents();

    const fixture = TestBed.createComponent(LanguageSwitcher);
    fixture.detectChanges();

    const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll('button');
    expect(buttons[0].textContent?.trim()).toBe('DE');
    expect(buttons[0].getAttribute('aria-pressed')).toBe('true');
  });

  it('switches instantly to English on click and persists the choice (AC 6)', async () => {
    await TestBed.configureTestingModule({
      imports: [LanguageSwitcher, getTranslocoTestingModule()],
    }).compileComponents();

    const fixture = TestBed.createComponent(LanguageSwitcher);
    fixture.detectChanges();

    const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll('button');
    (buttons[1] as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(fixture.componentInstance.activeLang).toBe('en');
    expect(localStorage.getItem('lang')).toBe('en');
    expect((fixture.nativeElement as HTMLElement).querySelectorAll('button')[1].getAttribute('aria-pressed')).toBe(
      'true'
    );
  });

  it('the language buttons are natively keyboard-focusable (AC 8 prerequisite)', async () => {
    await TestBed.configureTestingModule({
      imports: [LanguageSwitcher, getTranslocoTestingModule()],
    }).compileComponents();

    const fixture = TestBed.createComponent(LanguageSwitcher);
    fixture.detectChanges();

    const button = (fixture.nativeElement as HTMLElement).querySelector('button') as HTMLButtonElement;
    button.focus();

    expect(document.activeElement).toBe(button);
  });
});
