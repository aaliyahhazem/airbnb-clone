import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { MessageDto } from '../models/message';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class MessageHub {
  private hubConnection!: signalR.HubConnection;
  public messageReceived = new Subject<MessageDto>();
  public messageRead = new Subject<{ messageId: number; readerId: string }>();

  constructor(private auth: AuthService) { }

  public startConnection() {
    const isAuth = this.auth.isAuthenticated();
    console.log('MessageHub.startConnection: isAuthenticated=', isAuth);
    if (!isAuth) {
      console.log('MessageHub: user not authenticated — skipping start');
      return;
    }

    const payload = this.auth.getPayload() || {};
    const userID = payload['sub'] || payload['id'] || payload['nameid'] || payload['userId'] || '';
    const token = this.auth.getToken() || '';
    console.log('MessageHub: starting for userID=', userID, ' tokenPresent=', !!token);

    if (this.hubConnection) {
      try { this.hubConnection.stop(); } catch {}
      this.hubConnection = undefined as any;
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`http://localhost:5235/messagesHub?userID=${userID}`, {
        accessTokenFactory: () => this.auth.getToken() || ''
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveMessage', (message: MessageDto) => {
      console.log('Message received from hub', message);
      this.messageReceived.next(message);
    });
    this.hubConnection.on('MessageRead', (payload: any) => {
      console.log('MessageRead event from hub', payload);
      this.messageRead.next({ messageId: payload.messageId, readerId: String(payload.readerId) });
    });

    this.hubConnection.start()
      .then(() => console.log('MessageHub connected'))
      .catch(err => {
        console.error('MessageHub connection error:', err);
        setTimeout(() => this.startConnection(), 5000);
      });
  }
}
