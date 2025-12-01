import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { TranslateModule } from '@ngx-translate/core';

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
  errorMessage = '';
  constructor(private auth: AuthService, private router: Router) {}

  submit() {
    this.errorMessage = '';
    if (!this.password || !this.confirmPassword) {
      this.errorMessage = 'Please enter and confirm your password.';
      return;
    }
    if (this.password !== this.confirmPassword) {
      this.errorMessage = 'Passwords do not match.';
      return;
    }

    const payload = { fullName: this.fullname, email: this.email, password: this.password, userName: this.userName, firebaseUid: 'null' };
    this.auth.register(payload).subscribe({
      next: () => {
        // New users always get onboarding on first registration
        const isFirstLogin = localStorage.getItem('isFirstLogin') === 'true';
        if (isFirstLogin) {
          this.router.navigate(['/onboarding']);
        } else {
          this.router.navigate(['/']);
        }
      },
      error: err => {
        console.error('Register failed', err);
        this.errorMessage = err?.error?.message || 'Registration failed';
      }
    });
  }
}
