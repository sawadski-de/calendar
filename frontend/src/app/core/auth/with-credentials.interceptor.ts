import { HttpInterceptorFn } from '@angular/common/http';

/**
 * Forces every request to send/receive the HttpOnly auth cookie (AD-10) — centralized here so no
 * future call site can forget it (Story 1.1: "All Angular HttpClient calls use withCredentials: true").
 */
export const withCredentialsInterceptor: HttpInterceptorFn = (req, next) =>
  next(req.clone({ withCredentials: true }));
