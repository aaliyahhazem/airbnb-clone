import { Component, inject } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import {  FormGroup } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ListingService } from '../../../core/services/listings/listing.service';
import { ListingCreateVM, ListingUpdateVM, ListingDetailVM } from '../../../core/models/listing.model';

@Component({
  selector: 'app-listings-create-edit',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './listings-create-edit.html',
  styleUrls: ['./listings-create-edit.css']
})
export class ListingsCreateEdit {

  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private listingService = inject(ListingService);

  form!: FormGroup;

  editMode = false;
  currentId?: number;
  imagePreviews: string[] = [];
  selectedFiles: File[] = [];
  removeImageIds: number[] = [];
  existingImages: { id: number; url: string }[] = [];
  loading = false;
  error = '';
  successMessage = '';

  amenitiesList = [
    'Wi-Fi', 'Pool', 'Air Conditioning', 'Kitchen', 
    'Washer', 'Dryer', 'TV', 'Heating', 'Parking' , 'hire'
  ];

  constructor() {
    this.initForm();
  }

  private initForm(): void {
    this.form = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(5)]],
      description: ['', [Validators.required, Validators.minLength(20)]],
      pricePerNight: [0, [Validators.required, Validators.min(1)]],
      location: ['', [Validators.required, Validators.minLength(3)]],
      latitude: [0, [Validators.required]],
      longitude: [0, [Validators.required]],
      maxGuests: [1, [Validators.required, Validators.min(1)]],
      amenities: [[]] as any
    });
  }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.editMode = true;
      this.currentId = +idParam;
      this.loadListing(this.currentId);
    }
  }

  private loadListing(id: number): void {
    this.loading = true;
    this.listingService.getById(id).subscribe(
      (response) => {
        if (!response.isError && response.data) {
          this.populateForm(response.data);
          this.existingImages = (response.data.images || []).map(img => ({
            id: img.id,
            url: img.imageUrl
          }));
        } else {
          this.error = response.message || 'Failed to load listing';
          setTimeout(() => this.router.navigate(['/listings']), 2000);
        }
        this.loading = false;
      },
      (err) => {
        this.error = 'Error loading listing';
        this.loading = false;
        setTimeout(() => this.router.navigate(['/listings']), 2000);
      }
    );
  }

  private populateForm(listing: ListingDetailVM): void {
    this.form.patchValue({
      title: listing.title,
      description: listing.description,
      pricePerNight: listing.pricePerNight,
      location: listing.location,
      latitude: listing.latitude,
      longitude: listing.longitude,
      maxGuests: listing.maxGuests
    });
  }

  isInvalid(fieldName: string): boolean {
    const control = this.form.get(fieldName);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = input.files;
    if (!files) return;

    for (let i = 0; i < files.length; i++) {
      const file = files[i];
      const reader = new FileReader();
      reader.onload = () => {
        this.imagePreviews.push(reader.result as string);
        this.selectedFiles.push(file);
      };
      reader.readAsDataURL(file);
    }
  }

  removeNewImage(index: number): void {
    this.imagePreviews.splice(index, 1);
    this.selectedFiles.splice(index, 1);
  }

  removeExistingImage(imageId: number): void {
    this.removeImageIds.push(imageId);
    this.existingImages = this.existingImages.filter(img => img.id !== imageId);
  }

  toggleAmenity(amenity: string): void {
    const currentValue = this.form.get('amenities')?.value || [];
    const amenities = Array.isArray(currentValue) ? [...currentValue] : [];
    const index = amenities.indexOf(amenity);
    if (index > -1) {
      amenities.splice(index, 1);
    } else {
      amenities.push(amenity);
    }
    this.form.patchValue({ amenities });
  }

  isAmenitySelected(amenity: string): boolean {
    const amenities: string[] = this.form.get('amenities')?.value || [];
    return amenities.includes(amenity);
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.error = '';
    this.successMessage = '';

    const formValue = this.form.value;

    if (this.editMode && this.currentId) {
      const updateVM: ListingUpdateVM = {
        title: formValue.title!,
        description: formValue.description!,
        pricePerNight: formValue.pricePerNight!,
        location: formValue.location!,
        latitude: formValue.latitude!,
        longitude: formValue.longitude!,
        maxGuests: formValue.maxGuests!,
        newImages: this.selectedFiles.length > 0 ? this.selectedFiles : undefined,
        removeImageIds: this.removeImageIds.length > 0 ? this.removeImageIds : undefined,
        amenities: formValue.amenities || []
      };

      this.listingService.update(this.currentId, updateVM).subscribe(
        (response) => {
          if (!response.isError) {
            this.successMessage = 'Listing updated successfully!';
            setTimeout(() => {
              this.router.navigate(['/listings', this.currentId]);
            }, 1500);
          } else {
            this.error = response.message || 'Failed to update listing';
          }
          this.loading = false;
        },
        (err) => {
          this.error = err.error?.message || 'Error updating listing';
          this.loading = false;
        }
      );
    } else {
      const createVM: ListingCreateVM = {
        title: formValue.title!,
        description: formValue.description!,
        pricePerNight: formValue.pricePerNight!,
        location: formValue.location!,
        latitude: formValue.latitude!,
        longitude: formValue.longitude!,
        maxGuests: formValue.maxGuests!,
        images: this.selectedFiles.length > 0 ? this.selectedFiles : undefined,
        amenities: formValue.amenities || []
      };

      this.listingService.create(createVM).subscribe(
        (response) => {
          if (!response.isError) {
            this.successMessage = 'Listing created successfully!';
            setTimeout(() => {
                // after create, redirect to listings overview
                this.router.navigate(['/listings']);
            }, 1500);
          } else {
            this.error = response.message || 'Failed to create listing';
          }
          this.loading = false;
        },
        (err) => {
          this.error = err.error?.message || 'Error creating listing';
          this.loading = false;
        }
      );
    }
  }

  onDelete(): void {
    if (!this.currentId) return;
    if (!confirm('Are you sure you want to delete this listing?')) return;

    this.loading = true;
    this.listingService.delete(this.currentId).subscribe(
      (response) => {
        if (!response.isError) {
          this.successMessage = 'Listing deleted successfully!';
          setTimeout(() => {
            this.router.navigate(['/listings']);
          }, 1500);
        } else {
          this.error = response.message || 'Failed to delete listing';
        }
        this.loading = false;
      },
      (err) => {
        this.error = err.error?.message || 'Error deleting listing';
        this.loading = false;
      }
    );
  }

  goBack(): void {
    this.router.navigate(['/listings']);
  }
}
