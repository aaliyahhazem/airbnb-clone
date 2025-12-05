import { Component, OnInit, OnDestroy, ChangeDetectorRef, ElementRef, Renderer2, inject } from '@angular/core';
import { Subscription } from 'rxjs';
import { NotificationDto } from '../../../core/models/notification';
import { NotificationStoreService } from '../../../core/services/notification-store';
import { CommonModule, DatePipe } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';
import { RouterModule } from '@angular/router';
import { MessageStoreService } from '../../../core/services/message-store';
import { MessageDto } from '../../../core/models/message';
import { FavoriteStoreService } from '../../../core/services/favoriteService/favorite-store-service';
import { TranslateModule } from '@ngx-translate/core';
import { LanguageSwitcherComponent } from '../language-switcher/language-switcher.component';
import { LanguageService } from '../../../core/services/language.service';


@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterModule, TranslateModule, LanguageSwitcherComponent],
  templateUrl: './navbar.html',
  styleUrls: ['./navbar.css'],
})
export class Navbar implements OnInit, OnDestroy {
  notifications: NotificationDto[] = [];
  unreadCount = 0;
  private sub = new Subscription();
  private prevNotificationsLength = 0;
  public newIds = new Set<number>();
  messages: MessageDto[] = [];
  unreadMsgCount = 0;
  isAdmin = false;

  notificationOpen = false;
  messageOpen = false;
  favoriteCount = 0;
  private favoriteStore = inject(FavoriteStoreService);
  notificationService = inject(NotificationStoreService);
  messageService = inject(MessageStoreService);
  languageService = inject(LanguageService);
  currentLang: string = 'en';

  isAuthenticated = false;
  userFullName: string | null = null;

  private docClickUnlisten?: () => void;

  constructor(private store: NotificationStoreService, private cdr: ChangeDetectorRef, private messageStore: MessageStoreService, public auth: AuthService, private router: Router, private el: ElementRef, private renderer: Renderer2) {}

  ngOnInit() {
    this.currentLang = this.languageService.getCurrentLanguage();
    this.languageService.currentLanguage$.subscribe(lang => {
      this.currentLang = lang;
    });

    // Debug: Check current token on init
    const currentFullName = this.auth.getUserFullName();
    console.log('Navbar ngOnInit: Current user full name from token:', currentFullName);

    // Subscribe to authentication state changes
    this.sub.add(this.auth.isAuthenticated$.subscribe(isAuth => {
      Promise.resolve().then(() => {
        this.isAuthenticated = isAuth;
        // Get user's full name when authenticated
        this.userFullName = isAuth ? this.auth.getUserFullName() : null;
        console.log('Navbar: Authentication state changed:', isAuth);
        console.log('Navbar: User full name:', this.userFullName);
        // Load notifications and messages when user logs in
        if (isAuth) {
          console.log('User authenticated, loading unread notifications and messages');
          this.store.loadUnread();
          this.store.loadUnreadCount();
          this.messageStore.loadUnread();
          this.messageStore.loadUnreadCount();
            // Load favorites
          try { this.favoriteStore.loadFavorites(); } catch (err) {
            console.warn('Failed to load favorites:', err);
          }
          // If admin logged in and is on home/root, redirect to admin dashboard
          try {
            const url = this.router.url || '';
            if (this.isAdmin && (url === '/' || url.startsWith('/home') || url === '/')) {
              this.router.navigate(['/admin']);
            }
          } catch {}
        } else {
          // Clear data when logged out
          this.notifications = [];
          this.messages = [];
          this.unreadCount = 0;
          this.unreadMsgCount = 0;
          this.favoriteCount = 0;

        }
        this.cdr.detectChanges();
      });
    }));

    this.sub.add(this.store.notifications$.subscribe(list => {
      // schedule update in microtask to avoid ExpressionChangedAfterItHasBeenCheckedError
      Promise.resolve().then(() => {
        // detect newly arrived notifications (avoid treating initial load as new)
        if (this.prevNotificationsLength === 0) {
          // initial load
          this.notifications = list;
          this.prevNotificationsLength = list.length;
        } else if (list.length > this.prevNotificationsLength) {
          // new notifications appended at the beginning by prependNotification
          const added = list.slice(0, list.length - this.prevNotificationsLength);
          // collect ids of newly added notifications
          for (const n of added) {
            this.newIds.add(n.id);
            // remove highlight after 6s
            setTimeout(() => { this.newIds.delete(n.id); this.cdr.detectChanges(); }, 6000);
          }
          this.notifications = list;
          this.prevNotificationsLength = list.length;
          // auto-open notification dropdown briefly to show incoming items
          this.notificationOpen = true;
          // auto-close after 6s if user didn't interact
          setTimeout(() => { this.notificationOpen = false; this.cdr.detectChanges(); }, 6000);
        } else {
          this.notifications = list;
          this.prevNotificationsLength = list.length;
        }

        // in zoneless mode we need to trigger change detection manually
        this.cdr.detectChanges();
      });
    }));

    this.sub.add(this.store.unreadCount$.subscribe(cnt => {
      Promise.resolve().then(() => {
        this.unreadCount = cnt;
        console.log('Navbar: Unread count updated to:', cnt);
        this.cdr.detectChanges();
      });
    }));

    // messages
    this.sub.add(this.messageStore.messages$.subscribe((list: MessageDto[]) => {
      Promise.resolve().then(() => { this.messages = list; this.cdr.detectChanges(); });
    }));

    this.sub.add(this.messageStore.unreadCount$.subscribe((cnt: number) => {
      Promise.resolve().then(() => { this.unreadMsgCount = cnt; this.cdr.detectChanges(); });
    }));

    // Subscribe to favorite count
    this.sub.add(this.favoriteStore.favoriteCount$.subscribe(cnt => {
      Promise.resolve().then(() => {
        this.favoriteCount = cnt;
        this.cdr.detectChanges();
      });
    }));

    // close dropdowns when clicking outside the navbar component
    this.docClickUnlisten = this.renderer.listen('document', 'click', (evt: Event) => {
      const target = evt.target as Node;
      if (!this.el.nativeElement.contains(target)) {
        if (this.notificationOpen || this.messageOpen) {
          this.notificationOpen = false;
          this.messageOpen = false;
          this.cdr.detectChanges();
        }
      }
    });
  }

  toggleNotifications() {
    this.notificationOpen = !this.notificationOpen;
    if (this.notificationOpen) this.messageOpen = false;
  }

  openNotifications() {
    // Close dropdown first
    this.notificationOpen = false;
    // Navigate to notifications page
    console.log('Navigating to notifications, current URL:', this.router.url);
    this.router.navigate(['/notifications']).then(success => {
      console.log('Navigation to notifications:', success ? 'SUCCESS' : 'FAILED');
    });
  }

  toggleMessages() {
    this.messageOpen = !this.messageOpen;
    if (this.messageOpen) this.notificationOpen = false;
  }

  openChat() {
    // Close dropdown first
    this.messageOpen = false;
    // Navigate to messages page
    console.log('Navigating to messages, current URL:', this.router.url);
    this.router.navigate(['/messages']).then(success => {
      console.log('Navigation to messages:', success ? 'SUCCESS' : 'FAILED');
    });
  }

  markAll() { this.store.markAllAsRead(); }

  markAllMessagesRead() {
    // Mark all messages as read in the message store
    this.messageStore.markAllAsRead();
  }

  onNotificationClick(id: number, event?: Event) {
    event?.stopPropagation();
    // Mark as read and remove from newIds
    this.newIds.delete(id);
    this.store.markAsRead(id);
    // Close dropdown and navigate to notifications page
    this.notificationOpen = false;
    this.router.navigate(['/notifications']);
  }

  onMessageClick(message: MessageDto) {
    // Close dropdown and navigate to messages/chat page
    this.messageOpen = false;
    this.router.navigate(['/messages']);
  }

  markAsRead(id: number, event?: Event) {
    event?.stopPropagation();
    // if user marks it as read, remove from newIds immediately
    this.newIds.delete(id);
    this.store.markAsRead(id);
  }

  trackById(_index: number, item: NotificationDto) { return item.id; }

  ngOnDestroy() {
    this.sub.unsubscribe();
    if (this.docClickUnlisten) this.docClickUnlisten();
  }

}
