import { Component, OnInit, OnDestroy, inject, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { Subject, takeUntil } from 'rxjs';
import { RagChatService } from '../../../core/services/chat/rag-chat.service';
import { ChatMessage, ChatSession, ChatAction } from '../../../core/models/chat.model';
import { Router } from '@angular/router';

@Component({
  selector: 'app-broker-chat',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  templateUrl: './broker-chat.html',
  styleUrls: ['./broker-chat.css']
})
export class BrokerChatComponent implements OnInit, OnDestroy {
  @ViewChild('messagesContainer') messagesContainer!: ElementRef;

  private chatService = inject(RagChatService);
  private router = inject(Router);
  private destroy$ = new Subject<void>();

  isOpen = false;
  userInput = '';
  messages: ChatMessage[] = [];
  isTyping = false;
  hasError = false;

  ngOnInit(): void {
    // Subscribe to chat session
    this.chatService.chatSession$
      .pipe(takeUntil(this.destroy$))
      .subscribe(session => {
        this.messages = session.messages;
        setTimeout(() => this.scrollToBottom(), 100);
      });

    // Subscribe to typing indicator
    this.chatService.isTyping$
      .pipe(takeUntil(this.destroy$))
      .subscribe(typing => this.isTyping = typing);

    // Subscribe to chat open state
    this.chatService.chatOpen$
      .pipe(takeUntil(this.destroy$))
      .subscribe(isOpen => {
        this.isOpen = isOpen;
        if (this.isOpen && this.messages.length === 0) {
          this.showWelcomeMessage();
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  toggleChat(): void {
    this.chatService.toggleChat();
  }

  sendMessage(): void {
    const message = this.userInput.trim();
    if (!message) return;

    this.userInput = '';
    this.hasError = false;

    this.chatService.sendMessage(message)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.error) {
            this.hasError = true;
          }
        },
        error: (err) => {
          console.error('Chat error:', err);
          this.hasError = true;
        }
      });
  }

  handleAction(action: ChatAction): void {
    switch (action.type) {
      case 'search':
        this.router.navigate(['/listings'], {
          queryParams: action.data
        });
        this.isOpen = false;
        break;

      case 'book':
        // Navigate to booking with data
        if (action.data?.listingId) {
          this.router.navigate(['/listings', action.data.listingId]);
        }
        this.isOpen = false;
        break;

      case 'create_listing':
        this.router.navigate(['/host/create']);
        this.isOpen = false;
        break;

      case 'navigate':
        this.router.navigate([action.data?.path || '/']);
        this.isOpen = false;
        break;
    }
  }

  clearChat(): void {
    this.chatService.clearSession();
    this.showWelcomeMessage();
  }

  private showWelcomeMessage(): void {
    // Welcome message will be added by service if needed
  }

  private scrollToBottom(): void {
    if (this.messagesContainer) {
      const element = this.messagesContainer.nativeElement;
      element.scrollTop = element.scrollHeight;
    }
  }

  handleKeyPress(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }
}
