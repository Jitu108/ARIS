import { Injectable, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, finalize, of, shareReplay, tap, throwError } from 'rxjs';
import { LoginRequest, LoginResponse, LoginUser, LogoutRequest, RefreshRequest } from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  // Access + refresh tokens are held in memory only (never localStorage), per Technical
  // Documentation §6.3 — reduces XSS exfiltration surface, at the cost of the session not
  // surviving a page reload.
  private accessToken: string | null = null;
  private refreshToken: string | null = null;

  // Coalesces concurrent 401s (e.g. several protected calls in flight when the access token
  // lapses) onto a single POST /identity/refresh — the backend rotates a refresh token exactly
  // once and rejects reuse, so firing one per caller would fail every caller but the first.
  private refreshInFlight: Observable<LoginResponse> | null = null;

  private readonly _currentUser = signal<LoginUser | null>(null);
  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this._currentUser() !== null);

  constructor(private readonly http: HttpClient) {}

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/identity/login', request).pipe(
      tap((response) => {
        this.accessToken = response.accessToken;
        this.refreshToken = response.refreshToken;
        this._currentUser.set(response.user);
      }),
    );
  }

  refresh(): Observable<LoginResponse> {
    if (this.refreshInFlight) {
      return this.refreshInFlight;
    }

    const refreshToken = this.refreshToken;
    if (!refreshToken) {
      return throwError(() => new Error('No active session to refresh.'));
    }

    const request$ = this.http.post<LoginResponse>('/identity/refresh', { refreshToken } satisfies RefreshRequest).pipe(
      tap((response) => {
        this.accessToken = response.accessToken;
        this.refreshToken = response.refreshToken;
        this._currentUser.set(response.user);
      }),
      finalize(() => {
        this.refreshInFlight = null;
      }),
      shareReplay(1),
    );

    this.refreshInFlight = request$;
    return request$;
  }

  logout(): void {
    const accessToken = this.accessToken;
    const refreshToken = this.refreshToken;

    // Clear client-side session state up front — protected pages/data must become inaccessible
    // (FR-1.4) even if the server-side revoke call below fails or never completes. The access
    // token is captured above and attached explicitly, since by request time authInterceptor
    // would otherwise find it already cleared.
    this.clearSession();

    if (refreshToken) {
      this.http
        .post<void>(
          '/identity/logout',
          { refreshToken } satisfies LogoutRequest,
          accessToken ? { headers: { Authorization: `Bearer ${accessToken}` } } : {},
        )
        .pipe(catchError(() => of(void 0)))
        .subscribe();
    }
  }

  // Drops local session state without calling the server — used when a silent refresh fails
  // (the refresh token itself has lapsed, so there's nothing left to revoke).
  clearSession(): void {
    this.accessToken = null;
    this.refreshToken = null;
    this._currentUser.set(null);
  }

  getAccessToken(): string | null {
    return this.accessToken;
  }
}
