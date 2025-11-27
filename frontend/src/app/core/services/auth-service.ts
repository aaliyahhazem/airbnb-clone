import { Inject, Injectable, PLATFORM_ID } from '@angular/core';
import { BehaviorSubject, from, Observable, switchMap, tap } from 'rxjs';
import { AuthResponse, LoginCredentials, RegisterData, User, UserRole } from '../../features/auth/authModels';
import { HttpClient } from '@angular/common/http';
import { isPlatformBrowser } from '@angular/common';
import { environment } from '../../../environments/environment';
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
  private readonly TOKEN_KEY = 'auth_token';
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
   */
  private loadUserFromStorage(): void {
    if (isPlatformBrowser(this.platformId)) {
      const token = localStorage.getItem(this.TOKEN_KEY);
      const userData = localStorage.getItem(this.USER_KEY);

      if (token && userData) {
        try {
          const user: User = JSON.parse(userData);
          this.currentUserSubject.next(user);
        } catch (error) {
          this.clearStorage();
        }
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
          this.setAuthData(response.token, response.user);
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
          this.setAuthData(response.token, response.user);
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
        this.setAuthData(response.token, response.user);
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
        this.setAuthData(response.token, response.user);
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
          this.setAuthData(response.token, response.user);
        })
      );
  }
}
