import { Component, computed } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent {
  readonly userName = computed(() => this.authService.currentUser()?.displayName ?? '');

  constructor(private readonly authService: AuthService) {}
}
