import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { UserListComponent } from './user-list.component';
import { ListUsersResponse } from '../../../core/users/user-management.models';

describe('UserListComponent', () => {
  let httpMock: HttpTestingController;

  function createComponent() {
    const fixture = TestBed.createComponent(UserListComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('shows loading skeleton rows, then results, on init (FR-6.7)', () => {
    const fixture = createComponent();

    expect(fixture.componentInstance.loading()).toBe(true);

    const response: ListUsersResponse = {
      items: [
        { id: 'u1', username: 'jdoe', email: 'jdoe@aris.local', displayName: 'Jane Doe', roles: ['Coder'], isActive: true },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    };
    httpMock.expectOne((req) => req.url === '/identity/users').flush(response);

    expect(fixture.componentInstance.loading()).toBe(false);
    expect(fixture.componentInstance.users()).toEqual(response.items);
    expect(fixture.componentInstance.totalCount()).toBe(1);
  });

  it('renders the empty state when there are no matching accounts', () => {
    const fixture = createComponent();

    const response: ListUsersResponse = { items: [], page: 1, pageSize: 20, totalCount: 0 };
    httpMock.expectOne((req) => req.url === '/identity/users').flush(response);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.empty-state')?.textContent).toContain('No results');
  });

  it('shows an error banner when the request fails', () => {
    const fixture = createComponent();

    httpMock.expectOne((req) => req.url === '/identity/users').error(new ProgressEvent('error'));
    fixture.detectChanges();

    expect(fixture.componentInstance.errorMessage()).toBe('Something went wrong. Please try again.');
  });

  it('requests the next page when Next is clicked', () => {
    const fixture = createComponent();
    const fullPage: ListUsersResponse = {
      items: Array.from({ length: 20 }, (_, i) => ({
        id: `u${i}`,
        username: `user${i}`,
        email: `user${i}@aris.local`,
        displayName: `User ${i}`,
        roles: ['Coder'],
        isActive: true,
      })),
      page: 1,
      pageSize: 20,
      totalCount: 25,
    };
    httpMock.expectOne((req) => req.url === '/identity/users').flush(fullPage);

    fixture.componentInstance.goToNextPage();

    const nextRequest = httpMock.expectOne((req) => req.url === '/identity/users');
    expect(nextRequest.request.params.get('page')).toBe('2');
    nextRequest.flush({ ...fullPage, page: 2, items: [] });
  });

  it('resets to page 1 and re-fetches when the search query changes', () => {
    vi.useFakeTimers();
    try {
      const fixture = createComponent();
      httpMock.expectOne((req) => req.url === '/identity/users').flush({ items: [], page: 1, pageSize: 20, totalCount: 0 });

      fixture.componentInstance.query = 'jdoe';
      fixture.componentInstance.handleSearchInput();
      vi.advanceTimersByTime(300);

      const request = httpMock.expectOne((req) => req.url === '/identity/users');
      expect(request.request.params.get('query')).toBe('jdoe');
      expect(request.request.params.get('page')).toBe('1');
      request.flush({ items: [], page: 1, pageSize: 20, totalCount: 0 });
    } finally {
      vi.useRealTimers();
    }
  });

  const activeUser = { id: 'u1', username: 'jdoe', email: 'jdoe@aris.local', displayName: 'Jane Doe', roles: ['Coder'], isActive: true };

  it('shows a Deactivate button only for active accounts (FR-6.8)', () => {
    const fixture = createComponent();
    const response: ListUsersResponse = {
      items: [
        activeUser,
        { id: 'u2', username: 'bsmith', email: 'bsmith@aris.local', displayName: 'Bob Smith', roles: ['Coder'], isActive: false },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 2,
    };
    httpMock.expectOne((req) => req.url === '/identity/users').flush(response);
    fixture.detectChanges();

    const buttons = fixture.nativeElement.querySelectorAll('.deactivate-button');
    expect(buttons.length).toBe(1);
  });

  it('opens a confirm dialog and deactivates the user on confirm, updating the row in place', () => {
    const fixture = createComponent();
    httpMock
      .expectOne((req) => req.url === '/identity/users')
      .flush({ items: [activeUser], page: 1, pageSize: 20, totalCount: 1 });
    fixture.detectChanges();

    fixture.nativeElement.querySelector('.deactivate-button').click();
    fixture.detectChanges();

    expect(fixture.componentInstance.userPendingDeactivation()).toEqual(activeUser);

    fixture.componentInstance.confirmDeactivate();
    httpMock.expectOne((req) => req.method === 'POST' && req.url === '/identity/users/u1/deactivate').flush(null);

    expect(fixture.componentInstance.userPendingDeactivation()).toBeNull();
    expect(fixture.componentInstance.users()[0].isActive).toBe(false);
  });

  it('shows the backend-specific reason when deactivation fails', () => {
    const fixture = createComponent();
    httpMock
      .expectOne((req) => req.url === '/identity/users')
      .flush({ items: [activeUser], page: 1, pageSize: 20, totalCount: 1 });
    fixture.detectChanges();

    fixture.componentInstance.requestDeactivate(activeUser);
    fixture.componentInstance.confirmDeactivate();

    const problem = {
      type: 'https://aris.dev/problems/conflict',
      title: 'Conflict.',
      status: 409,
      detail: 'User is already inactive.',
    };
    httpMock
      .expectOne((req) => req.method === 'POST' && req.url === '/identity/users/u1/deactivate')
      .flush(problem, { status: 409, statusText: 'Conflict' });

    expect(fixture.componentInstance.deactivateError()).toBe('User is already inactive.');
  });

  it('closes the confirm dialog without calling the backend on cancel', () => {
    const fixture = createComponent();
    httpMock
      .expectOne((req) => req.url === '/identity/users')
      .flush({ items: [activeUser], page: 1, pageSize: 20, totalCount: 1 });
    fixture.detectChanges();

    fixture.componentInstance.requestDeactivate(activeUser);
    fixture.componentInstance.cancelDeactivate();

    expect(fixture.componentInstance.userPendingDeactivation()).toBeNull();
    httpMock.expectNone((req) => req.method === 'POST');
  });
});
