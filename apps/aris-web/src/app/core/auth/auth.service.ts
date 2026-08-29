import { Injectable, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of, tap } from 'rxjs';
import { LoginRequest, LoginResponse, LoginUser, LogoutRequest } from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  // Access + refresh tokens are held in memory only (never localStorage), per Technical
  // Documentation §6.3 — reduces XSS exfiltration surface, at the cost of the session not
  // surviving a page reload. Refresh-token persistence/rotation is TARIS-013's concern.
  private accessToken: string | null = null;
  private refreshToken: string | null = null;

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

  logout(): void {
    const accessToken = this.accessToken;
    const refreshToken = this.refreshToken;

    // Clear client-side session state up front — protected pages/data must become inaccessible
    // (FR-1.4) even if the server-side revoke call below fails or never completes. The access
    // token is captured above and attached explicitly, since by request time authInterceptor
    // would otherwise find it already cleared.
    this.accessToken = null;
    this.refreshToken = null;
    this._currentUser.set(null);

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

  getAccessToken(): string | null {
    return this.accessToken;
  }
}
