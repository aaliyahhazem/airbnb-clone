import { Injectable, inject, PLATFORM_ID } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap, map } from 'rxjs/operators';
import { Observable, of, BehaviorSubject } from 'rxjs';
import { Router } from '@angular/router';
import { isPlatformBrowser } from '@angular/common';
import { AuthService as NewAuthService } from './auth-service';

import { firebaseAuth } from '../../firebase.config';
import { GoogleAuthProvider, signInWithPopup } from 'firebase/auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly cookieName = 'airbnb_token';
  private readonly localStorageKey = 'airbnb_token';
  private readonly USER_KEY = 'user_data';
  private readonly apiBase = 'http://localhost:5235/api/auth';
  private authSubject: BehaviorSubject<boolean>;
  public isAuthenticated$: Observable<boolean>;
  private isBrowser: boolean;
  private newAuthService = inject(NewAuthService);

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

  // Google login using Firebase
  googleLogin(): Observable<any> {
    const provider = new GoogleAuthProvider();

    return new Observable(observer => {
      signInWithPopup(firebaseAuth, provider)
        .then(async (result) => {
          const idToken = await result.user.getIdToken();
          console.log('Firebase Google ID token:', idToken);

          // Send token to your backend to get your own JWT
          this.http.post<any>(`${this.apiBase}/google-login`, { idToken })
            .subscribe({
              next: (res) => {
                const token = res?.result?.token;
                if (token) this.setToken(token);
                observer.next(res);
                observer.complete();
              },
              error: (err) => observer.error(err)
            });
        })
        .catch(err => observer.error(err));
    });
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
        return cookieToken;
      }
    }
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
    return !!this.getToken();
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

  // Get user's full name from JWT token
  getUserFullName(): string | null {
    const p = this.getPayload();
    if (!p) {
      return null;
    }
    const fullName = p['name'] || p['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || null;
    return fullName;
  }

  // Login endpoint call: expects backend returns LoginResponseVM { token, isFirstLogin, user }
  login(model: { email: string; password: string }): Observable<any> {
    console.log('AuthService: login called with', model);
    return this.http.post<any>(`${this.apiBase}/login`, model).pipe(
      tap(res => {
        // backend returns { result: { token, isFirstLogin, user }, ... }
        const loginResponse = res?.result;
        const token = loginResponse?.token;
        const isFirstLogin = loginResponse?.isFirstLogin;

        if (token) {
          // remove any existing token first (handle multi-login in same client)
          this.removeToken();
          this.setToken(token);
          console.log('Login successful, token set');

          // Store isFirstLogin flag for onboarding check
          if (this.isBrowser && isFirstLogin !== undefined) {
            localStorage.setItem('isFirstLogin', isFirstLogin.toString());
            console.log('isFirstLogin flag stored:', isFirstLogin);
          }

          // Extract user data from token and save to USER_KEY for new AuthService
          if (this.isBrowser) {
            const userName = this.getUserFullName();
            if (userName) {
              const user = {
                id: '',
                email: model.email,
                userName: userName,
                fullName: userName,
                role: 'Guest'
              };
              localStorage.setItem(this.USER_KEY, JSON.stringify(user));
              console.log('✅ User data saved for new AuthService:', user);

              // Trigger the new auth service to reload from storage
              this.newAuthService['loadUserFromStorage']();
            }
          }

          // Navigation will be handled by Login component based on isFirstLogin
        }
      })
    );
  }

  register(model: any) {
    return this.http.post<any>(`${this.apiBase}/register`, model).pipe(
      tap(res => {
        // Registration also returns LoginResponseVM with token and isFirstLogin
        const loginResponse = res?.result;
        const token = loginResponse?.token;
        const isFirstLogin = loginResponse?.isFirstLogin;

        if (token) {
          // remove any existing token first
          this.removeToken();
          this.setToken(token);
          console.log('Registration successful, token set');

          // Store isFirstLogin flag for onboarding check
          if (this.isBrowser && isFirstLogin !== undefined) {
            localStorage.setItem('isFirstLogin', isFirstLogin.toString());
            console.log('isFirstLogin flag stored:', isFirstLogin);
          }

          // Extract user data from token and save to USER_KEY for new AuthService
          if (this.isBrowser) {
            const userName = this.getUserFullName();
            if (userName) {
              const user = {
                id: '',
                email: model.email || '',
                userName: userName,
                fullName: userName,
                role: 'Guest'
              };
              localStorage.setItem(this.USER_KEY, JSON.stringify(user));
              console.log('✅ User data saved for new AuthService:', user);

              // Trigger the new auth service to reload from storage
              this.newAuthService['loadUserFromStorage']();
            }
          }
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

  /**
   * Register face for an existing user (after login)
   */
  registerFace(userId: string, imageFile: File): Observable<any> {
    const formData = new FormData();
    formData.append('imageFile', imageFile);
    formData.append('UserId', userId);
    formData.append('CreatedBy', userId);

    return this.http.post<any>('http://localhost:5235/api/faceid/register', formData);
  }

  /**
   * Login using face recognition
   */
  loginWithFace(imageFile: File): Observable<any> {
    const formData = new FormData();
    formData.append('image', imageFile);

    return this.http.post<any>('http://localhost:5235/api/faceid/login', formData).pipe(
      tap(res => {
        console.log('Face login full response:', res);
        
        // Backend returns { result: "token_string", success: boolean, ... }
        // The token is directly in result as a string, not nested in an object
        let token = res?.result;
        
        // If result is an object with token property, use that
        if (typeof token === 'object' && token?.token) {
          token = token.token;
        }

        console.log('Extracted token:', token ? 'Found (length: ' + token.length + ')' : 'Not found');

        if (token && typeof token === 'string') {
          this.removeToken();
          this.setToken(token);
          console.log('Face login successful, token set');

          // If backend provides explicit isFirstLogin flag use it; otherwise do not change
          // Allow backend to control whether onboarding should be shown
          const providedIsFirstLogin = res?.isFirstLogin ?? (res?.result && typeof res.result === 'object' ? res.result.isFirstLogin : undefined);
          if (this.isBrowser && typeof providedIsFirstLogin !== 'undefined') {
            try {
              localStorage.setItem('isFirstLogin', String(providedIsFirstLogin));
              console.log('isFirstLogin flag stored from backend:', providedIsFirstLogin);
            } catch (err) {
              console.warn('Failed to store isFirstLogin flag:', err);
            }
          }
        } else {
          console.error('Face login: No valid token in response', res);
        }
      })
    );
  }

  /**
   * Verify if user has face registered
   */
  verifyFaceExists(userId: string): Observable<any> {
    return this.http.get<any>(`http://localhost:5235/api/faceid/verify/${userId}`);
  }
}
