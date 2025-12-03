import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslateModule],
  templateUrl: './register.html',
  styleUrls: ['./register.css']
})
export class Register {
  fullname = '';
  email = '';
  password = '';
  confirmPassword = '';
  userName = '';
  showPassword = false;
  showConfirmPassword = false;
  isLoading = false;
  errorMessage = '';
  fieldErrors: { [key: string]: string } = {};

  constructor(
    private auth: AuthService,
    private router: Router,
    private translate: TranslateService,
    private cdr: ChangeDetectorRef
  ) {}

  togglePasswordVisibility() {
    this.showPassword = !this.showPassword;
  }

  toggleConfirmPasswordVisibility() {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  validateField(fieldName: string, value: string) {
    this.fieldErrors[fieldName] = '';

    switch (fieldName) {
      case 'fullname':
        if (!value || value.trim().length < 2) {
          this.fieldErrors[fieldName] = this.translate.instant('auth.fullNameMinLength');
        }
        break;
      case 'userName':
        if (!value || value.trim().length < 3) {
          this.fieldErrors[fieldName] = this.translate.instant('auth.usernameMinLength');
        } else if (!/^[a-zA-Z0-9_]+$/.test(value)) {
          this.fieldErrors[fieldName] = this.translate.instant('auth.usernameInvalid');
        }
        break;
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
        } else if (!/(?=.*[a-z])(?=.*\d)/.test(value)) {
          this.fieldErrors[fieldName] = this.translate.instant('auth.passwordStrength');
        }
        // Re-validate confirm password if it's already filled
        if (this.confirmPassword) {
          this.validateField('confirmPassword', this.confirmPassword);
        }
        break;
      case 'confirmPassword':
        if (!value) {
          this.fieldErrors[fieldName] = this.translate.instant('auth.confirmPasswordRequired');
        } else if (value !== this.password) {
          this.fieldErrors[fieldName] = this.translate.instant('auth.passwordsNotMatch');
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
        const backendMessage = error.error.errorMessage;
        if (backendMessage.toLowerCase().includes('email')) {
          this.fieldErrors['email'] = this.translate.instant('auth.emailTaken');
        } else if (backendMessage.toLowerCase().includes('username')) {
          this.fieldErrors['userName'] = this.translate.instant('auth.usernameTaken');
        } else {
          this.errorMessage = backendMessage;
        }
      } else if (error.error?.message) {
        // Fallback to message field
        if (error.error.message.toLowerCase().includes('email')) {
          this.fieldErrors['email'] = this.translate.instant('auth.emailTaken');
        } else if (error.error.message.toLowerCase().includes('username')) {
          this.fieldErrors['userName'] = this.translate.instant('auth.usernameTaken');
        } else {
          this.errorMessage = error.error.message;
        }
      } else {
        // Fallback error message
        this.errorMessage = this.translate.instant('auth.registerError');
      }
    } else if (error.status === 409) {
      // Conflict - duplicate data
      this.errorMessage = this.translate.instant('auth.duplicateData');
    } else if (error.status === 0 || error.status >= 500) {
      // Network or server error
      this.errorMessage = this.translate.instant('auth.serverError');
    } else {
      // Generic error
      this.errorMessage = error.error?.errorMessage || error.error?.message || this.translate.instant('auth.registerError');
    }

    this.cdr.detectChanges();
  }  submit(form?: NgForm) {
    // Reset previous errors
    this.errorMessage = '';
    this.fieldErrors = {};

    // Validate all fields
    this.validateField('fullname', this.fullname);
    this.validateField('userName', this.userName);
    this.validateField('email', this.email);
    this.validateField('password', this.password);
    this.validateField('confirmPassword', this.confirmPassword);

    // Check if there are validation errors
    if (Object.keys(this.fieldErrors).some(key => this.fieldErrors[key])) {
      return;
    }

    this.isLoading = true;

    const payload = {
      fullName: this.fullname,
      email: this.email,
      password: this.password,
      userName: this.userName,
      firebaseUid: 'null'
    };

    this.auth.register(payload).subscribe({
      next: () => {
        this.isLoading = false;
        // New users always get onboarding on first registration
        const isFirstLogin = localStorage.getItem('isFirstLogin') === 'true';
        if (isFirstLogin) {
          this.router.navigate(['/onboarding']);
        } else {
          this.router.navigate(['/']);
        }
      },
      error: err => {
        this.isLoading = false;
        console.error('Register failed', err);
        this.handleAuthError(err);
      }
    });
  }
}
