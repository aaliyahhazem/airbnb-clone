import { CanActivate,Router, UrlTree } from '@angular/router';
import { AuthService } from '../services/auth-service';
import { Injectable } from '@angular/core';


@Injectable({
  providedIn: 'root'
})
export class authGuard implements CanActivate  {

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}
 canActivate(): boolean | UrlTree {
    if (this.authService.isAuthenticated()) {
      return true;
    } else {
      // Redirect user to login if not authenticated
      return this.router.createUrlTree(['/auth/login']);
    }
  }
};
