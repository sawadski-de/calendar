import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { vi } from 'vitest';
import { getTranslocoTestingModule } from '../../testing/transloco-testing';
import { Login } from './login';

describe('Login', () => {
  let fixture: ComponentFixture<Login>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Login, getTranslocoTestingModule()],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(Login);
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('shows no error before any submission', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[role="alert"]')).toBeNull();
  });

  it('shows the generic error message when login fails (AC 12)', () => {
    const component = fixture.componentInstance;
    component.form.setValue({ email: 'admin@example.com', password: 'wrong' });
    component.submit();

    const req = httpMock.expectOne('/api/auth/login');
    req.flush({ code: 'invalid-credentials' }, { status: 401, statusText: 'Unauthorized' });
    fixture.detectChanges();

    expect(component.error()).toBe(true);
    // Assert via the translation key's rendered value rather than a hardcoded literal — this stays
    // green through copy edits and still fails if the component stops using login.genericError.
    const transloco = TestBed.inject(TranslocoService);
    const expectedMessage = transloco.translate('login.genericError');
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[role="alert"]')?.textContent?.trim()).toBe(expectedMessage);
  });

  it('navigates to / on successful login', () => {
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigateByUrl');
    const component = fixture.componentInstance;

    component.form.setValue({ email: 'admin@example.com', password: 'Admin#12345' });
    component.submit();

    const req = httpMock.expectOne('/api/auth/login');
    req.flush(null);

    expect(navigateSpy).toHaveBeenCalledWith('/');
    expect(component.error()).toBe(false);
  });

  it('does not submit an incomplete form', () => {
    const component = fixture.componentInstance;
    component.form.setValue({ email: '', password: '' });
    component.submit();

    httpMock.expectNone('/api/auth/login');
  });
});
