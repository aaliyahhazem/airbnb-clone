import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { NotificationDto } from '../models/notification';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class NotificationHub {
  private hubConnection!: signalR.HubConnection;
  public notificationReceived = new Subject<NotificationDto>();

  constructor(private auth: AuthService) { }

  public startConnection() {
    // Only start the hub when user is authenticated
    const isAuth = this.auth.isAuthenticated();
    console.log('NotificationHub.startConnection: isAuthenticated=', isAuth);
    if (!isAuth) {
      console.log('NotificationHub: user not authenticated — skipping start');
      return;
    }

    const payload = this.auth.getPayload() || {};
    const userID = payload['sub'] || payload['id'] || payload['nameid'] || payload['userId'] || '';
    const token = this.auth.getToken() || '';
    console.log('NotificationHub: starting for userID=', userID, ' tokenPresent=', !!token);

    // if a connection already exists, stop it first
    if (this.hubConnection) {
      try { this.hubConnection.stop(); } catch {}
      this.hubConnection = undefined as any;
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`http://localhost:5235/notificationsHub?userID=${userID}`, {
        accessTokenFactory: () => this.auth.getToken() || ''
      })
      .withAutomaticReconnect()
      .build();

    // attach handlers before starting
    this.hubConnection.on('ReceiveNotification', (notification: NotificationDto) => {
      console.log('Notification received from hub', notification);
      this.notificationReceived.next(notification);
    });

    this.hubConnection.start()
      .then(() => console.log('NotificationHub: SignalR connected'))
      .catch(err => {
        console.error('NotificationHub: SignalR connection error:', err);
        setTimeout(() => this.startConnection(), 5000);
      });
  }
}
