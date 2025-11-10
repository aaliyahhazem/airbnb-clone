import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { ListingsService } from './listings';

export const listingExistsGuard: CanActivateFn = (route) => {
  const idParam = route.paramMap.get('id');
  const id = idParam ? +idParam : NaN;

  const service = inject(ListingsService);
  const router = inject(Router);

  if (Number.isFinite(id) && service.getById(id)) {
    return true;
  }

  router.navigate(['/listings']);
  return false;
};
