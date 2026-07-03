import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from './auth.service';

/**
 * Story 1.1, AC 3: the auth cookie is HttpOnly, so the only way to know whether a session is
 * still valid is to ask the backend (GET /api/auth/me) — there is no client-side session state.
 */
export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.me().pipe(
    map(() => true),
    catchError(() => of(router.parseUrl('/login')))
  );
};
