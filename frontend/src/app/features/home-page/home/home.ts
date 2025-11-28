import { Component, HostListener, ElementRef, AfterViewInit, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HeroCard } from "../hero-card/hero-card";
import { HomeListingCard } from "../home-listing-card/home-listing-card";
import { StackedCards } from "../stacked-cards/stacked-cards";
import { ListingOverviewVM } from '../../../core/models/listing.model';
import { ListingService } from '../../../core/services/listings/listing.service';
import { Router } from '@angular/router';
import { NavigationEnd } from '@angular/router';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, HeroCard, HomeListingCard, StackedCards],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit {
  listings: ListingOverviewVM[] = [];
  loading = false;
  error: string | null = null;

  constructor(
    private listingService: ListingService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
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

