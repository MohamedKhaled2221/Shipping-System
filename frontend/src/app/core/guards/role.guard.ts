import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { UserRole } from '../models/enums';

/** Usage: { path: '...', canActivate: [roleGuard], data: { roles: [UserRole.Admin] } } */
export const roleGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const allowedRoles = (route.data['roles'] as UserRole[] | undefined) ?? [];
  const currentRole = authService.role();

  if (currentRole !== null && allowedRoles.includes(currentRole)) return true;

  return router.createUrlTree(['/']);
};
