import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from '../auth/auth.service';
import { LoginResponse } from '../auth/auth.models';

describe('authInterceptor', () => {
  let httpClient: HttpClient;
  let httpMock: HttpTestingController;
  let authService: AuthService;
  let navigateByUrl: ReturnType<typeof vi.fn>;

  const loginResponse: LoginResponse = {
    accessToken: 'access-token',
    refreshToken: 'refresh-token',
    expiresInSeconds: 1800,
    user: { id: 'user-1', displayName: 'Test User', roles: ['Clinician'] },
    mustChangePassword: false,
  };

  beforeEach(() => {
    navigateByUrl = vi.fn();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: { navigateByUrl } },
      ],
    });

    httpClient = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);

    authService.login({ username: 'admin', password: 'Admin@12345' }).subscribe();
    httpMock.expectOne('/identity/login').flush(loginResponse);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('attaches the current access token to outgoing requests', () => {
    httpClient.get('/patients/123').subscribe();

    const req = httpMock.expectOne('/patients/123');
    expect(req.request.headers.get('Authorization')).toBe('Bearer access-token');
    req.flush({});
  });

  it('on a 401, silently refreshes and retries the original request once', () => {
    let result: unknown;
    httpClient.get('/patients/123').subscribe((response) => (result = response));

    httpMock.expectOne('/patients/123').flush('expired', { status: 401, statusText: 'Unauthorized' });

    const refreshReq = httpMock.expectOne('/identity/refresh');
    expect(refreshReq.request.body).toEqual({ refreshToken: 'refresh-token' });
    refreshReq.flush({ ...loginResponse, accessToken: 'access-token-2', refreshToken: 'refresh-token-2' });

    const retryReq = httpMock.expectOne('/patients/123');
    expect(retryReq.request.headers.get('Authorization')).toBe('Bearer access-token-2');
    retryReq.flush({ ok: true });

    expect(result).toEqual({ ok: true });
    expect(navigateByUrl).not.toHaveBeenCalled();
  });

  it('when the refresh itself fails, clears the session and redirects to /login', () => {
    let error: unknown;
    httpClient.get('/patients/123').subscribe({ error: (err) => (error = err) });

    httpMock.expectOne('/patients/123').flush('expired', { status: 401, statusText: 'Unauthorized' });
    httpMock.expectOne('/identity/refresh').flush('expired', { status: 401, statusText: 'Unauthorized' });

    expect(error).toBeTruthy();
    expect(authService.isAuthenticated()).toBe(false);
    expect(navigateByUrl).toHaveBeenCalledWith('/login');
  });

  it('does not attempt a refresh for a 401 on the login request itself', () => {
    let error: unknown;
    httpClient.post('/identity/login', { username: 'admin', password: 'wrong' }).subscribe({
      error: (err) => (error = err),
    });

    httpMock.expectOne('/identity/login').flush('bad credentials', { status: 401, statusText: 'Unauthorized' });

    expect(error).toBeTruthy();
    httpMock.expectNone('/identity/refresh');
    expect(navigateByUrl).not.toHaveBeenCalled();
  });
});
