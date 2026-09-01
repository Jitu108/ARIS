import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { ShellComponent } from './shell.component';
import { AuthService } from '../auth/auth.service';
import { LoginResponse } from '../auth/auth.models';

@Component({ standalone: true, template: '' })
class StubRouteComponent {}

describe('ShellComponent', () => {
  let httpMock: HttpTestingController;
  let authService: AuthService;

  const loginResponse: LoginResponse = {
    accessToken: 'access-token',
    refreshToken: 'refresh-token',
    expiresInSeconds: 1800,
    user: { id: 'user-1', displayName: 'Jane Clinician', roles: ['Clinician'] },
    mustChangePassword: false,
  };

  function login(): void {
    authService.login({ username: 'jane', password: 'Admin@12345' }).subscribe();
    httpMock.expectOne('/identity/login').flush(loginResponse);
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([
          { path: '', component: ShellComponent, children: [{ path: '', component: StubRouteComponent }] },
          { path: 'login', component: StubRouteComponent },
        ]),
      ],
    });

    httpMock = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it("renders the authenticated user's identity in the header", async () => {
    login();

    const harness = await RouterTestingHarness.create('/');
    const el = harness.routeNativeElement!;

    expect(el.querySelector('.account-name')?.textContent).toBe('Jane Clinician');
    expect(el.querySelector('.account-role')?.textContent).toBe('Clinician');
    expect(el.querySelector('.avatar')?.textContent?.trim()).toBe('JC');
  });

  it('marks the Dashboard nav item active via real router state on "/"', async () => {
    login();

    const harness = await RouterTestingHarness.create('/');
    const navLink = harness.routeNativeElement!.querySelector('a.nav-item') as HTMLAnchorElement;

    expect(navLink.classList.contains('active')).toBe(true);
    expect(navLink.getAttribute('href')).toBe('/');
  });

  it('shows the "Log out" control in the account menu and logs out through it', async () => {
    login();

    const harness = await RouterTestingHarness.create('/');
    const router = TestBed.inject(Router);
    const navigateByUrl = vi.spyOn(router, 'navigateByUrl');

    const el = harness.routeNativeElement!;
    (el.querySelector('.account-menu-trigger') as HTMLButtonElement).click();
    harness.detectChanges();

    const logoutButton = el.querySelector('.dropdown-row') as HTMLButtonElement;
    expect(logoutButton.textContent).toContain('Log out');

    logoutButton.click();
    harness.detectChanges();

    expect(authService.isAuthenticated()).toBe(false);
    expect(navigateByUrl).toHaveBeenCalledWith('/login');

    httpMock.expectOne('/identity/logout').flush(null);
  });
});
