import { Component, OnInit, signal } from '@angular/core';
import { Router, RouterOutlet, NavigationStart, NavigationEnd, NavigationError, Event } from '@angular/router';
import { Navbar } from './app/shared/components/navbar/navbar';
import { Footer } from './app/shared/components/footer/footer';
import { NotificationHub } from './app/core/services/notification-hub';
import { MessageHub } from './app/core/services/message-hub';
import { NotificationStoreService } from './app/core/services/notification-store';
import { AuthService } from './app/core/services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, Navbar, Footer],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class App implements OnInit {
  protected readonly title = signal('frontend');

  constructor(
    private hub: NotificationHub,
    private store: NotificationStoreService,
    private messageHub: MessageHub,
    private auth: AuthService,
    private router: Router
  ) {
    // helpful debug logs for navigation
    console.log('ROUTER CONFIG:', this.router.config);
    this.router.events.subscribe((e: Event) => {
      if (e instanceof NavigationStart) console.log('NAV START ->', e.url);
      if (e instanceof NavigationEnd) console.log('NAV END   ->', e.url);
      if (e instanceof NavigationError) console.error('NAV ERROR ->', e.error);
    });
  }

  ngOnInit(): void {
    // Start hubs and load notifications when user becomes authenticated
    this.auth.isAuthenticated$.subscribe(isAuth => {
      if (isAuth) {
        try { this.hub.startConnection(); } catch (err) { console.warn('Hub start failed:', err); }
        try { this.messageHub.startConnection(); } catch (err) { console.warn('Message hub start failed:', err); }
        this.store.loadInitial();
      }
    });
  }
}
