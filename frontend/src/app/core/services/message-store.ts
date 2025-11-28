import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { MessageDto } from '../models/message';
import { MessageHub } from './message-hub';
import { MessageService } from './api/message.service';

@Injectable({ providedIn: 'root' })
export class MessageStoreService {
  private messagesSubject = new BehaviorSubject<MessageDto[]>([]);
  messages$ = this.messagesSubject.asObservable();

  private unreadCountSubject = new BehaviorSubject<number>(0);
  unreadCount$ = this.unreadCountSubject.asObservable();

  constructor(private hub: MessageHub, private api: MessageService) {
    this.hub.messageReceived.subscribe(m => this.prepend(m));
  }

  // Load unread messages for navbar dropdown
  loadUnread() {
    this.api.getUnread().subscribe((res: any) => {
      const msgs = Array.isArray(res.result) ? res.result : [];
      const list: MessageDto[] = msgs.map((m: any) => ({
        id: m.id ?? 0,
        senderId: m.senderId ?? m.senderUserName ?? '',
        receiverId: m.receiverId ?? '',
        content: m.content ?? '',
        sentAt: m.sentAt ?? '',
        isRead: false // These are unread messages
      }));

      this.messagesSubject.next(list);
      this.unreadCountSubject.next(list.length);
      console.log('Unread messages loaded:', list.length);
    }, err => console.error('Failed to load unread messages', err));
  }

  // Load all conversations for chat window
  loadConversations() {
    this.api.getConversations().subscribe((res: any) => {
      const convs = Array.isArray(res.result) ? res.result : [];
      const list: MessageDto[] = convs.map((c: any, idx: number) => ({
        id: 0,
        senderId: c.otherUserName ?? c.otherUserId?.toString() ?? `user-${idx}`,
        receiverId: '',
        content: c.lastMessage ?? '',
        sentAt: c.lastSentAt ?? '',
        isRead: (c.unreadCount ?? 0) === 0
      }));

      this.messagesSubject.next(list);
      this.unreadCountSubject.next(convs.reduce((acc: number, c: any) => acc + (c.unreadCount ?? 0), 0));
      console.log('All conversations loaded:', list.length);
    }, err => console.error('Failed to load conversations', err));
  }

  // Backward compatibility - loads unread by default
  loadInitial() {
    this.loadUnread();
  }

  markAsRead(id: number) {
    // Update local state immediately
    const msgs = this.messagesSubject.value.slice();
    let changed = false;
    for (let i = 0; i < msgs.length; i++) {
      if (msgs[i].id === id && !msgs[i].isRead) {
        msgs[i] = { ...msgs[i], isRead: true } as MessageDto;
        changed = true;
        break;
      }
    }
    if (changed) {
      this.messagesSubject.next(msgs);
      const newCount = Math.max(0, this.unreadCountSubject.value - 1);
      this.unreadCountSubject.next(newCount);
    }

    // Call backend API to mark as read (don't wait for response)
    if (id > 0) {
      this.api.markAsRead(id).subscribe({
        next: () => console.log('Message marked as read on backend:', id),
        error: (err) => console.error('Failed to mark message as read on backend:', err)
      });
    }
  }

  markAllAsRead() {
    // Get all message IDs that need to be marked
    const msgs = this.messagesSubject.value.slice();
    const unreadIds = msgs.filter(m => !m.isRead && m.id > 0).map(m => m.id);

    // Update local state immediately
    let changed = false;
    for (let i = 0; i < msgs.length; i++) {
      if (!msgs[i].isRead) {
        msgs[i] = { ...msgs[i], isRead: true } as MessageDto;
        changed = true;
      }
    }
    if (changed) {
      this.messagesSubject.next(msgs);
      this.unreadCountSubject.next(0);
    }

    // Call backend API for each unread message
    for (const id of unreadIds) {
      this.api.markAsRead(id).subscribe({
        next: () => console.log('Message marked as read on backend:', id),
        error: (err) => console.error('Failed to mark message as read on backend:', err)
      });
    }
  }

  private prepend(m: MessageDto) {
    const current = this.messagesSubject.value;
    this.messagesSubject.next([m, ...current]);
    this.unreadCountSubject.next(this.unreadCountSubject.value + (m.isRead ? 0 : 1));
  }
}
