import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate {
  constructor(private auth: AuthService, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
    // Check if token exists in cookie (this is the source of truth)
    const hasToken = !!this.auth.getToken();

    console.log('AuthGuard check:', {
      hasToken: hasToken,
      targetUrl: state.url
    });

    if (hasToken) {
      console.log('✓ User has valid token, allowing access');
      return true;
    }

    // No token, redirect to login
    console.log('✗ No token found, redirecting to login');
    this.router.navigate(['/auth/login'], {
      queryParams: { returnUrl: state.url }
    });
    return false;
  }
}
