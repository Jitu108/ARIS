import { HttpErrorResponse } from '@angular/common/http';
import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ProblemDetails } from '../../../core/auth/auth.models';
import { SEEDED_ROLES, SeededRole } from '../../../core/users/user-management.models';
import { UserManagementService } from '../../../core/users/user-management.service';
import { IconComponent } from '../../../shared/icons/icon.component';

@Component({
  selector: 'app-create-user',
  standalone: true,
  imports: [FormsModule, IconComponent],
  templateUrl: './create-user.component.html',
  styleUrl: './create-user.component.scss',
})
export class CreateUserComponent {
  readonly availableRoles = SEEDED_ROLES;

  username = '';
  email = '';
  displayName = '';
  password = '';
  selectedRoles = new Set<SeededRole>();

  readonly submitting = signal(false);
  readonly errorMessage = signal('');
  readonly successMessage = signal('');

  constructor(private readonly userManagementService: UserManagementService) {}

  toggleRole(role: SeededRole): void {
    if (this.selectedRoles.has(role)) {
      this.selectedRoles.delete(role);
    } else {
      this.selectedRoles.add(role);
    }
  }

  handleSubmit(): void {
    const username = this.username.trim();
    const email = this.email.trim();
    const displayName = this.displayName.trim();
    const password = this.password;
    const roles = Array.from(this.selectedRoles);

    this.successMessage.set('');

    if (!username || !email || !displayName || !password || roles.length === 0) {
      this.errorMessage.set('Username, email, display name, password, and at least one role are all required.');
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set('');

    this.userManagementService.createUser({ username, email, password, displayName, roles }).subscribe({
      next: (response) => {
        this.submitting.set(false);
        this.successMessage.set(`${response.displayName} (${response.username}) was created and can log in immediately.`);
        this.resetForm();
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        const detail = error instanceof HttpErrorResponse ? (error.error as ProblemDetails | null)?.detail : undefined;
        this.errorMessage.set(detail ?? 'Something went wrong. Please try again.');
      },
    });
  }

  private resetForm(): void {
    this.username = '';
    this.email = '';
    this.displayName = '';
    this.password = '';
    this.selectedRoles = new Set<SeededRole>();
  }
}
