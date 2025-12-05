import { Component, ViewChild, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FaceCaptureComponent } from './face-capture/face-capture.component';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslateModule, FaceCaptureComponent],
  templateUrl: './register.html',
  styleUrls: ['./register.css']
})
export class Register {
  @ViewChild(FaceCaptureComponent) faceCapture!: FaceCaptureComponent;

  fullname = '';
  email = '';
  password = '';
  confirmPassword = '';
  userName = '';
  errorMessage = '';
  successMessage = '';
  capturedFaceFile: File | null = null;
  isLoading = false;
  showFaceRegistrationPrompt = false;
  
  constructor(
    private auth: AuthService,
    private router: Router,
    private translate: TranslateService,
    private cdr: ChangeDetectorRef
  ) { }

  submit() {
    this.errorMessage = '';
    this.successMessage = '';
    
    if (!this.password || !this.confirmPassword) {
      this.errorMessage = this.translate.instant('auth.passwordRequired') || 'Please enter and confirm your password.';
      return;
    }
    if (this.password !== this.confirmPassword) {
      this.errorMessage = this.translate.instant('auth.passwordMismatch') || 'Passwords do not match.';
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
        this.successMessage = this.translate.instant('auth.registerSuccess') || 'Registration successful!';
        
        // Show face registration suggestion prompt
        setTimeout(() => {
          this.showFaceRegistrationPrompt = true;
        }, 800);
      },
      error: err => {
        console.error('Register failed', err);
        this.isLoading = false;
        this.errorMessage = err?.error?.message || this.translate.instant('auth.registerFailed') || 'Registration failed';
      }
    });
  }

  proceedWithFaceRegistration() {
    this.showFaceRegistrationPrompt = false;
    this.faceCapture.open();
  }

  skipFaceRegistration() {
    this.showFaceRegistrationPrompt = false;
    this.proceedToNextStep();
  }

  onFaceCaptured(file: File) {
    this.isLoading = true;
    const userId = this.auth.getPayload()?.sub || this.auth.getPayload()?.nameid;
    
    if (!userId) {
      this.isLoading = false;
      this.errorMessage = this.translate.instant('auth.userIdError') || 'Unable to get user ID. Please try again.';
      return;
    }

    this.auth.registerFace(userId, file).subscribe({
      next: (res) => {
        console.log('Face registration successful:', res);
        this.isLoading = false;
        this.successMessage = this.translate.instant('auth.faceRegistrationSuccess') || 'Face registered successfully!';
        
        // Close the face capture modal
        this.faceCapture.closeModal();
        
        setTimeout(() => {
          this.proceedToNextStep();
        }, 1500);
      },
      error: (err) => {
        console.error('Face registration failed:', err);
        this.isLoading = false;
        this.errorMessage = err?.error?.message || this.translate.instant('auth.faceRegistrationFailed') || 'Face registration failed. Please try again.';
        
        // Close the face capture modal on error
        this.faceCapture.closeModal();
      }
    });
  }

    this.cdr.detectChanges();
  }

  signUpWithGoogle() {
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

  submit(form?: NgForm) {
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
  onFaceCaptureCancelled() {
    this.showFaceRegistrationPrompt = false;
    this.proceedToNextStep();
  }

  private proceedToNextStep() {
    const isFirstLogin = localStorage.getItem('isFirstLogin') === 'true';
    if (isFirstLogin) {
      this.router.navigate(['/onboarding']);
    } else {
      this.router.navigate(['/']);
    }
  }
}

