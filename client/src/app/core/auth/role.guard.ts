import { inject } from '@angular/core';
import { Router, type CanActivateFn } from '@angular/router';
import type { UserRole } from '../api/api.types';
import { AuthService } from './auth.service';

export const roleGuard = (role: UserRole): CanActivateFn => {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (auth.user()?.role === role) {
      return true;
    }

    return router.createUrlTree(['/forbidden']);
  };
};
