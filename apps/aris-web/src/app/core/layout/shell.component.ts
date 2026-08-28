import { Component, computed, signal } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { IconComponent } from '../../shared/icons/icon.component';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, IconComponent],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent {
  readonly menuOpen = signal(false);

  readonly userName = computed(() => this.authService.currentUser()?.displayName ?? '');
  readonly roleLabel = computed(() => this.authService.currentUser()?.roles?.[0] ?? '');
  readonly userInitials = computed(() => this.initialsFor(this.userName()));

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
  ) {}

  toggleMenu(): void {
    this.menuOpen.update((open) => !open);
  }

  logout(): void {
    this.authService.logout();
    this.menuOpen.set(false);
    this.router.navigateByUrl('/login');
  }

  private initialsFor(name: string): string {
    const parts = name.split(' ').filter((part) => !part.endsWith('.'));
    const first = parts[0]?.[0] ?? '';
    const last = parts.length > 1 ? parts[parts.length - 1][0] : '';
    return (first + last).toUpperCase();
  }
}
