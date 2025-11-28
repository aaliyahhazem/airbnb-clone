import { Component, OnInit, signal } from '@angular/core';
import { Router, RouterOutlet, NavigationStart, NavigationEnd, NavigationError, Event } from '@angular/router';
import { Navbar } from './shared/components/navbar/navbar';
import { Footer } from './shared/components/footer/footer';
import { NotificationHub } from './core/services/notification-hub';
import { MessageHub } from './core/services/message-hub';
import { NotificationStoreService } from './core/services/notification-store';
import { AuthService } from './core/services/auth.service';

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
    // Navigation logging removed for cleaner console; keep subscription for potential future use
    this.router.events.subscribe(() => {});
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
