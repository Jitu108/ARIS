import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ProblemDetails } from '../../../core/auth/auth.models';
import { UserSummary } from '../../../core/users/user-management.models';
import { UserManagementService } from '../../../core/users/user-management.service';
import { IconComponent } from '../../../shared/icons/icon.component';

const PAGE_SIZE = 20;
const SEARCH_DEBOUNCE_MS = 300;

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [FormsModule, RouterLink, IconComponent],
  templateUrl: './user-list.component.html',
  styleUrl: './user-list.component.scss',
})
export class UserListComponent implements OnInit {
  readonly pageSize = PAGE_SIZE;

  query = '';

  readonly users = signal<UserSummary[]>([]);
  readonly page = signal(1);
  readonly totalCount = signal(0);
  readonly loading = signal(true);
  readonly errorMessage = signal('');

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));
  readonly rangeStart = computed(() => (this.totalCount() === 0 ? 0 : (this.page() - 1) * this.pageSize + 1));
  readonly rangeEnd = computed(() => Math.min(this.page() * this.pageSize, this.totalCount()));

  private searchDebounceHandle?: ReturnType<typeof setTimeout>;

  constructor(private readonly userManagementService: UserManagementService) {}

  ngOnInit(): void {
    this.fetchUsers();
  }

  handleSearchInput(): void {
    clearTimeout(this.searchDebounceHandle);
    this.searchDebounceHandle = setTimeout(() => {
      this.page.set(1);
      this.fetchUsers();
    }, SEARCH_DEBOUNCE_MS);
  }

  goToPreviousPage(): void {
    if (this.page() > 1) {
      this.page.update((page) => page - 1);
      this.fetchUsers();
    }
  }

  goToNextPage(): void {
    if (this.page() < this.totalPages()) {
      this.page.update((page) => page + 1);
      this.fetchUsers();
    }
  }

  private fetchUsers(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.userManagementService.listUsers(this.query.trim(), this.page(), this.pageSize).subscribe({
      next: (response) => {
        this.loading.set(false);
        this.users.set(response.items);
        this.totalCount.set(response.totalCount);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        const detail = error instanceof HttpErrorResponse ? (error.error as ProblemDetails | null)?.detail : undefined;
        this.errorMessage.set(detail ?? 'Something went wrong. Please try again.');
      },
    });
  }
}
