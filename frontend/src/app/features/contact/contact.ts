import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule],
  templateUrl: './contact.html',
  styleUrl: './contact.css'
})
export class ContactComponent {
  contactForm: FormGroup;
  isSubmitting = false;
  submitSuccess = false;

  contactMethods = [
    {
      icon: 'bi-telephone-fill',
      titleKey: 'contact.methods.phone.title',
      valueKey: 'contact.methods.phone.value',
      value: '+20 123 456 789'
    },
    {
      icon: 'bi-envelope-fill',
      titleKey: 'contact.methods.email.title',
      valueKey: 'contact.methods.email.value',
      value: 'support@thebroker.com'
    },
    {
      icon: 'bi-geo-alt-fill',
      titleKey: 'contact.methods.address.title',
      valueKey: 'contact.methods.address.value',
      value: 'Cairo, Egypt'
    },
    {
      icon: 'bi-clock-fill',
      titleKey: 'contact.methods.hours.title',
      valueKey: 'contact.methods.hours.value',
      value: '24/7'
    }
  ];

  faqs = [
    {
      questionKey: 'contact.faqs.booking.question',
      answerKey: 'contact.faqs.booking.answer'
    },
    {
      questionKey: 'contact.faqs.cancellation.question',
      answerKey: 'contact.faqs.cancellation.answer'
    },
    {
      questionKey: 'contact.faqs.payment.question',
      answerKey: 'contact.faqs.payment.answer'
    },
    {
      questionKey: 'contact.faqs.hosting.question',
      answerKey: 'contact.faqs.hosting.answer'
    }
  ];

  constructor(private fb: FormBuilder) {
    this.contactForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      subject: ['', [Validators.required, Validators.minLength(3)]],
      message: ['', [Validators.required, Validators.minLength(10)]]
    });
  }

  onSubmit() {
    if (this.contactForm.valid) {
      this.isSubmitting = true;
      // Simulate form submission
      setTimeout(() => {
        this.isSubmitting = false;
        this.submitSuccess = true;
        this.contactForm.reset();
        // Reset success message after 3 seconds
        setTimeout(() => {
          this.submitSuccess = false;
        }, 3000);
      }, 2000);
    } else {
      this.markFormGroupTouched();
    }
  }

  private markFormGroupTouched() {
    Object.keys(this.contactForm.controls).forEach(key => {
      const control = this.contactForm.get(key);
      control?.markAsTouched();
    });
  }

  getFieldError(fieldName: string): string | null {
    const control = this.contactForm.get(fieldName);
    if (control && control.errors && control.touched) {
      if (control.errors['required']) return 'validation.required';
      if (control.errors['email']) return 'validation.invalidEmail';
      if (control.errors['minlength']) return 'validation.minLength';
    }
    return null;
  }
}
