import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { firstValueFrom, Observable } from 'rxjs';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('allows activation when the session is valid (GET /api/auth/me succeeds)', async () => {
    const result$ = TestBed.runInInjectionContext(() =>
      authGuard(undefined as never, undefined as never)
    ) as Observable<unknown>;
    const resultPromise = firstValueFrom(result$);

    httpMock.expectOne('/api/auth/me').flush({ id: '1', email: 'a@b.com' });

    expect(await resultPromise).toBe(true);
  });

  it('redirects to /login when the session is invalid (AC 3)', async () => {
    const result$ = TestBed.runInInjectionContext(() =>
      authGuard(undefined as never, undefined as never)
    ) as Observable<{ toString(): string }>;
    const resultPromise = firstValueFrom(result$);

    httpMock.expectOne('/api/auth/me').flush(null, { status: 401, statusText: 'Unauthorized' });

    const result = await resultPromise;
    expect(result.toString()).toBe('/login');
  });
});
