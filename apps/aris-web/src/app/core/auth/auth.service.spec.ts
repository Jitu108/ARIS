import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { LoginResponse } from './auth.models';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  const loginResponse: LoginResponse = {
    accessToken: 'access-token',
    refreshToken: 'refresh-token',
    expiresInSeconds: 1800,
    user: { id: 'user-1', displayName: 'Test User', roles: ['Clinician'] },
    mustChangePassword: false,
  };

  function login(): void {
    service.login({ username: 'admin', password: 'Admin@12345' }).subscribe();
    httpMock.expectOne('/identity/login').flush(loginResponse);
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('clears the session synchronously, before the /identity/logout call settles', () => {
    login();
    expect(service.isAuthenticated()).toBe(true);

    service.logout();

    expect(service.isAuthenticated()).toBe(false);
    expect(service.getAccessToken()).toBeNull();
    expect(service.currentUser()).toBeNull();

    httpMock.expectOne('/identity/logout').flush(null);
  });

  it('sends the refresh token and the pre-clear access token as bearer auth to /identity/logout', () => {
    login();

    service.logout();

    const req = httpMock.expectOne('/identity/logout');
    expect(req.request.body).toEqual({ refreshToken: 'refresh-token' });
    expect(req.request.headers.get('Authorization')).toBe('Bearer access-token');
    req.flush(null);
  });

  it('leaves the session cleared even when the revoke call fails', () => {
    login();

    service.logout();

    httpMock.expectOne('/identity/logout').flush('server error', {
      status: 500,
      statusText: 'Internal Server Error',
    });

    expect(service.isAuthenticated()).toBe(false);
  });

  it('does not call /identity/logout when there is no session to log out of', () => {
    service.logout();

    httpMock.expectNone('/identity/logout');
  });
});
