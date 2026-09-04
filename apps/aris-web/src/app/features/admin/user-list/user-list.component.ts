import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ProblemDetails } from '../../../core/auth/auth.models';
import { UserSummary } from '../../../core/users/user-management.models';
import { UserManagementService } from '../../../core/users/user-management.service';
import { IconComponent } from '../../../shared/icons/icon.component';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog.component';

const PAGE_SIZE = 20;
const SEARCH_DEBOUNCE_MS = 300;

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [FormsModule, RouterLink, IconComponent, ConfirmDialogComponent],
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

  readonly userPendingDeactivation = signal<UserSummary | null>(null);
  readonly deactivating = signal(false);
  readonly deactivateError = signal('');

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

  requestDeactivate(user: UserSummary): void {
    this.deactivateError.set('');
    this.userPendingDeactivation.set(user);
  }

  cancelDeactivate(): void {
    this.userPendingDeactivation.set(null);
  }

  confirmDeactivate(): void {
    const user = this.userPendingDeactivation();
    if (!user) {
      return;
    }

    this.deactivating.set(true);
    this.deactivateError.set('');

    this.userManagementService.deactivateUser(user.id).subscribe({
      next: () => {
        this.deactivating.set(false);
        this.userPendingDeactivation.set(null);
        this.users.update((users) =>
          users.map((existing) => (existing.id === user.id ? { ...existing, isActive: false } : existing)),
        );
      },
      error: (error: unknown) => {
        this.deactivating.set(false);
        const detail = error instanceof HttpErrorResponse ? (error.error as ProblemDetails | null)?.detail : undefined;
        this.deactivateError.set(detail ?? 'Something went wrong. Please try again.');
      },
    });
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
