import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { UserDetailComponent } from './user-detail.component';
import { UserSummary } from '../../../core/users/user-management.models';
import { ProblemDetails } from '../../../core/auth/auth.models';

describe('UserDetailComponent', () => {
  let httpMock: HttpTestingController;

  const user: UserSummary = {
    id: 'u1',
    username: 'jdoe',
    email: 'jdoe@aris.local',
    displayName: 'Jane Doe',
    roles: ['Coder'],
    isActive: true,
  };

  function createComponent() {
    const fixture = TestBed.createComponent(UserDetailComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: 'u1' }) } },
        },
      ],
    });

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('fetches and displays the user by id (FR-6.3)', () => {
    const fixture = createComponent();

    httpMock.expectOne((req) => req.method === 'GET' && req.url === '/identity/users/u1').flush(user);

    expect(fixture.componentInstance.user()).toEqual(user);
    expect(fixture.componentInstance.loading()).toBe(false);
  });

  it('shows an error banner when the user cannot be found', () => {
    const fixture = createComponent();

    const problem: ProblemDetails = {
      type: 'https://aris.dev/problems/not-found',
      title: 'Resource not found.',
      status: 404,
      detail: 'User not found.',
    };
    httpMock.expectOne((req) => req.method === 'GET' && req.url === '/identity/users/u1').flush(problem, { status: 404, statusText: 'Not Found' });

    expect(fixture.componentInstance.errorMessage()).toBe('User not found.');
    expect(fixture.componentInstance.user()).toBeNull();
  });

  it('pre-seeds the role editor with the current roles when opened', () => {
    const fixture = createComponent();
    httpMock.expectOne((req) => req.method === 'GET' && req.url === '/identity/users/u1').flush(user);

    fixture.componentInstance.startEditingRoles();

    expect(fixture.componentInstance.editingRoles()).toBe(true);
    expect(Array.from(fixture.componentInstance.selectedRoles)).toEqual(['Coder']);
  });

  it('rejects saving with no roles selected, without calling the backend (FR-6.2)', () => {
    const fixture = createComponent();
    httpMock.expectOne((req) => req.method === 'GET' && req.url === '/identity/users/u1').flush(user);

    fixture.componentInstance.startEditingRoles();
    fixture.componentInstance.selectedRoles.clear();
    fixture.componentInstance.saveRoles();

    expect(fixture.componentInstance.errorMessage()).toContain('At least one role');
    httpMock.expectNone((req) => req.method === 'PUT');
  });

  it('saves the updated roles and shows a success message (FR-6.2)', () => {
    const fixture = createComponent();
    httpMock.expectOne((req) => req.method === 'GET' && req.url === '/identity/users/u1').flush(user);

    fixture.componentInstance.startEditingRoles();
    fixture.componentInstance.toggleRole('RiskAnalyst');
    fixture.componentInstance.saveRoles();

    const updated: UserSummary = { ...user, roles: ['Coder', 'RiskAnalyst'] };
    const putRequest = httpMock.expectOne((req) => req.method === 'PUT' && req.url === '/identity/users/u1/roles');
    expect(putRequest.request.body).toEqual({ roles: ['Coder', 'RiskAnalyst'] });
    putRequest.flush(updated);

    expect(fixture.componentInstance.user()).toEqual(updated);
    expect(fixture.componentInstance.editingRoles()).toBe(false);
    expect(fixture.componentInstance.successMessage()).toContain('updated');
  });

  it('shows the backend-specific reason when a role change is rejected', () => {
    const fixture = createComponent();
    httpMock.expectOne((req) => req.method === 'GET' && req.url === '/identity/users/u1').flush(user);

    fixture.componentInstance.startEditingRoles();
    fixture.componentInstance.saveRoles();

    const problem: ProblemDetails = {
      type: 'https://aris.dev/problems/validation-error',
      title: 'Validation failed.',
      status: 400,
      detail: 'Unknown role(s): NotARole.',
    };
    httpMock
      .expectOne((req) => req.method === 'PUT' && req.url === '/identity/users/u1/roles')
      .flush(problem, { status: 400, statusText: 'Bad Request' });

    expect(fixture.componentInstance.errorMessage()).toBe('Unknown role(s): NotARole.');
  });
});
