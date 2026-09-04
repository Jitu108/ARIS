import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRouteSnapshot, provideRouter, RouterStateSnapshot, UrlTree } from '@angular/router';
import { authGuard } from './auth.guard';
import { AuthService } from '../auth/auth.service';
import { LoginResponse } from '../auth/auth.models';

// FR-2.1 / UT-NG-04: authGuard must block navigation with no valid session, and let it through
// once one exists — the baseline every protected route (`app.routes.ts`) relies on.
describe('authGuard', () => {
  let authService: AuthService;
  let httpMock: HttpTestingController;

  const loginResponse: LoginResponse = {
    accessToken: 'access-token',
    refreshToken: 'refresh-token',
    expiresInSeconds: 1800,
    user: { id: 'user-1', displayName: 'Test User', roles: ['Clinician'] },
    mustChangePassword: false,
  };

  function runGuard(): boolean | UrlTree {
    return TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
    ) as boolean | UrlTree;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    authService = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('redirects to /login when there is no active session', () => {
    const result = runGuard();

    expect(result).not.toBe(true);
    expect((result as UrlTree).toString()).toBe('/login');
  });

  it('allows navigation once a session is active', () => {
    authService.login({ username: 'admin', password: 'Admin@12345' }).subscribe();
    httpMock.expectOne('/identity/login').flush(loginResponse);

    expect(runGuard()).toBe(true);
  });

  it('redirects to /login again once the session is cleared', () => {
    authService.login({ username: 'admin', password: 'Admin@12345' }).subscribe();
    httpMock.expectOne('/identity/login').flush(loginResponse);
    authService.clearSession();

    const result = runGuard();

    expect((result as UrlTree).toString()).toBe('/login');
  });
});
