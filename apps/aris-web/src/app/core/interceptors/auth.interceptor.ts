import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';

// Attaches the bearer token to every request and, on 401, clears the session and redirects to
// /login. Does NOT attempt a silent refresh — POST /identity/refresh doesn't exist yet
// (TARIS-013), which is where that retry-once-then-redirect step gets added.
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
      // handles that error directly, so don't treat it as a session invalidation here.
      const isLoginRequest = req.url.startsWith('/identity/login');
      if (!isLoginRequest && error instanceof HttpErrorResponse && error.status === 401) {
        authService.logout();
        router.navigateByUrl('/login');
      }
      return throwError(() => error);
    }),
  );
};
