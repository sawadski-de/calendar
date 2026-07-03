import { TranslocoTestingModule, TranslocoTestingOptions } from '@jsverse/transloco';

/**
 * Synchronous in-memory translations for specs — keeps component tests from depending on the
 * runtime HTTP loader (`public/i18n/*.json`) that only exists once the app is actually served.
 */
export function getTranslocoTestingModule(options: TranslocoTestingOptions = {}) {
  return TranslocoTestingModule.forRoot({
    langs: {
      de: {
        app: { title: 'Team-Terminkalender' },
        login: {
          heading: 'Anmelden',
          emailLabel: 'E-Mail',
          passwordLabel: 'Passwort',
          submit: 'Anmelden',
          genericError: 'Anmeldung fehlgeschlagen. Bitte überprüfe deine Zugangsdaten.',
        },
        nav: { logout: 'Abmelden' },
        calendar: {
          viewMonth: 'Monat',
          viewWeek: 'Woche',
          viewDay: 'Tag',
          viewSwitcherLabel: 'Ansicht',
          previous: 'Zurück',
          next: 'Weiter',
          today: 'Heute',
          empty: 'Du hast noch keine Termine.',
        },
        languageSwitcher: { groupLabel: 'Sprache', de: 'DE', en: 'EN' },
      },
      en: {
        app: { title: 'Team Calendar' },
        login: {
          heading: 'Sign in',
          emailLabel: 'Email',
          passwordLabel: 'Password',
          submit: 'Sign in',
          genericError: 'Sign-in failed. Please check your credentials.',
        },
        nav: { logout: 'Sign out' },
        calendar: {
          viewMonth: 'Month',
          viewWeek: 'Week',
          viewDay: 'Day',
          viewSwitcherLabel: 'View',
          previous: 'Previous',
          next: 'Next',
          today: 'Today',
          empty: "You don't have any appointments yet.",
        },
        languageSwitcher: { groupLabel: 'Language', de: 'DE', en: 'EN' },
      },
    },
    translocoConfig: {
      availableLangs: ['de', 'en'],
      defaultLang: 'de',
      fallbackLang: 'de',
      reRenderOnLangChange: true,
    },
    preloadLangs: true,
    ...options,
  });
}
