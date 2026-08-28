import { HttpErrorResponse } from '@angular/common/http';
import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { ProblemDetails } from '../../core/auth/auth.models';
import { IconComponent } from '../../shared/icons/icon.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, IconComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  username = '';
  password = '';

  readonly showPassword = signal(false);
  readonly submitting = signal(false);
  readonly errorMessage = signal('');

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
  ) {}

  togglePasswordVisibility(): void {
    this.showPassword.update((visible) => !visible);
  }

  handleSubmit(): void {
    const username = this.username.trim();
    const password = this.password;

    if (!username || !password) {
      this.errorMessage.set('Enter your username and password.');
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set('');

    this.authService.login({ username, password }).subscribe({
      next: (response) => {
        this.submitting.set(false);
        // /change-password isn't built yet (FR-6.16 is a separate ticket) — unreachable with the
        // current seed data, since the seeded admin has mustChangePassword: false.
        this.router.navigateByUrl(response.mustChangePassword ? '/change-password' : '/');
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        // Always the exact backend message (FR-1.2's generic wording), never a separate client string.
        const detail = error instanceof HttpErrorResponse
          ? (error.error as ProblemDetails | null)?.detail
          : undefined;
        this.errorMessage.set(detail ?? 'Something went wrong. Please try again.');
      },
    });
  }
}
