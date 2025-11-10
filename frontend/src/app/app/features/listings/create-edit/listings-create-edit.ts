import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ListingsService } from '../services/listings';
import { Listing } from '../models/listing.model';

@Component({
  selector: 'app-listings-create-edit',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './listings-create-edit.html',
  styleUrls: ['./listings-create-edit.css']
})
export class ListingsCreateEdit implements OnInit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private service = inject(ListingsService);

  form = this.fb.group({
    title: ['', Validators.required],
    location: ['', Validators.required],
    price: [1, [Validators.required, Validators.min(1)]],
    description: [''],
    rating: [0, [Validators.min(0), Validators.max(5)]],
    dateAvailable: [''],
    imageUrl: ['']
  });

  editMode = false;
  currentId?: number;
  imagePreview?: string;

  ngOnInit(): void {
    // Check if we're in edit mode by looking for an ID param on the route
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.editMode = true;
      this.currentId = +idParam;
      // Load existing listing data into the form
      const existing = this.service.getById(this.currentId);
      if (existing) {
        // Patch form values with existing listing data
        this.form.patchValue(existing);
        this.imagePreview = existing.imageUrl;
      } else {
        this.router.navigate(['/listings']);
      }
    }
  }

  isInvalid(name: string): boolean {
    const c = this.form.get(name);
    return !!c && c.invalid && (c.touched );
  }

  // Handle image file input change to show preview 
  onFileChange(e: Event) {
    const input = e.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = () => {
      const dataUrl = String(reader.result);
      this.imagePreview = dataUrl;
      this.form.patchValue({ imageUrl: dataUrl });
    };
    reader.readAsDataURL(file);
  }

  onSubmit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const payload = this.form.value as Omit<Listing, 'id'>;

    if (this.editMode && this.currentId) {
      this.service.update(this.currentId, payload);
    } else {
      this.service.create(payload);
    }
    this.router.navigate(['/listings']);
  }

  onDelete() {
    if (!this.currentId) return;
    if (!confirm('Delete this listing?')) return;
    this.service.remove(this.currentId);
    this.router.navigate(['/listings']);
  }

  goBack() {
    this.router.navigate(['/listings']);
  }
}
