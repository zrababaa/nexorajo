import { HttpErrorResponse, type HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import type { ApiErrorResponse } from '../api/api.types';
import { AuthService } from '../auth/auth.service';
import { FlashService } from '../../shared/flash/flash.service';

/**
 * Normalizes auth failures (401/403) into navigation; every other status is left for the caller
 * to interpret against `ApiErrorResponse` (400 validation errors bind to a form, 422 on a send
 * endpoint opens the spam-blocked modal) so this only reacts to what nothing downstream can act on.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const flash = inject(FlashService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && req.url.startsWith('/api/')) {
        if (error.status === 401) {
          auth.logout();
          void router.navigate(['/login']);
        } else if (error.status === 403) {
          flash.error(message(error) ?? 'You do not have permission to do that.');
        } else if (error.status === 0 || error.status >= 500) {
          flash.error(message(error) ?? 'Something went wrong. Please try again.');
        }
      }

      return throwError(() => error);
    }),
  );
};

function message(error: HttpErrorResponse): string | undefined {
  const body = error.error as ApiErrorResponse | undefined;
  return body?.message ?? undefined;
}
