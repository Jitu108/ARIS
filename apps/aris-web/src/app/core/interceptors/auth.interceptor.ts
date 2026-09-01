import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';

// Attaches the bearer token to every request. On a 401 from any other endpoint, attempts exactly
// one silent refresh (POST /identity/refresh) and retries the original request once; if the
// refresh itself fails, clears the session and redirects to /login — Technical Documentation §8.2.
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const token = authService.getAccessToken();
  const authedReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authedReq).pipe(
    catchError((error: unknown) => {
      // A failed /identity/login attempt is its own 401, not an expired session — LoginComponent
      // handles that error directly. A failed /identity/refresh call means the refresh token
      // itself is no longer usable, so retrying it here would only loop.
      const isAuthEndpoint = req.url.startsWith('/identity/login') || req.url.startsWith('/identity/refresh');
      if (isAuthEndpoint || !(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      return authService.refresh().pipe(
        switchMap((response) =>
          next(req.clone({ setHeaders: { Authorization: `Bearer ${response.accessToken}` } })),
        ),
        catchError((refreshError: unknown) => {
          authService.clearSession();
          router.navigateByUrl('/login');
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};
