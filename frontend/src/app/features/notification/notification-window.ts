import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { NotificationDto } from '../../core/models/notification';
import { NotificationStoreService } from '../../core/services/notification-store';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-notification-window',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  templateUrl: './notification-window.html',
  styleUrls: ['./notification-window.css']
})
export class NotificationWindow implements OnInit, OnDestroy {
  notifications: NotificationDto[] = [];
  unreadCount = 0;
  private sub = new Subscription();
  public newIds = new Set<number>();
  private prevNotificationsLength = 0;

  constructor(
    private store: NotificationStoreService,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) {}

  ngOnInit() {
    // Subscribe to notifications list
    this.sub.add(this.store.notifications$.subscribe(list => {
      Promise.resolve().then(() => {
        // Detect newly arrived notifications
        if (this.prevNotificationsLength === 0) {
          // Initial load
          this.notifications = list;
          this.prevNotificationsLength = list.length;
        } else if (list.length > this.prevNotificationsLength) {
          // New notifications appended at the beginning
          const added = list.slice(0, list.length - this.prevNotificationsLength);
          // Collect ids of newly added notifications
          for (const n of added) {
            this.newIds.add(n.id);
            // Remove highlight after 6s
            setTimeout(() => {
              this.newIds.delete(n.id);
              this.cdr.detectChanges();
            }, 6000);
          }
          this.notifications = list;
          this.prevNotificationsLength = list.length;
        } else {
          this.notifications = list;
          this.prevNotificationsLength = list.length;
        }
        this.cdr.detectChanges();
      });
    }));

    // Subscribe to unread count
    this.sub.add(this.store.unreadCount$.subscribe(cnt => {
      Promise.resolve().then(() => {
        this.unreadCount = cnt;
        this.cdr.detectChanges();
      });
    }));

    // Load all notifications for this page (not just unread)
    this.store.loadAll();
  }

  markAll() {
    this.store.markAllAsRead();
  }

  markAsRead(id: number, event?: Event) {
    event?.stopPropagation();
    // Remove from newIds immediately
    this.newIds.delete(id);
    this.store.markAsRead(id);
  }

  handleAction(url: string, event?: Event) {
    event?.stopPropagation();
    this.router.navigate([url]);
  }

  trackById(_index: number, item: NotificationDto) {
    return item.id;
  }

  ngOnDestroy() {
    this.sub.unsubscribe();
  }
}
