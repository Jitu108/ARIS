import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRouteSnapshot, provideRouter, RouterStateSnapshot, UrlTree } from '@angular/router';
import { adminGuard } from './admin.guard';
import { AuthService } from '../auth/auth.service';
import { LoginResponse } from '../auth/auth.models';

// FR-6.6: adminGuard must block navigation for anyone who isn't authenticated as an
// Administrator — client-side convenience only, the backend enforces this independently.
describe('adminGuard', () => {
  let authService: AuthService;
  let httpMock: HttpTestingController;

  function loginResponseFor(roles: string[]): LoginResponse {
    return {
      accessToken: 'access-token',
      refreshToken: 'refresh-token',
      expiresInSeconds: 1800,
      user: { id: 'user-1', displayName: 'Test User', roles },
      mustChangePassword: false,
    };
  }

  function runGuard(): boolean | UrlTree {
    return TestBed.runInInjectionContext(() =>
      adminGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
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

    expect((result as UrlTree).toString()).toBe('/login');
  });

  it('redirects to / when the current user is not an Administrator', () => {
    authService.login({ username: 'coder', password: 'whatever' }).subscribe();
    httpMock.expectOne('/identity/login').flush(loginResponseFor(['Coder']));

    const result = runGuard();

    expect((result as UrlTree).toString()).toBe('/');
  });

  it('allows navigation for an Administrator', () => {
    authService.login({ username: 'admin', password: 'Admin@12345' }).subscribe();
    httpMock.expectOne('/identity/login').flush(loginResponseFor(['Administrator']));

    expect(runGuard()).toBe(true);
  });
});
