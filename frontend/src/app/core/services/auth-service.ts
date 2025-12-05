import { Inject, Injectable, PLATFORM_ID } from '@angular/core';
import { BehaviorSubject, from, Observable, switchMap, tap } from 'rxjs';
import { AuthResponse, LoginCredentials, RegisterData, User, UserRole } from '../../features/auth/authModels';
import { HttpClient } from '@angular/common/http';
import { isPlatformBrowser } from '@angular/common';
import { environment } from '../../../environments/environment';
import { UserPreferencesService } from './user-preferences/user-preferences.service';

import { initializeApp, getApps } from 'firebase/app';
import {
  getAuth,
  signInWithPopup,
  GoogleAuthProvider,
  FacebookAuthProvider,
  UserCredential,
  signOut as firebaseSignOut
} from 'firebase/auth';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly TOKEN_KEY = 'airbnb_token';
  private readonly USER_KEY = 'user_data';

  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  private firebaseApp: any;
  private firebaseAuth: any;

  constructor(
    private http: HttpClient,
    @Inject(PLATFORM_ID) private platformId: any
  ) {
    this.initializeFirebase();
    this.loadUserFromStorage();
  }

  /**
   * Initialize Firebase safely to avoid duplicate-app errors
   */
  private initializeFirebase(): void {
    if (isPlatformBrowser(this.platformId)) {
      const apps = getApps();
      this.firebaseApp = apps.length === 0 ? initializeApp(environment.firebase) : apps[0];
      this.firebaseAuth = getAuth(this.firebaseApp);
    }
  }

  /**
   * Load user from localStorage on app start
   */
  loadUserFromStorage(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    const token = localStorage.getItem(this.TOKEN_KEY);
    const userData = localStorage.getItem(this.USER_KEY);

    if (token && userData) {
      try {
        const user: User = JSON.parse(userData);
        this.currentUserSubject.next(user);
      } catch {
        this.clearStorage();
      }
    } else if (token) {
      const userName = this.getUserNameFromToken();
      if (userName) {
        const user: User = {
          id: '',
          email: '',
          userName,
          fullName: userName,
          role: UserRole.GUEST
        };
        localStorage.setItem(this.USER_KEY, JSON.stringify(user));
        this.currentUserSubject.next(user);
      }
    }
  }

  /**
   * Email/password login
   */
  login(credentials: LoginCredentials): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/login`, credentials).pipe(
      tap(response => {
        const loginData = response.result;
        if (isPlatformBrowser(this.platformId)) {
          localStorage.setItem(this.TOKEN_KEY, loginData.token);

          const userName = this.extractUserNameFromToken(loginData.token);
          if (userName) {
            const user: User = {
              id: '',
              email: '',
              userName,
              fullName: userName,
              role: UserRole.GUEST
            };
            localStorage.setItem(this.USER_KEY, JSON.stringify(user));
            this.currentUserSubject.next(user);

            // Migrate guest preferences
            setTimeout(() => {
              try {
                const userPreferencesService = new UserPreferencesService();
                userPreferencesService.migrateGuestPreferences(userName);
              } catch {}
            }, 0);
          }
        }
      })
    );
  }

  /**
   * Registration
   */
  register(userData: RegisterData): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/register`, userData).pipe(
      tap(response => {
        const loginData = response.result;
        this.setAuthData(loginData.token, loginData.user);
      })
    );
  }

  /**
   * Google login using Firebase
   */
  loginWithGoogle(): Observable<AuthResponse> {
    const provider = new GoogleAuthProvider();
    provider.addScope('email');
    provider.addScope('profile');

    return from(signInWithPopup(this.firebaseAuth, provider)).pipe(
      switchMap((result: UserCredential) => from(result.user.getIdToken())),
      switchMap((firebaseToken: string) =>
        this.http.post<AuthResponse>(`${environment.apiUrl}/auth/google`, { token: firebaseToken })
      ),
      tap(response => {
        const loginData = response.result;
        this.setAuthData(loginData.token, loginData.user);
      })
    );
  }

  /**
   * Facebook login using Firebase
   */
  loginWithFacebook(): Observable<AuthResponse> {
    const provider = new FacebookAuthProvider();
    provider.addScope('email');
    provider.addScope('public_profile');

    return from(signInWithPopup(this.firebaseAuth, provider)).pipe(
      switchMap((result: UserCredential) => from(result.user.getIdToken())),
      switchMap((firebaseToken: string) =>
        this.http.post<AuthResponse>(`${environment.apiUrl}/auth/facebook`, { token: firebaseToken })
      ),
      tap(response => {
        const loginData = response.result;
        this.setAuthData(loginData.token, loginData.user);
      })
    );
  }

  /**
   * Logout
   */
  logout(): Observable<void> {
    return new Observable(observer => {
      if (this.firebaseAuth) {
        firebaseSignOut(this.firebaseAuth).catch(() => {});
      }
      this.http.post(`${environment.apiUrl}/auth/logout`, {}).subscribe({
        next: () => this.completeLogout(observer),
        error: () => this.completeLogout(observer)
      });
    });
  }

  private completeLogout(observer: any): void {
    this.clearStorage();
    this.currentUserSubject.next(null);
    observer.next();
    observer.complete();
  }

  /**
   * Store auth data
   */
  private setAuthData(token: string, user: User): void {
    if (!isPlatformBrowser(this.platformId)) return;

    localStorage.setItem(this.TOKEN_KEY, token);
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    this.currentUserSubject.next(user);

    // Migrate preferences
    if (user.userName || user.email) {
      const userIdentifier = user.userName || user.email;
      setTimeout(() => {
        try {
          const userPreferencesService = new UserPreferencesService();
          userPreferencesService.migrateGuestPreferences(userIdentifier);
        } catch {}
      }, 0);
    }
  }

  private clearStorage(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
  }

  getToken(): string | null {
    return isPlatformBrowser(this.platformId) ? localStorage.getItem(this.TOKEN_KEY) : null;
  }

  private extractUserNameFromToken(token: string): string | null {
    if (!token) return null;
    const parts = token.split('.');
    if (parts.length < 2) return null;

    try {
      const payload = atob(parts[1].replace(/-/g, '+').replace(/_/g, '/'));
      const claims = JSON.parse(decodeURIComponent(escape(payload)));
      return claims['name'] || claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || null;
    } catch {
      return null;
    }
  }

  private getUserNameFromToken(): string | null {
    const token = this.getToken();
    return this.extractUserNameFromToken(token || '');
  }

  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }

  isAuthenticated(): boolean {
    return !!this.getToken() && !!this.currentUserSubject.value;
  }

  hasRole(role: UserRole): boolean {
    const user = this.currentUserSubject.value;
    return user ? user.role === role : false;
  }

  refreshToken(): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/refresh`, {}).pipe(
      tap(response => {
        const loginData = response.result;
        this.setAuthData(loginData.token, loginData.user);
      })
    );
  }
}
