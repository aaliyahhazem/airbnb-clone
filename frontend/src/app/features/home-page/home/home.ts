import { Component, HostListener, ElementRef, AfterViewInit, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HomeListingCard } from "../home-listing-card/home-listing-card";
import { ListingOverviewVM } from '../../../core/models/listing.model';
import { ListingService } from '../../../core/services/listings/listing.service';
import { Router, RouterModule } from '@angular/router';
import { NavigationEnd } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { LanguageService } from '../../../core/services/language.service';
import { RagChatService } from '../../../core/services/chat/rag-chat.service';
import { UserPreferencesService } from '../../../core/services/user-preferences/user-preferences.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, HomeListingCard, TranslateModule, RouterModule],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit {
  listings: ListingOverviewVM[] = [];
  loading = false;
  error: string | null = null;
  private languageService = inject(LanguageService);
  currentLang: string = 'en';

  // Pagination state for each section
  topPriorityPage = 1;
  villaPage = 1;
  cairoPage = 1;
  itemsPerPage = 6;

  constructor(
    private listingService: ListingService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    private chatService: RagChatService,
    private userPreferences: UserPreferencesService
  ) { }

  viewOnMap() {
    this.router.navigate(['/map']);
  }

  get topPriorityListings(): ListingOverviewVM[] {
    const sorted = this.listings
      .sort((a, b) => (b.priority || 0) - (a.priority || 0));
    // Apply personalized sorting to top results
    const personalized = this.userPreferences.sortByRelevance(sorted);
    const start = (this.topPriorityPage - 1) * this.itemsPerPage;
    return personalized.slice(start, start + this.itemsPerPage);
  }

  get topPriorityTotal(): number {
    return this.listings
      .sort((a, b) => (b.priority || 0) - (a.priority || 0)).length;
  }

  get topPriorityPages(): number {
    return Math.ceil(this.topPriorityTotal / this.itemsPerPage);
  }

  get cairoListings(): ListingOverviewVM[] {
    const filtered = this.listings
      .filter(l => l.destination?.toLowerCase().includes('cairo') || l.location?.toLowerCase().includes('cairo'));
    const start = (this.cairoPage - 1) * this.itemsPerPage;
    return filtered.slice(start, start + this.itemsPerPage);
  }

  get cairoTotal(): number {
    return this.listings
      .filter(l => l.destination?.toLowerCase().includes('cairo') || l.location?.toLowerCase().includes('cairo')).length;
  }

  get cairoPages(): number {
    return Math.ceil(this.cairoTotal / this.itemsPerPage);
  }

  get villaListings(): ListingOverviewVM[] {
    const filtered = this.listings
      .filter(l => l.type?.toLowerCase().includes('villa'));
    const start = (this.villaPage - 1) * this.itemsPerPage;
    return filtered.slice(start, start + this.itemsPerPage);
  }

  get villaTotal(): number {
    return this.listings
      .filter(l => l.type?.toLowerCase().includes('villa')).length;
  }

  get villaPages(): number {
    return Math.ceil(this.villaTotal / this.itemsPerPage);
  }

  // Clicking on service cards
  scrollToVillas() {
    this.scrollToElement('villas-section');
  }

  scrollToFeatured() {
    this.scrollToElement('featured-section');
  }

  scrollToCairo() {
    this.scrollToElement('cairo-section');
  }

  private scrollToElement(elementId: string) {

    const element = document.getElementById(elementId);
    if (element) {
      element.scrollIntoView({
        behavior: 'smooth',
        block: 'start'
      });
    }
  }

  // Pagination methods for top priority
  nextTopPriorityPage(): void {
    if (this.topPriorityPage < this.topPriorityPages) {
      this.topPriorityPage++;
      this.cdr.markForCheck();
    }
  }

  prevTopPriorityPage(): void {
    if (this.topPriorityPage > 1) {
      this.topPriorityPage--;
      this.cdr.markForCheck();
    }
  }

  // Pagination methods for villas
  nextVillaPage(): void {
    if (this.villaPage < this.villaPages) {
      this.villaPage++;
      this.cdr.markForCheck();
    }
  }

  prevVillaPage(): void {
    if (this.villaPage > 1) {
      this.villaPage--;
      this.cdr.markForCheck();
    }
  }

  // Pagination methods for cairo
  nextCairoPage(): void {
    if (this.cairoPage < this.cairoPages) {
      this.cairoPage++;
      this.cdr.markForCheck();
    }
  }

  prevCairoPage(): void {
    if (this.cairoPage > 1) {
      this.cairoPage--;
      this.cdr.markForCheck();
    }
  }

  ngOnInit(): void {
    this.currentLang = this.languageService.getCurrentLanguage();
    this.languageService.currentLanguage$.subscribe(lang => {
      this.currentLang = lang;
    });

    this.loadListings();

    this.router.events.subscribe(event => {
      if (event instanceof NavigationEnd && this.router.url.startsWith('/listings')) {
        this.loadListings();
      }
    });
  }

  loadListings(): void {
    this.loading = true;
    this.error = null;

    this.listingService.getPaged().subscribe({
      next: (res) => {
        this.listings = res.data || [];
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Error loading listings:', err);
        this.error = 'Failed to load listings';
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  checkHaveListing(): void {
    this.listingService.checkUserHasListings().subscribe({
      next: (response) => {
        if (response.hasListings) {
          // User has listings, navigate to host dashboard
          this.router.navigate(['/host']);
        } else {
          // User has no listings, navigate to create listing
          this.router.navigate(['/host/create']);
        }
      },
      error: (err) => {
        console.error('Error checking listings:', err);
        // Default to create page on error
        this.router.navigate(['/host/create']);
      }
    });
  }

  openChat(): void {
    this.chatService.openChat();
  }
}
