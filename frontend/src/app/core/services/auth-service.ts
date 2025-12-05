import { Inject, Injectable, PLATFORM_ID, inject } from '@angular/core';
import { BehaviorSubject, from, Observable, switchMap, tap } from 'rxjs';
import { AuthResponse, LoginCredentials, RegisterData, User, UserRole } from '../../features/auth/authModels';
import { HttpClient } from '@angular/common/http';
import { isPlatformBrowser } from '@angular/common';
import { environment } from '../../../environments/environment';
import { UserPreferencesService } from './user-preferences/user-preferences.service';
import { initializeApp } from 'firebase/app';
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
  private readonly TOKEN_KEY = 'airbnb_token'; // Match old auth service key
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

  /*
   * Initializes Firebase app and authentication
   */
  private initializeFirebase(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.firebaseApp = initializeApp(environment.firebaseConfig);
      this.firebaseAuth = getAuth(this.firebaseApp);
    }
  }
    /**
   * Loads user data from localStorage on app initialization
   * Made public so it can be triggered after login from old auth service
   */
  loadUserFromStorage(): void {
    if (isPlatformBrowser(this.platformId)) {
      const token = localStorage.getItem(this.TOKEN_KEY);
      const userData = localStorage.getItem(this.USER_KEY);

      console.log('🔍 loadUserFromStorage: token exists?', !!token, 'userData exists?', !!userData);

      if (token && userData) {
        try {
          const user: User = JSON.parse(userData);
          console.log('📤 Emitting user from storage:', user);
          this.currentUserSubject.next(user);
        } catch (error) {
          console.error('AuthService: Failed to parse user data', error);
          this.clearStorage();
        }
      } else if (token) {
        // Token exists but no user data - extract userName from token
        const userName = this.getUserNameFromToken();
        console.log('🎫 Extracted userName from token:', userName);
        if (userName) {
          const user: User = {
            id: '',
            email: '',
            userName: userName,
            fullName: userName,
            role: UserRole.GUEST
          };
          console.log('📤 Emitting user from token:', user);
          this.currentUserSubject.next(user);
          localStorage.setItem(this.USER_KEY, JSON.stringify(user));
        }
      } else {
        console.log('⚠️ No token or user data found, user remains null');
      }
    }
  }
    /**
   * Email/Password login using Microsoft Identity backend
   */
  login(credentials: LoginCredentials): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/login`, credentials)
      .pipe(
        tap(response => {
          // Backend wraps response in { result: LoginResponseVM, ... }
          const loginData = response.result;

          // Just save the token, we'll get user data from JWT when needed
          if (isPlatformBrowser(this.platformId)) {
            localStorage.setItem(this.TOKEN_KEY, loginData.token);

            // Get userName from JWT token using the same method as navbar
            const userName = this.extractUserNameFromToken(loginData.token);

            if (userName) {
              const user: User = {
                id: '',
                email: '',
                userName: userName,
                fullName: userName,
                role: UserRole.GUEST
              };

              localStorage.setItem(this.USER_KEY, JSON.stringify(user));
              this.currentUserSubject.next(user);

              // Migrate guest preferences
              setTimeout(() => {
                try {
                  const userPreferencesService = inject(UserPreferencesService);
                  userPreferencesService.migrateGuestPreferences(userName);
                } catch (e) {
                  console.warn('Could not migrate preferences:', e);
                }
              }, 0);
            }
          }
        })
      );
  }
  /*
   * Email/Password registration using Microsoft Identity backend
   */
  register(userData: RegisterData): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/register`, userData)
      .pipe(
        tap(response => {
          console.log('Register response from backend:', response);
          // Backend wraps response in { result: LoginResponseVM, ... }
          const loginData = response.result;
          this.setAuthData(loginData.token, loginData.user);
        })
      );
  }
  /**
   * Social Login with Google (Firebase)
   */
  loginWithGoogle(): Observable<AuthResponse> {
    const provider = new GoogleAuthProvider();
    provider.addScope('email');
    provider.addScope('profile');

    return from(signInWithPopup(this.firebaseAuth, provider)).pipe(
      switchMap((result: UserCredential) => {
        const idToken = result.user.getIdToken();
        return from(idToken);
      }),
      switchMap((firebaseToken: string) => {
        return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/google`, {
          token: firebaseToken
        });
      }),
      tap(response => {
        console.log('Google login response from backend:', response);
        // Backend wraps response in { result: LoginResponseVM, ... }
        const loginData = response.result;
        this.setAuthData(loginData.token, loginData.user);
      })
    );
  }
  /**
   * Facebook social login using Firebase
   */
  loginWithFacebook(): Observable<AuthResponse> {
    const provider = new FacebookAuthProvider();
    provider.addScope('email');
    provider.addScope('public_profile');

    return from(signInWithPopup(this.firebaseAuth, provider)).pipe(
      switchMap((result: UserCredential) => {
        const idToken = result.user.getIdToken();
        return from(idToken);
      }),
      switchMap((firebaseToken: string) => {
        return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/facebook`, {
          token: firebaseToken
        });
      }),
      tap(response => {
        console.log('Facebook login response from backend:', response);
        // Backend wraps response in { result: LoginResponseVM, ... }
        const loginData = response.result;
        this.setAuthData(loginData.token, loginData.user);
      })
    );
  }
  /**
   * Logout user from both backend and Firebase
   */
  logout(): Observable<void> {
    return new Observable(observer => {
      // Sign out from Firebase
      if (this.firebaseAuth) {
        firebaseSignOut(this.firebaseAuth).catch(() => {
        });
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
 /*
   * Stores authentication data
   */
  private setAuthData(token: string, user: User): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem(this.TOKEN_KEY, token);
      localStorage.setItem(this.USER_KEY, JSON.stringify(user));

      this.currentUserSubject.next(user);

      // Migrate guest preferences to logged-in user using username (safer than email)
      if (user.userName || user.email) {
        const userIdentifier = user.userName || user.email;
        // Use setTimeout to avoid circular dependency issues
        setTimeout(() => {
          try {
            const userPreferencesService = inject(UserPreferencesService);
            userPreferencesService.migrateGuestPreferences(userIdentifier);
          } catch (e) {
            console.warn('Could not migrate preferences:', e);
          }
        }, 0);
      }
    }
  }
/**
   * Clears all authentication data
   */
  private clearStorage(): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem(this.TOKEN_KEY);
      localStorage.removeItem(this.USER_KEY);
    }
  }
    /*
   * Gets current authentication token
   */
  getToken(): string | null {
    if (isPlatformBrowser(this.platformId)) {
      return localStorage.getItem(this.TOKEN_KEY);
    }
    return null;
  }

  /**
   * Extract userName from JWT token claims
   */
  private extractUserNameFromToken(token: string): string | null {
    if (!token) return null;

    const parts = token.split('.');
    if (parts.length < 2) return null;

    try {
      const payload = atob(parts[1].replace(/-/g, '+').replace(/_/g, '/'));
      const claims = JSON.parse(decodeURIComponent(escape(payload)));

      // Use the same claim name as navbar's getUserFullName
      const userName = claims['name'] || claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'];
      return userName || null;
    } catch (error) {
      console.error('Failed to decode token:', error);
      return null;
    }
  }

  /**
   * Get userName from stored token (for compatibility)
   */
  private getUserNameFromToken(): string | null {
    const token = this.getToken();
    return this.extractUserNameFromToken(token || '');
  }
    /**
   * Gets current user data
   */
  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }
  /**
   * Checks if user is authenticated
   */
  isAuthenticated(): boolean {
    return !!this.getToken() && !!this.currentUserSubject.value;
  }
    /**
   * Checks if user has specific role
   */
  hasRole(role: UserRole): boolean {
    const user = this.currentUserSubject.value;
    return user ? user.role === role : false;
  }
  /**
   * Refreshes token (called by interceptor)
   */
  refreshToken(): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/refresh`, {})
      .pipe(
        tap(response => {
          console.log('Token refresh response from backend:', response);
          // Backend wraps response in { result: LoginResponseVM, ... }
          const loginData = response.result;
          this.setAuthData(loginData.token, loginData.user);
        })
      );
  }
}
