import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ProblemDetails } from '../../../core/auth/auth.models';
import { SEEDED_ROLES, SeededRole, UserSummary } from '../../../core/users/user-management.models';
import { UserManagementService } from '../../../core/users/user-management.service';
import { IconComponent } from '../../../shared/icons/icon.component';

@Component({
  selector: 'app-user-detail',
  standalone: true,
  imports: [RouterLink, IconComponent],
  templateUrl: './user-detail.component.html',
  styleUrl: './user-detail.component.scss',
})
export class UserDetailComponent implements OnInit {
  readonly availableRoles = SEEDED_ROLES;

  readonly user = signal<UserSummary | null>(null);
  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly successMessage = signal('');
  readonly editingRoles = signal(false);
  readonly savingRoles = signal(false);

  selectedRoles = new Set<SeededRole>();

  private userId = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly userManagementService: UserManagementService,
  ) {}

  ngOnInit(): void {
    this.userId = this.route.snapshot.paramMap.get('id') ?? '';
    this.fetchUser();
  }

  startEditingRoles(): void {
    const user = this.user();
    if (!user) {
      return;
    }

    this.selectedRoles = new Set(user.roles as SeededRole[]);
    this.successMessage.set('');
    this.errorMessage.set('');
    this.editingRoles.set(true);
  }

  cancelEditingRoles(): void {
    this.editingRoles.set(false);
  }

  toggleRole(role: SeededRole): void {
    if (this.selectedRoles.has(role)) {
      this.selectedRoles.delete(role);
    } else {
      this.selectedRoles.add(role);
    }
  }

  saveRoles(): void {
    const roles = Array.from(this.selectedRoles);

    if (roles.length === 0) {
      this.errorMessage.set('At least one role must be selected.');
      return;
    }

    this.savingRoles.set(true);
    this.errorMessage.set('');

    this.userManagementService.changeUserRoles(this.userId, { roles }).subscribe({
      next: (user) => {
        this.savingRoles.set(false);
        this.editingRoles.set(false);
        this.user.set(user);
        this.successMessage.set('Roles updated.');
      },
      error: (error: unknown) => {
        this.savingRoles.set(false);
        const detail = error instanceof HttpErrorResponse ? (error.error as ProblemDetails | null)?.detail : undefined;
        this.errorMessage.set(detail ?? 'Something went wrong. Please try again.');
      },
    });
  }

  private fetchUser(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.userManagementService.getUser(this.userId).subscribe({
      next: (user) => {
        this.loading.set(false);
        this.user.set(user);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        const detail = error instanceof HttpErrorResponse ? (error.error as ProblemDetails | null)?.detail : undefined;
        this.errorMessage.set(detail ?? 'Something went wrong. Please try again.');
      },
    });
  }
}
