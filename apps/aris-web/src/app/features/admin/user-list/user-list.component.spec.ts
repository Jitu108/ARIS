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
});
