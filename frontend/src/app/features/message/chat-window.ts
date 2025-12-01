import { Component, OnInit, ViewChild, ElementRef, ChangeDetectorRef, NgZone, Injector } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
// services are dynamically imported inside ngOnInit to avoid circular/undefined token issues

@Component({
  selector: 'app-chat-window',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  templateUrl: './chat-window.html',
  styleUrls: ['./chat-window.css']
})
export class ChatWindow implements OnInit {
  messages: any[] = [];
  conversations: any[] = [];
  selectedUserId: string | null = null;
  newReceiverId = '';
  newMessage = '';
  currentUserId: string | null = null;
  selectedMessages: any[] = [];
  selectedUserName: string | null = null;
  isTyping = false; // Add typing indicator property

  // services will be obtained from injector in ngOnInit to avoid circular injection/undefined-token issues
  private api: any;
  private store: any;
  private auth: any;
  private hub: any;

  @ViewChild('messagesContainer') private messagesContainer!: ElementRef;

  constructor(private injector: Injector, private cdr: ChangeDetectorRef, private zone: NgZone) {}

  get selectedUserDisplayName(): string | null {
    const conv = this.conversations.find((c: any) => c.otherUserId === this.selectedUserId);
    return conv ? conv.otherUserName : this.selectedUserName;
  }

  async ngOnInit(): Promise<void> {
    // dynamic import the service classes then resolve them from the injector
    const msMod = await import('../../core/services/api/message.service');
    const storeMod = await import('../../core/services/message-store');
    const hubMod = await import('../../core/services/message-hub');
    const authMod = await import('../../core/services/auth.service');

    const MessageServiceClass = msMod.MessageService;
    const MessageStoreClass = storeMod.MessageStoreService;
    const MessageHubClass = hubMod.MessageHub;
    const AuthServiceClass = authMod.AuthService;

    this.api = this.injector.get(MessageServiceClass);
    this.store = this.injector.get(MessageStoreClass);
    this.auth = this.injector.get(AuthServiceClass);
    this.hub = this.injector.get(MessageHubClass);

    const payload = this.auth.getPayload();
    this.currentUserId = payload?.sub || payload?.id || payload?.nameid || null;

    // Load all conversations for chat window (not just unread)
    try { this.store.loadConversations(); } catch {}

    // load conversations for current user from API
    this.loadConversations();

    // subscribe to real-time incoming messages and append to open conversation
    this.hub.messageReceived.subscribe((m: any) => {
      // SignalR callbacks may run outside Angular zone — ensure UI updates by running inside zone
      this.zone.run(() => {
        try {
          // update global messages list (newest first)
          this.messages = [m, ...this.messages];
          // determine other user id
          const other = (m.senderId === this.currentUserId) ? m.receiverId : m.senderId;
          if (this.selectedUserId && (other === this.selectedUserId || m.senderId === this.selectedUserId || m.receiverId === this.selectedUserId)) {
            // append to selectedMessages (which is ordered ascending)
            const displayName = m.senderId === this.currentUserId ? 'me' : (this.selectedUserName || (this.conversations.find((c: any) => c.otherUserId === this.selectedUserId)?.otherUserName) || '');
            const msgWithName = { ...m, senderDisplayName: displayName };
            this.selectedMessages.push(msgWithName);
            // keep selectedMessages sorted ascending by sentAt
            this.selectedMessages.sort((a: any, b: any) => new Date(a.sentAt).getTime() - new Date(b.sentAt).getTime());
            // refresh conversations preview from server
            this.loadConversations();
            // scroll to bottom so new message is visible
            setTimeout(() => this.scrollToBottom(), 50);
          } else {
            // refresh conversations previews to show incoming message
            this.loadConversations();
          }
        } catch (e) { console.error('error handling incoming message', e); }
        this.cdr.detectChanges();
      });
    });

    // subscribe to read receipts
    this.hub.messageRead.subscribe((p: any) => {
      try {
        const msgId = p?.messageId;
        if (!msgId) return;
        // if the message is present in selectedMessages and was sent by current user, mark it read
        const idx = this.selectedMessages.findIndex(m => m.id === msgId);
        if (idx >= 0) {
          this.selectedMessages[idx].isRead = true;
          this.selectedMessages[idx].senderDisplayName = 'me';
          this.cdr.detectChanges();
        }
      } catch (e) { console.error('error handling messageRead', e); }
    });

    // ensure hub is started when this component is visible and user is authenticated
    this.auth.isAuthenticated$.subscribe((isAuth: boolean) => {
      if (isAuth) {
        try { this.hub.startConnection(); } catch (e) { console.error('failed to start message hub', e); }
      }
    });
  }

  private loadConversations() {
    this.api.getConversations().subscribe({
      next: (res: any) => {
        const list = Array.isArray(res?.result) ? res.result : (res?.result || []);
        this.conversations = list;
        // if no selection, pick first
        if (!this.selectedUserId && this.conversations.length) {
          this.selectedUserId = this.conversations[0].otherUserId || this.conversations[0].otherUserIdString || this.conversations[0].otherUserId;
          // load messages for selected conversation
          if (this.selectedUserId) this.selectConversation(this.selectedUserId);
        }
        this.cdr.detectChanges();
      },
      error: (e: any) => { console.error('Failed to load conversations', e); }
    });
  }

  rebuildConversations() {
    const map = new Map<string, any[]>();
    for (const m of this.messages) {
      const other = (m.senderId === this.currentUserId) ? m.receiverId : m.senderId;
      if (!map.has(other)) map.set(other, []);
      map.get(other)!.push(m);
    }
    const arr: { userId: string; messages: any[] }[] = [];
    for (const [userId, msgs] of map.entries()) {
      // sort by sentAt desc
      msgs.sort((a, b) => new Date(b.sentAt).getTime() - new Date(a.sentAt).getTime());
      arr.push({ userId, messages: msgs });
    }
    // sort conversations by latest message
    arr.sort((a, b) => new Date(b.messages[0].sentAt).getTime() - new Date(a.messages[0].sentAt).getTime());
    this.conversations = arr;
    if (!this.selectedUserId && this.conversations.length) {
      this.selectedUserId = this.conversations[0].userId;
    }
  }

  selectConversation(userId: string) {
    this.selectedUserId = userId;
    // fetch conversation from API
    this.api.getConversation(userId).subscribe({
      next: (res: any) => {
        const list = Array.isArray(res.result) ? res.result : (res || []);
        // sort ascending by sentAt so chat reads top->bottom
        list.sort((a: any, b: any) => new Date(a.sentAt).getTime() - new Date(b.sentAt).getTime());
        // annotate messages with sender display name
        const convForOther = this.conversations.find((c: any) => c.otherUserId === this.selectedUserId);
        const otherName = convForOther?.otherUserName || this.selectedUserName || '';
        this.selectedMessages = list.map((m: any) => ({
          ...m,
          id: (typeof m.id === 'string' && m.id) ? Number(m.id) : m.id,
          isRead: !!m.isRead,
          senderDisplayName: m.senderId === this.currentUserId ? 'me' : otherName
        }));
        // update conversations preview as well
        this.loadConversations();
        // set selected user name from conversations if available
        const conv = this.conversations.find((c: any) => c.otherUserId === this.selectedUserId);
        this.selectedUserName = conv?.otherUserName || this.selectedUserName || null;

        // mark unread messages in this conversation as read (only messages where current user is receiver)
        const unread = this.selectedMessages.filter(m => (m.receiverId === this.currentUserId) && !m.isRead && typeof m.id === 'number' && m.id > 0);
        for (const m of unread) {
          console.log('Marking message as read, id=', m.id);
          this.api.markAsRead(m.id).subscribe({
            next: (res: any) => {
              console.log('markAsRead success', res);
              m.isRead = true;
              // update global store so navbar/unread counters update
              try { this.store.markAsRead(m.id); } catch {}
            },
            error: (e: any) => { console.error('Failed to mark message read', e); }
          });
        }
      },
      error: (e: any) => {
        console.error('Failed to load conversation', e);
        this.selectedMessages = [];
      }
    });
  }

  startNewConversation() {
    if (!this.newReceiverId) return;
    // resolve username to id then open conversation
    const username = this.newReceiverId.trim();
    this.selectedUserName = username;
    this.newReceiverId = '';
    this.api.getUserByUserName(username).subscribe({
      next: (res: any) => {
        const id = res?.id || null;
        if (id) {
          const idStr: string = id;
          this.selectedUserId = idStr;
          this.selectConversation(idStr);
        } else {
          console.warn('User not found', username);
        }
      },
      error: (e: any) => { console.error('Failed to resolve username', e); }
    });
  }

  send() {
    if (!this.selectedUserId || !this.newMessage) return;
    // optimistic append so sender sees the message immediately
    const temp = {
      id: null,
      senderId: this.currentUserId,
      receiverId: this.selectedUserId,
      content: this.newMessage,
      sentAt: new Date().toISOString(),
      isRead: true
    } as any;
    this.selectedMessages.push(temp);
    // ensure order and scroll
    this.selectedMessages.sort((a: any, b: any) => new Date(a.sentAt).getTime() - new Date(b.sentAt).getTime());
    this.scrollToBottom();
    // keep a copy of the text in case send fails
    const sentContent = this.newMessage;
    // find receiver username from conversations list, fall back to selectedUserName (when starting a new conversation)
    const conv = this.conversations.find((c: any) => (c.otherUserId === this.selectedUserId) || (c.otherUserIdString === this.selectedUserId));
    const receiverUserName = conv?.otherUserName || conv?.otherUserNameString || this.selectedUserName || '';
    if (!receiverUserName) {
      console.warn('No receiver username available; aborting send');
      // restore input so user can retry
      this.newMessage = sentContent;
      return;
    }
    const payload = { receiverUserName, content: this.newMessage };
    this.newMessage = '';
    this.api.create(payload).subscribe({
      next: () => {
        // reload conversation from server to get canonical ids (server or hub will also push the message)
        this.selectConversation(this.selectedUserId!);
      },
      error: (e: any) => {
        console.error(e);
        // if failed, show the temp message still but mark as failed (could add UI)
        // restore input so user can retry
        this.newMessage = sentContent;
      }
    });
  }

  private scrollToBottom() {
    try {
      if (this.messagesContainer && this.messagesContainer.nativeElement) {
        const el = this.messagesContainer.nativeElement as HTMLElement;
        el.scrollTop = el.scrollHeight;
      }
    } catch {}
  }

}
