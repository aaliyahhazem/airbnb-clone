import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslateModule],
  templateUrl: './login.html',
  styleUrls: ['./login.css']
})
export class Login {
  email = '';
  password = '';
  constructor(
    private auth: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {
    console.log('Login component initialized');
    this.cdr.markForCheck();
  }

  submit() {
    console.log('Submitting login with:', { email: this.email, password: this.password });
    this.auth.login({ email: this.email, password: this.password }).subscribe({
      next: (response) => {
        console.log('Login response:', response);

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
        console.error('Login failed', err);
        console.error('Error details:', {
          status: err.status,
          statusText: err.statusText,
          message: err.error?.message || err.message,
          errors: err.error?.errors,
          fullError: err.error
        });
      }
    });
  }
}
