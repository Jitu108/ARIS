import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';

// FR-6.6: user-management screens are Administrator-only client-side too — this is a UX
// convenience only, since the backend independently enforces the same rule on every request.
export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }

  return authService.currentUser()?.roles.includes('Administrator') ? true : router.createUrlTree(['/']);
};
