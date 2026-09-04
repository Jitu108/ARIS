import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { LoginComponent } from './login.component';
import { ProblemDetails, LoginResponse } from '../../core/auth/auth.models';

describe('LoginComponent', () => {
  let httpMock: HttpTestingController;
  let router: Router;

  // FR-1.2: the backend returns this identical problem-details body whether the username is
  // unknown, the password is wrong, or the account is inactive — there is no field-specific
  // variant for the component to accidentally special-case.
  const genericInvalidCredentials: ProblemDetails = {
    type: 'https://aris.dev/problems/invalid-credentials',
    title: 'Invalid credentials.',
    status: 401,
    detail: 'Invalid username or password.',
    traceId: 'trace-1',
  };

  function createComponent() {
    const fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();
    return fixture;
  }

  function submit(fixture: ReturnType<typeof createComponent>, username: string, password: string): void {
    const component = fixture.componentInstance;
    component.username = username;
    component.password = password;
    component.handleSubmit();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });

    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('shows the generic backend message for an unknown username', () => {
    const fixture = createComponent();
    submit(fixture, 'nobody', 'whatever');

    httpMock.expectOne('/identity/login').flush(genericInvalidCredentials, { status: 401, statusText: 'Unauthorized' });

    expect(fixture.componentInstance.errorMessage()).toBe('Invalid username or password.');
  });

  it('shows the exact same message for a wrong password as for an unknown username', () => {
    const fixture = createComponent();
    submit(fixture, 'admin', 'wrong-password');

    // Same body the backend sends for the unknown-username case above — same type/title/status/detail,
    // only traceId differs, per Technical Documentation §4.1. The component must not branch on it.
    httpMock.expectOne('/identity/login').flush(
      { ...genericInvalidCredentials, traceId: 'trace-2' },
      { status: 401, statusText: 'Unauthorized' },
    );

    expect(fixture.componentInstance.errorMessage()).toBe('Invalid username or password.');
  });

  it('shows the same message for a deactivated account as for any other invalid login', () => {
    const fixture = createComponent();
    submit(fixture, 'deactivated-user', 'correct-password');

    httpMock.expectOne('/identity/login').flush(
      { ...genericInvalidCredentials, traceId: 'trace-3' },
      { status: 401, statusText: 'Unauthorized' },
    );

    expect(fixture.componentInstance.errorMessage()).toBe('Invalid username or password.');
  });

  it('falls back to a generic client-side message when the error has no problem-details body', () => {
    const fixture = createComponent();
    submit(fixture, 'admin', 'Admin@12345');

    httpMock.expectOne('/identity/login').error(new ProgressEvent('error'));

    expect(fixture.componentInstance.errorMessage()).toBe('Something went wrong. Please try again.');
  });

  it('renders the error message in the login form when present', () => {
    const fixture = createComponent();
    submit(fixture, 'nobody', 'whatever');

    httpMock.expectOne('/identity/login').flush(genericInvalidCredentials, { status: 401, statusText: 'Unauthorized' });
    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('.error-banner');
    expect(banner?.textContent).toContain('Invalid username or password.');
  });

  it('rejects an empty submission locally, without calling the backend', () => {
    const fixture = createComponent();
    submit(fixture, '', '');

    expect(fixture.componentInstance.errorMessage()).toBe('Enter your username and password.');
    httpMock.expectNone('/identity/login');
  });

  it('navigates to "/" on a successful login', () => {
    const fixture = createComponent();
    const navigateByUrl = vi.spyOn(router, 'navigateByUrl');
    submit(fixture, 'admin', 'Admin@12345');

    const response: LoginResponse = {
      accessToken: 'access-token',
      refreshToken: 'refresh-token',
      expiresInSeconds: 1800,
      user: { id: 'user-1', displayName: 'Admin', roles: ['Administrator'] },
      mustChangePassword: false,
    };
    httpMock.expectOne('/identity/login').flush(response);

    expect(navigateByUrl).toHaveBeenCalledWith('/');
  });
});
