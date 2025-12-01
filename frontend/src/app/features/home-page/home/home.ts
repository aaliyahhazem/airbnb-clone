import { Component, HostListener, ElementRef, AfterViewInit, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HeroCard } from "../hero-card/hero-card";
import { HomeListingCard } from "../home-listing-card/home-listing-card";
import { StackedCards } from "../stacked-cards/stacked-cards";
import { ListingOverviewVM } from '../../../core/models/listing.model';
import { ListingService } from '../../../core/services/listings/listing.service';
import { Router, RouterModule } from '@angular/router';
import { NavigationEnd } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { LanguageService } from '../../../core/services/language.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, HeroCard, HomeListingCard, StackedCards, TranslateModule, RouterModule],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit {
  listings: ListingOverviewVM[] = [];
  loading = false;
  error: string | null = null;
  private languageService = inject(LanguageService);
  currentLang: string = 'en';

  constructor(
    private listingService: ListingService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  get topPriorityListings(): ListingOverviewVM[] {
    return this.listings
      .sort((a, b) => (b.priority || 0) - (a.priority || 0))
      .slice(0, 6);
  }

  get cairoListings(): ListingOverviewVM[] {
    return this.listings
      .filter(l => l.destination?.toLowerCase().includes('cairo') || l.location?.toLowerCase().includes('cairo'))
      .slice(0, 6);
  }

  get villaListings(): ListingOverviewVM[] {
    return this.listings
      .filter(l => l.type?.toLowerCase().includes('villa'))
      .slice(0, 6);
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
}