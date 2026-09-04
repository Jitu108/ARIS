import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CreateUserComponent } from './create-user.component';
import { CreateUserResponse } from '../../../core/users/user-management.models';
import { ProblemDetails } from '../../../core/auth/auth.models';

describe('CreateUserComponent', () => {
  let httpMock: HttpTestingController;

  function createComponent() {
    const fixture = TestBed.createComponent(CreateUserComponent);
    fixture.detectChanges();
    return fixture;
  }

  function fillAndSubmit(
    fixture: ReturnType<typeof createComponent>,
    overrides: Partial<{ username: string; email: string; displayName: string; password: string; roles: string[] }> = {},
  ): void {
    const component = fixture.componentInstance;
    component.username = overrides.username ?? 'jdoe';
    component.email = overrides.email ?? 'jdoe@aris.local';
    component.displayName = overrides.displayName ?? 'Jane Doe';
    component.password = overrides.password ?? 'P@ssword1';
    component.selectedRoles = new Set((overrides.roles ?? ['Coder']) as never[]);
    component.handleSubmit();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('rejects submission locally when a required field is missing, without calling the backend', () => {
    const fixture = createComponent();
    fillAndSubmit(fixture, { username: '' });

    expect(fixture.componentInstance.errorMessage()).toContain('required');
    httpMock.expectNone('/identity/users');
  });

  it('rejects submission locally when no role is selected', () => {
    const fixture = createComponent();
    fillAndSubmit(fixture, { roles: [] });

    expect(fixture.componentInstance.errorMessage()).toContain('required');
    httpMock.expectNone('/identity/users');
  });

  it('shows a success message and resets the form on 201, per FR-6.5 (usable immediately)', () => {
    const fixture = createComponent();
    fillAndSubmit(fixture);

    const response: CreateUserResponse = {
      id: 'user-1',
      username: 'jdoe',
      email: 'jdoe@aris.local',
      displayName: 'Jane Doe',
      roles: ['Coder'],
      isActive: true,
    };
    httpMock.expectOne('/identity/users').flush(response, { status: 201, statusText: 'Created' });

    expect(fixture.componentInstance.successMessage()).toContain('Jane Doe');
    expect(fixture.componentInstance.username).toBe('');
    expect(fixture.componentInstance.selectedRoles.size).toBe(0);
  });

  it('shows the backend-specific reason for a 409 duplicate username/email (FR-6.4)', () => {
    const fixture = createComponent();
    fillAndSubmit(fixture);

    const problem: ProblemDetails = {
      type: 'https://aris.dev/problems/conflict',
      title: 'Conflict.',
      status: 409,
      detail: 'Username or email already in use.',
    };
    httpMock.expectOne('/identity/users').flush(problem, { status: 409, statusText: 'Conflict' });

    expect(fixture.componentInstance.errorMessage()).toBe('Username or email already in use.');
  });

  it('falls back to a generic client-side message when the error has no problem-details body', () => {
    const fixture = createComponent();
    fillAndSubmit(fixture);

    httpMock.expectOne('/identity/users').error(new ProgressEvent('error'));

    expect(fixture.componentInstance.errorMessage()).toBe('Something went wrong. Please try again.');
  });
});
