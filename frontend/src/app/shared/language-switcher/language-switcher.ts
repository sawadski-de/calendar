import { Component, inject } from '@angular/core';
import { TranslocoService, TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'app-language-switcher',
  standalone: true,
  imports: [TranslocoPipe],
  templateUrl: './language-switcher.html',
  styleUrl: './language-switcher.css',
})
export class LanguageSwitcher {
  private readonly transloco = inject(TranslocoService);

  readonly languages = ['de', 'en'] as const;

  get activeLang(): string {
    return this.transloco.getActiveLang();
  }

  setLang(lang: string): void {
    this.transloco.setActiveLang(lang);
    localStorage.setItem('lang', lang);
  }
}
