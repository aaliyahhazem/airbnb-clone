import { Injectable, inject, PLATFORM_ID } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap, map } from 'rxjs/operators';
import { Observable, of, BehaviorSubject } from 'rxjs';
import { Router } from '@angular/router';
import { isPlatformBrowser } from '@angular/common';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly cookieName = 'airbnb_token';
  private readonly localStorageKey = 'airbnb_token';
  private readonly apiBase = 'http://localhost:5235/api/auth';
  private authSubject: BehaviorSubject<boolean>;
  public isAuthenticated$: Observable<boolean>;
  private isBrowser: boolean;

  constructor(private http: HttpClient, private router: Router) {
    this.isBrowser = typeof window !== 'undefined' && typeof localStorage !== 'undefined';

    // Initialize auth state from localStorage (more reliable than cookies in dev)
    const hasToken = this.isBrowser ? !!this.getTokenSilent() : false;
    this.authSubject = new BehaviorSubject<boolean>(hasToken);
    this.isAuthenticated$ = this.authSubject.asObservable();

    if (this.isBrowser) {
      if (hasToken) {
        console.log('AuthService initialized: User is authenticated (token found)');
      } else {
        console.log('AuthService initialized: User is NOT authenticated (no token)');
        console.log('localStorage token:', localStorage.getItem(this.localStorageKey));
      }
    } else {
      console.log('AuthService initialized: Running on server (SSR), skipping token check');
    }
  }

  // Silent token retrieval without logging (for initialization)
  private getTokenSilent(): string | null {
    if (!this.isBrowser) return null;

    const token = localStorage.getItem(this.localStorageKey);
    if (token) return token;

    const nameEQ = this.cookieName + '=';
    const ca = document.cookie.split(';');
    for (let i = 0; i < ca.length; i++) {
      let c = ca[i];
      while (c.charAt(0) === ' ') c = c.substring(1, c.length);
      if (c.indexOf(nameEQ) === 0) {
        return decodeURIComponent(c.substring(nameEQ.length, c.length));
      }
    }
    return null;
  }

  // store token in both localStorage and cookie for maximum persistence
  setToken(token: string, days = 1) {
    if (!this.isBrowser) {
      this.authSubject.next(true);
      return;
    }

    console.log('Setting token, length:', token?.length);

    // Store in localStorage (most reliable for dev)
    localStorage.setItem(this.localStorageKey, token);
    console.log('✓ Token saved to localStorage');

    // Also store in cookie
    // Clear any existing cookie first
    document.cookie = `${this.cookieName}=;Max-Age=0;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/`;

    const maxAge = days * 24 * 60 * 60; // seconds
    const expires = new Date(Date.now() + maxAge * 1000).toUTCString();
    const sameSite = 'SameSite=Lax';
    const secure = (location.protocol === 'https:') ? ';Secure' : '';
    document.cookie = `${this.cookieName}=${encodeURIComponent(token)};Max-Age=${maxAge};expires=${expires};path=/;${sameSite}${secure}`;
    console.log('✓ Token saved to cookie');

    // notify subscribers
    this.authSubject.next(true);
  }

  getToken(): string | null {
    if (!this.isBrowser) return null;

    // Try localStorage first (more reliable)
    const token = localStorage.getItem(this.localStorageKey);
    if (token) {
      console.log('Token found in localStorage:', 'YES (length: ' + token.length + ')');
      return token;
    }

    // Fallback to cookie
    const nameEQ = this.cookieName + '=';
    const ca = document.cookie.split(';');
    for (let i = 0; i < ca.length; i++) {
      let c = ca[i];
      while (c.charAt(0) === ' ') c = c.substring(1, c.length);
      if (c.indexOf(nameEQ) === 0) {
        const cookieToken = decodeURIComponent(c.substring(nameEQ.length, c.length));
        console.log('Token found in cookie:', cookieToken ? 'YES (length: ' + cookieToken.length + ')' : 'NO');
        return cookieToken;
      }
    }
    console.log('No token found in localStorage or cookies');
    return null;
  }

  removeToken() {
    if (!this.isBrowser) {
      this.authSubject.next(false);
      return;
    }

    // Remove from localStorage
    localStorage.removeItem(this.localStorageKey);
    console.log('✓ Token removed from localStorage');

    // Remove from cookie
    document.cookie = `${this.cookieName}=;Max-Age=0;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/`;
    console.log('✓ Token removed from cookie');

    this.authSubject.next(false);
  }

  isAuthenticated(): boolean {
    const hasToken = !!this.getToken();
    console.log('isAuthenticated check:', hasToken);
    return hasToken;
  }

  // simple JWT payload decoder (no validation) to access claims
  getPayload(): any | null {
    const token = this.getToken();
    if (!token) return null;
    const parts = token.split('.');
    if (parts.length < 2) return null;
    try {
      const payload = atob(parts[1].replace(/-/g, '+').replace(/_/g, '/'));
      return JSON.parse(decodeURIComponent(escape(payload)));
    } catch {
      return null;
    }
  }

  isAdmin(): boolean {
    const p = this.getPayload();
    if (!p) return false;
    const role = p['role'] || p['roles'] || p['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
    if (!role) return false;
    if (Array.isArray(role)) return role.includes('Admin');
    return role === 'Admin';
  }

  // Login endpoint call: expects backend returns token (adjust if backend returns different shape)
  login(model: { email: string; password: string }): Observable<any> {
    console.log('AuthService: login called with', model);
    return this.http.post<any>(`${this.apiBase}/login`, model).pipe(
      tap(res => {
        // backend returns { result: '<token>', ... }
        const token = res?.result || res?.token;
        if (token) {
          // remove any existing token first (handle multi-login in same client)
          this.removeToken();
          this.setToken(token);
          console.log('Login successful, token set');
          // if admin, navigate to admin dashboard
          if (this.isAdmin()) this.router.navigate(['/admin']);
        }
      })
    );
  }

  register(model: any) {
    return this.http.post<any>(`${this.apiBase}/register`, model).pipe(
      tap(res => {
        const token = res?.result || res?.token;
        if (token) {
          // remove any existing token first
          this.removeToken();
          this.setToken(token);
          console.log('Registration successful, token set');
        }
      })
    );
  }

  logout() {
    // clear token and notify
    this.removeToken();
    // navigate to login page
    this.router.navigate(['/auth/login']);
  }
}
