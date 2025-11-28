import { Component, inject, Inject, PLATFORM_ID, ChangeDetectorRef, NgZone } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import {  FormGroup } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ListingService } from '../../../core/services/listings/listing.service';
import { ListingCreateVM, ListingUpdateVM, ListingDetailVM } from '../../../core/models/listing.model';
import { isPlatformBrowser } from '@angular/common';
import { MapService } from '../../../core/services/map/map';

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

  // Map picker state (for choosing lat/lng on a map)
  showMapPicker = false;
  private leaflet: any;
  private pickerMap: any | null = null;
  private pickerMarker: any | null = null;
  private platformId = inject(PLATFORM_ID);
  private cdr = inject(ChangeDetectorRef);
  private ngZone = inject(NgZone);
  private mapService = inject(MapService);
  private tempLatitude: number | null = null;
  private tempLongitude: number | null = null;

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
      destination: ['', [Validators.required, Validators.minLength(2)]],
      type: ['', [Validators.required]],
      latitude: [0, [Validators.required]],
      longitude: [0, [Validators.required]],
      maxGuests: [1, [Validators.required, Validators.min(1)]],
      bedrooms: [1, [Validators.required, Validators.min(1)]],
      bathrooms: [1, [Validators.required, Validators.min(1)]],
      amenities: [[]] as any
    });
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(paramMap => {
      const idParam = paramMap.get('id');
      if (idParam) {
        this.editMode = true;
        this.currentId = +idParam;
        this.loadListing(this.currentId);
      } else {
        this.editMode = false;
        this.currentId = undefined;
        this.initForm();
      }
    });
  }

  // Note: component handles both Create and Edit cases. On init, if an ID
  // is present we load the existing listing into the form (edit mode).

  toggleMapPicker(): void {
    this.showMapPicker = !this.showMapPicker;
    if (this.showMapPicker) {
      void this.initPickerMap();
    } else {
      if (this.pickerMap) {
        try { this.pickerMap.remove(); } catch {}
        this.pickerMap = null;
        this.pickerMarker = null;
      }
    }
  }

  private async initPickerMap(): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) return;
    if (this.pickerMap) return;
    try {
      this.leaflet = await import('leaflet');
      // create map in container
      const el = document.getElementById('picker-map');
      if (!el) return;

      this.pickerMap = this.leaflet.map(el, { center: [30.0444, 31.2357], zoom: 6 });
      this.leaflet.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { maxZoom: 19 }).addTo(this.pickerMap);

      // use inline SVG icon as in other maps
      const svgPin = `
        <svg width="30" height="42" viewBox="0 0 30 42" xmlns="http://www.w3.org/2000/svg">
          <path d="M15 0C9.477 0 5 4.477 5 10c0 7.5 10 22 10 22s10-14.5 10-22c0-5.523-4.477-10-10-10z" fill="#007bff"/>
          <circle cx="15" cy="11" r="4" fill="#fff"/>
        </svg>
      `;
      const customIcon = this.leaflet.divIcon({ html: svgPin, className: 'custom-leaflet-icon', iconSize: [30,42], iconAnchor: [15,42] });

      // place initial marker if form already has coords
      const lat = Number(this.form.get('latitude')?.value);
      const lng = Number(this.form.get('longitude')?.value);
      if (isFinite(lat) && isFinite(lng) && (lat !== 0 || lng !== 0)) {
        this.pickerMarker = this.leaflet.marker([lat, lng], { icon: customIcon }).addTo(this.pickerMap);
        this.pickerMap.setView([lat, lng], 14);
      }

      // click to place marker (tentative selection)
      this.pickerMap.on('click', (e: any) => {
        const { lat: clickedLat, lng: clickedLng } = e.latlng || {};
        if (!isFinite(clickedLat) || !isFinite(clickedLng)) return;
        if (this.pickerMarker) {
          try { this.pickerMarker.setLatLng([clickedLat, clickedLng]); } catch { this.pickerMarker = null; }
        }
        if (!this.pickerMarker) {
          this.pickerMarker = this.leaflet.marker([clickedLat, clickedLng], { icon: customIcon }).addTo(this.pickerMap);
        }
        // store coords temporarily (do not patch form until confirmed)
        this.tempLatitude = Number(clickedLat.toFixed(6));
        this.tempLongitude = Number(clickedLng.toFixed(6));
        // ensure form inputs reflect tentative coords immediately
        this.ngZone.run(() => {
          this.form.patchValue({ latitude: this.tempLatitude, longitude: this.tempLongitude });
          try { this.cdr.detectChanges(); } catch {}
        });
      });
    } catch (err) {
      console.warn('Picker map init failed', err);
    }
  }

  confirmPickedLocation(): void {
    // Apply the tentative coordinates to the form and optionally reverse-geocode
    if (this.tempLatitude !== null && this.tempLongitude !== null) {
      this.form.patchValue({ latitude: this.tempLatitude, longitude: this.tempLongitude });
      // call backend to reverse-geocode and fill location if available
      try {
        // Call frontend reverse geocode (Nominatim) to get a human-readable address
        this.mapService.reverseGeocode(this.tempLatitude, this.tempLongitude).subscribe((g: any) => {
          if (g && g.formattedAddress) {
            this.ngZone.run(() => {
              this.form.patchValue({ location: g.formattedAddress });
              try { this.cdr.detectChanges(); } catch {}
            });
          }
        }, () => {/* ignore geocode errors */});
      } catch {}
    }
    this.showMapPicker = false;
    if (this.pickerMap) { try { this.pickerMap.remove(); } catch {} this.pickerMap = null; this.pickerMarker = null; }
    // clear temporary coords
    this.tempLatitude = null;
    this.tempLongitude = null;
  }

  private loadListing(id: number): void {
    this.loading = true;
    this.listingService.getById(id).subscribe(
      (response) => {
        if (!response.isError && response.data) {
          // Update form and existing images inside the Angular zone
          // and trigger change detection immediately to avoid
          // ExpressionChangedAfterItHasBeenCheckedError when bindings
          // (like disabled) change as a result of populating the form.
          this.ngZone.run(() => {
            this.populateForm(response.data);
            this.existingImages = (response.data.images || []).map((img: any) => ({
              id: img.id,
              url: img.imageUrl
            }));
            try { this.cdr.detectChanges(); } catch {}
          });
        } else {
          this.error = response.message || 'Failed to load listing';
          setTimeout(() => this.router.navigate(['/host']), 2000);
        }
        this.loading = false;
      },
      (err) => {
        this.error = 'Error loading listing';
        this.loading = false;
        setTimeout(() => this.router.navigate(['/host']), 2000);
      }
    );
  }

  private populateForm(listing: ListingDetailVM): void {
    this.form.patchValue({
      title: listing.title,
      description: listing.description,
      pricePerNight: listing.pricePerNight,
      location: listing.location,
      destination: listing.destination,
      type: listing.type,
      latitude: listing.latitude,
      longitude: listing.longitude,
      maxGuests: listing.maxGuests,
      bedrooms: listing.bedrooms,
      bathrooms: listing.bathrooms
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
        this.ngZone.run(() => {
          this.imagePreviews.push(reader.result as string);
          this.selectedFiles.push(file);
          try { this.cdr.detectChanges(); } catch {}
        });
      };
      reader.readAsDataURL(file);
    }
  }

  removeNewImage(index: number): void {
    this.ngZone.run(() => {
      this.imagePreviews.splice(index, 1);
      this.selectedFiles.splice(index, 1);
      try { this.cdr.detectChanges(); } catch {}
    });
  }

  removeExistingImage(imageId: number): void {
    this.ngZone.run(() => {
      this.removeImageIds.push(imageId);
      this.existingImages = this.existingImages.filter(img => img.id !== imageId);
      try { this.cdr.detectChanges(); } catch {}
    });
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
        destination: formValue.destination!,
        type: formValue.type!,
        latitude: formValue.latitude!,
        longitude: formValue.longitude!,
        maxGuests: formValue.maxGuests!,
        numberOfRooms: formValue.bedrooms!,
        numberOfBathrooms: formValue.bathrooms!,
        newImages: this.selectedFiles.length > 0 ? this.selectedFiles : undefined,
        removeImageIds: this.removeImageIds.length > 0 ? this.removeImageIds : undefined,
        amenities: formValue.amenities || []
      };
      console.log('Update payload:', updateVM);

      this.listingService.update(this.currentId, updateVM).subscribe(
        (response) => {
          if (!response.isError) {
            this.successMessage = 'Listing updated successfully!';
            setTimeout(() => {
              this.router.navigate(['/host', this.currentId]);
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
        destination: formValue.destination!,
        type: formValue.type!,
        latitude: formValue.latitude!,
        longitude: formValue.longitude!,
        maxGuests: formValue.maxGuests!,
        numberOfRooms: formValue.bedrooms!,
        numberOfBathrooms: formValue.bathrooms!,
        images: this.selectedFiles.length > 0 ? this.selectedFiles : undefined,
        amenities: formValue.amenities || []
      };
      console.log('Create payload:', createVM);

      this.listingService.create(createVM).subscribe(
        (response) => {
          if (!response.isError) {
            this.successMessage = 'Listing created successfully!';
            setTimeout(() => {
                // after create, redirect to listings overview
                this.router.navigate(['/host']);
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
            this.router.navigate(['/host']);
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
    this.router.navigate(['/host']);
  }
}