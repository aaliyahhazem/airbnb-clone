import { Component, OnInit, signal, inject } from '@angular/core';
import { Router, RouterOutlet, NavigationStart, NavigationEnd, NavigationError, Event } from '@angular/router';
import { Navbar } from './shared/components/navbar/navbar';
import { Footer } from './shared/components/footer/footer';
import { NotificationHub } from './core/services/notification-hub';
import { MessageHub } from './core/services/message-hub';
import { NotificationStoreService } from './core/services/notification-store';
import { MessageStoreService } from './core/services/message-store';
import { AuthService } from './core/services/auth.service';
import { LanguageService } from './core/services/language.service';
import { TranslateService } from '@ngx-translate/core';
import { FavoriteStoreService } from './core/services/favoriteService/favorite-store-service';
import { BookingService } from './core/services/Booking/booking-service';
import { BookingStoreService } from './core/services/Booking/booking-store-service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, Navbar, Footer],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class App implements OnInit {
  protected readonly title = signal('frontend');
  private languageService = inject(LanguageService);
  private translateService = inject(TranslateService);

  constructor(
    private hub: NotificationHub,
    private store: NotificationStoreService,
    private messageHub: MessageHub,
    private messageStore: MessageStoreService,
    private auth: AuthService,
    private router: Router,
    private favoriteStore: FavoriteStoreService,
    private bookingService: BookingService,
    private bookingStore: BookingStoreService
  ) {
    // Initialize language service on app startup
    // Language detection happens automatically in LanguageService constructor
    const currentLang = this.languageService.getCurrentLanguage();
    console.log('App initialized with language:', currentLang);

    // Make sure TranslateService is using the detected language
    this.translateService.use(currentLang);

    // Navigation logging removed for cleaner console; keep subscription for potential future use
    this.router.events.subscribe(() => {});
  }

  ngOnInit(): void {
    // Start hubs and load notifications when user becomes authenticated
    this.auth.isAuthenticated$.subscribe(isAuth => {
      if (isAuth) {
        console.log('User authenticated, starting hubs and loading data');
        try { this.hub.startConnection(); } catch (err) { console.warn('Hub start failed:', err); }
        try { this.messageHub.startConnection(); } catch (err) { console.warn('Message hub start failed:', err); }
        this.store.loadInitial();
        this.messageStore.loadInitial();
        this.favoriteStore.loadFavorites();
        this.bookingService.getMyBookings();
      } else {
        console.log('User not authenticated, stopping hubs');
        try { this.hub.stopConnection(); } catch (err) { console.warn('Hub stop failed:', err); }
        try { this.messageHub.stopConnection(); } catch (err) { console.warn('Message hub stop failed:', err); }
      }
    });
  }
}
