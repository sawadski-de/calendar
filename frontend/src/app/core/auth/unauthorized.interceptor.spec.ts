import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { vi } from 'vitest';
import { unauthorizedInterceptor } from './unauthorized.interceptor';

describe('unauthorizedInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([unauthorizedInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => httpMock.verify());

  it('redirects to /login on a 401 mid-session from a protected endpoint (AC 13)', () => {
    const navigateSpy = vi.spyOn(router, 'navigateByUrl');

    http.post('/api/admin/persons', {}).subscribe({ error: () => undefined });
    httpMock.expectOne('/api/admin/persons').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(navigateSpy).toHaveBeenCalledWith('/login');
  });

  it('does not redirect when the login endpoint itself returns 401 (AC 12)', () => {
    const navigateSpy = vi.spyOn(router, 'navigateByUrl');

    http.post('/api/auth/login', {}).subscribe({ error: () => undefined });
    httpMock.expectOne('/api/auth/login').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(navigateSpy).not.toHaveBeenCalled();
  });

  it('does not redirect on /api/auth/me — authGuard handles that 401 itself', () => {
    const navigateSpy = vi.spyOn(router, 'navigateByUrl');

    http.get('/api/auth/me').subscribe({ error: () => undefined });
    httpMock.expectOne('/api/auth/me').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(navigateSpy).not.toHaveBeenCalled();
  });
});
