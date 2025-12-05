import { Component, ChangeDetectorRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FaceCaptureComponent } from './face-capture/face-capture.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslateModule, FaceCaptureComponent],
  templateUrl: './login.html',
  styleUrls: ['./login.css']
})
export class Login {

  @ViewChild(FaceCaptureComponent) faceCapture!: FaceCaptureComponent;

  email = '';
  password = '';
  showPassword = false;

  isLoginMode: 'email' | 'face' = 'email';
  isLoading = false;

  errorMessage = '';
  successMessage = '';
  fieldErrors: { [key: string]: string } = {};

  constructor(
    private auth: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    private translate: TranslateService
  ) {}

  // ============================================================
  // UI Helpers
  // ============================================================
  togglePasswordVisibility() {
    this.showPassword = !this.showPassword;
  }

  validateField(fieldName: string, value: string) {
    this.fieldErrors[fieldName] = '';

    switch (fieldName) {
      case 'email':
        if (!value) {
          this.fieldErrors[fieldName] = this.translate.instant('auth.emailRequired');
        } else if (!this.isValidEmail(value)) {
          this.fieldErrors[fieldName] = this.translate.instant('auth.emailInvalid');
        }
        break;

      case 'password':
        if (!value) {
          this.fieldErrors[fieldName] = this.translate.instant('auth.passwordRequired');
        } else if (value.length < 6) {
          this.fieldErrors[fieldName] = this.translate.instant('auth.passwordMinLength');
        }
        break;
    }
    this.cdr.detectChanges();
  }

  private isValidEmail(email: string): boolean {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }

  // ============================================================
  // ERROR HANDLING
  // ============================================================
  private handleAuthError(error: any) {
    this.errorMessage = '';
    this.fieldErrors = {};

    if (error.status === 400) {
      if (error.error?.errors) {
        Object.keys(error.error.errors).forEach(field => {
          this.fieldErrors[field.toLowerCase()] = error.error.errors[field][0];
        });
      } else {
        this.errorMessage = error.error?.message || this.translate.instant('auth.invalidCredentials');
      }
    } else if (error.status === 401) {
      this.errorMessage = this.translate.instant('auth.invalidCredentials');
    } else if (error.status === 404) {
      this.errorMessage = this.translate.instant('auth.userNotFound');
    } else if (error.status === 0 || error.status >= 500) {
      this.errorMessage = this.translate.instant('auth.serverError');
    } else {
      this.errorMessage = error.error?.errorMessage || error.error?.message || this.translate.instant('auth.loginError');
    }

    this.cdr.detectChanges();
  }

  loginWithGoogle() {
    this.isLoading = true;
    this.auth.googleLogin().subscribe({
      next: res => {
        this.isLoading = false;
        console.log('Google login response:', res);

        // Navigate after login
        if (this.auth.isAdmin()) {
          this.router.navigate(['/admin']);
        } else {
          this.router.navigate(['/']);
        }
      },
      error: err => {
        this.isLoading = false;
        console.error('Google login failed', err);
        this.errorMessage = this.translate.instant('auth.loginError');
        this.cdr.detectChanges();
      }
    });
  }

  // ============================================================
  // SUBMIT - MAIN ENTRY
  // ============================================================
  submit(form?: NgForm) {
    if (this.isLoginMode === 'email') {
      this.submitEmailLogin();
    }
  }

  // ============================================================
  // EMAIL/PASSWORD LOGIN
  // ============================================================
  private submitEmailLogin() {
    this.errorMessage = '';
    this.fieldErrors = {};

    // Validate
    this.validateField('email', this.email);
    this.validateField('password', this.password);
    if (Object.keys(this.fieldErrors).some(k => this.fieldErrors[k])) return;

    this.isLoading = true;

    this.auth.login({ email: this.email, password: this.password }).subscribe({
      next: () => {
        this.isLoading = false;
        this.successMessage = this.translate.instant('auth.loginSuccess');

        setTimeout(() => this.navigateAfterLogin(), 800);
      },
      error: err => {
        this.isLoading = false;
        this.handleAuthError(err);
      }
    });
  }

  // ============================================================
  // FACE LOGIN
  // ============================================================
  switchToFaceLogin() {
    this.errorMessage = '';
    this.successMessage = '';
    this.isLoginMode = 'face';
    this.faceCapture.open();
  }

  switchToEmailLogin() {
    this.errorMessage = '';
    this.successMessage = '';
    this.isLoginMode = 'email';
  }

  onFaceCaptured(file: File) {
    this.errorMessage = '';
    this.successMessage = '';
    this.isLoading = true;

    this.auth.loginWithFace(file).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.successMessage = this.translate.instant('auth.loginSuccess');
        
        this.faceCapture.closeModal();
        setTimeout(() => this.navigateAfterLogin(), 800);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err?.error?.message || this.translate.instant('auth.faceLoginFailed');
        this.faceCapture.closeModal();
        this.switchToEmailLogin();
      }
    });
  }

  onFaceCaptureCancelled() {
    this.switchToEmailLogin();
    this.isLoading = false;
  }

  // ============================================================
  // NAVIGATION AFTER LOGIN
  // ============================================================
  private navigateAfterLogin() {
    const isFirstLogin = localStorage.getItem('isFirstLogin') === 'true';

    if (isFirstLogin) {
      this.router.navigate(['/onboarding']);
    } else if (this.auth.isAdmin()) {
      this.router.navigate(['/admin']);
    } else {
      this.router.navigate(['/']);
    }
  }
}
