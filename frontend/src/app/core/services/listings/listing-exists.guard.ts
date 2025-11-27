import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ListingService } from './listing.service';

export const listingExistsGuard: CanActivateFn = (route) => {
  const idParam = route.paramMap.get('id');
  const id = idParam ? +idParam : NaN;

  const service = inject(ListingService);
  const router = inject(Router);

  if (!Number.isFinite(id)) {
    router.navigate(['/listings']);
    return false;
  }

  // Wait for backend to confirm listing exists. Return Observable<boolean>.
  return service.getById(id).pipe(
    map((res: any) => {
      const exists = !res?.isError && !!(res.data && (res.data.id ?? res.data.Id));
      if (!exists) router.navigate(['/listings']);
      return exists;
    }),
    catchError((_) => {
      router.navigate(['/listings']);
      return of(false);
    })
  );
};
