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
  isLoginMode = 'email'; // 'email' or 'face'
  showPassword = false;
  isLoading = false;
  errorMessage = '';
  fieldErrors: { [key: string]: string } = {};

  constructor(
    private auth: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    private translate: TranslateService
  ) {
    console.log('Login component initialized');
    this.cdr.markForCheck();
  }

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

  private handleAuthError(error: any) {
    this.errorMessage = '';
    this.fieldErrors = {};

    console.log('Full error object:', error);
    console.log('Error.error:', error.error);

    if (error.status === 400) {
      // Bad request - validation errors
      if (error.error?.errors) {
        // Handle validation errors from backend
        Object.keys(error.error.errors).forEach(field => {
          const fieldName = field.toLowerCase();
          this.fieldErrors[fieldName] = error.error.errors[field][0];
        });
      } else if (error.error?.errorMessage) {
        // Handle the backend error structure: error.error.errorMessage
        this.errorMessage = error.error.errorMessage;
      } else if (error.error?.message) {
        // Fallback to message field
        this.errorMessage = error.error.message;
      } else {
        // Fallback error message
        this.errorMessage = this.translate.instant('auth.invalidCredentials');
      }
    } else if (error.status === 401) {
      // Unauthorized - wrong credentials
      this.errorMessage = this.translate.instant('auth.invalidCredentials');
    } else if (error.status === 404) {
      // User not found
      this.errorMessage = this.translate.instant('auth.userNotFound');
    } else if (error.status === 0 || error.status >= 500) {
      // Network or server error
      this.errorMessage = this.translate.instant('auth.serverError');
    } else {
      // Generic error
      this.errorMessage = error.error?.errorMessage || error.error?.message || this.translate.instant('auth.loginError');
    }

    this.cdr.detectChanges();
  }

  submit(form?: NgForm) {
    // Reset previous errors
    this.errorMessage = '';
    this.fieldErrors = {};

    // Validate form
    this.validateField('email', this.email);
    this.validateField('password', this.password);

    // Check if there are validation errors
    if (Object.keys(this.fieldErrors).some(key => this.fieldErrors[key])) {
      return;
    }

    this.isLoading = true;
  submit() {
    if (this.isLoginMode === 'email') {
      this.submitEmailLogin();
    }
    // Face login is triggered separately via switchToFaceLogin()
  }

  private submitEmailLogin() {
    this.errorMessage = '';
    this.successMessage = '';
    
    if (!this.email || !this.password) {
      this.errorMessage = this.translate.instant('auth.emailPasswordRequired') || 'Please enter both email and password';
      return;
    }

    console.log('Submitting login with:', { email: this.email, password: this.password });
    this.isLoading = true;
    this.cdr.markForCheck();
    
    this.auth.login({ email: this.email, password: this.password }).subscribe({
      next: (response) => {
        console.log('Login response:', response);
        this.isLoading = false;

        // Check if this is first-time login
        const isFirstLogin = localStorage.getItem('isFirstLogin') === 'true';

        if (isFirstLogin) {
          // Redirect to onboarding for first-time users
          this.router.navigate(['/onboarding']);
        } else if (this.auth.isAdmin()) {
          // Admin users go to admin dashboard
          this.router.navigate(['/admin']);
        } else {
          // Regular users go to home
          this.router.navigate(['/']);
        }
      },
      error: err => {
        this.isLoading = false;
        console.error('Login failed', err);
        console.error('Error details:', {
          status: err.status,
          statusText: err.statusText,
          message: err.error?.message || err.message,
          errors: err.error?.errors,
          fullError: err.error
        });
        this.isLoading = false;
        this.errorMessage = err?.error?.message || this.translate.instant('auth.loginFailed') || 'Login failed. Please try again.';
        this.cdr.markForCheck();
        this.handleAuthError(err);
      }
    });
  }

  switchToFaceLogin() {
    this.isLoginMode = 'face';
    this.errorMessage = '';
    this.successMessage = '';
    this.faceCapture.open();
  }

  switchToEmailLogin() {
    this.isLoginMode = 'email';
    this.errorMessage = '';
    this.successMessage = '';
  }

  onFaceCaptured(file: File) {
    this.errorMessage = '';
    this.successMessage = '';
    this.isLoading = true;
    this.cdr.markForCheck();
    
    this.auth.loginWithFace(file).subscribe({
      next: (response) => {
        console.log('Face login response:', response);
        this.isLoading = false;
        this.successMessage = this.translate.instant('auth.loginSuccess') || 'Login successful!';
        this.cdr.markForCheck();
        
        // Close the face capture modal
        this.faceCapture.closeModal();
        
        setTimeout(() => {
          this.navigateAfterLogin();
        }, 800);
      },
      error: (err) => {
        console.error('Face login failed:', err);
        this.isLoading = false;
        this.errorMessage = err?.error?.message || this.translate.instant('auth.faceLoginFailed') || 'Face recognition failed. Please try again or use email/password.';
        this.isLoginMode = 'email';
        
        // Close the face capture modal on error
        this.faceCapture.closeModal();
        
        this.cdr.markForCheck();
      }
    });
  }

  onFaceCaptureCancelled() {
    this.isLoginMode = 'email';
    this.isLoading = false;
    this.cdr.markForCheck();
  }

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
