// src/app/core/services/notification-store.service.ts
import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { tap, map } from 'rxjs/operators';
import { NotificationDto } from '../models/notification';
import { NotificationHub } from './notification-hub';
import { NotificationService } from './api/notification.service';

@Injectable({ providedIn: 'root' })
export class NotificationStoreService {
  private notificationsSubject = new BehaviorSubject<NotificationDto[]>([]);
  notifications$ = this.notificationsSubject.asObservable();

  private unreadCountSubject = new BehaviorSubject<number>(0);
  unreadCount$ = this.unreadCountSubject.asObservable();

  // use NotificationService for API calls

  constructor(
    private api: NotificationService,
    private hub: NotificationHub
  ) {
    // كل ما يجي notification من الـ hub نحدّث الستيت محليًا
    this.hub.notificationReceived.subscribe(n => this.prependNotification(n));
  }

  // Load unread notifications for navbar dropdown
  loadUnread() {
    this.api.getUnread()
      .pipe(
        map((res: any) => Array.isArray(res.result) ? res.result : []),
        tap((list: NotificationDto[]) => {
          this.notificationsSubject.next(list);
          this.unreadCountSubject.next(list.length);
          console.log('Unread notifications loaded:', list.length);
        })
      )
      .subscribe({
        next: (list: NotificationDto[]) => console.log('Unread notifications:', list),
        error: err => console.error('Failed to load unread notifications', err)
      });
  }

  // Load all notifications for notification window page
  loadAll() {
    this.api.getForCurrentUser()
      .pipe(
        map((res: any) => Array.isArray(res.result) ? res.result : []),
        tap((list: NotificationDto[]) => {
          this.notificationsSubject.next(list);
          this.unreadCountSubject.next(list.filter((x: NotificationDto) => !x.isRead).length);
          console.log('All notifications loaded:', list.length);
        })
      )
      .subscribe({
        next: (list: NotificationDto[]) => console.log('All notifications:', list),
        error: err => console.error('Failed to load all notifications', err)
      });
  }

  // Backward compatibility - loads unread by default
  loadInitial() {
    this.loadUnread();
  }

  private prependNotification(n: NotificationDto) {
    const current = this.notificationsSubject.value;
    this.notificationsSubject.next([n, ...current]);
    this.unreadCountSubject.next(this.unreadCountSubject.value + (n.isRead ? 0 : 1));
  }

  markAsRead(id: number) {
    return this.api.markAsRead(id).pipe(
      tap(() => {
        const list = this.notificationsSubject.value.map(x => x.id === id ? { ...x, isRead: true } : x);
        this.notificationsSubject.next(list);
        this.unreadCountSubject.next(list.filter(x => !x.isRead).length);
      })
    );
  }

  markAllAsRead() {
    return this.api.markAllAsRead().pipe(
      tap(() => {
        const list = this.notificationsSubject.value.map(x => ({ ...x, isRead: true }));
        this.notificationsSubject.next(list);
        this.unreadCountSubject.next(0);
      })
    );
  }
}
