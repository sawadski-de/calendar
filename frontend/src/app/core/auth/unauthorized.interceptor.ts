import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

/**
 * Story 1.1, AC 13: a 401 mid-session (expired cookie) redirects to /login in a controlled way
 * instead of leaving the user on a broken screen. Two requests are excluded from this global
 * redirect: the login request's own 401 (bad credentials, AC 12) is handled inline by the login
 * page, and /api/auth/me's 401 is handled by authGuard itself (which already turns it into a
 * redirect) — including it here too would fire two competing navigations to /login.
 */
export const unauthorizedInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: unknown) => {
      const isExcluded = req.url.endsWith('/api/auth/login') || req.url.endsWith('/api/auth/me');
      if (error instanceof HttpErrorResponse && error.status === 401 && !isExcluded) {
        router.navigateByUrl('/login');
      }
      return throwError(() => error);
    })
  );
};
