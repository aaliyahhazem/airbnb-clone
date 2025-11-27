import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BaseService } from './base.service';
import { MessageDto } from '../../models/message';

@Injectable({ providedIn: 'root' })
export class MessageService extends BaseService {
  private readonly url = `${this.apiBase}/message`;

  constructor(http: HttpClient) { super(http); }

  // backend provides a conversations endpoint summarizing recent chats; use it for navbar initial load
  getForCurrentUser() { return this.getConversations(); }
  getConversations() { return this.http.get<any>(`${this.url}/conversations`); }
  getById(id: number) { return this.http.get<any>(`${this.url}/${id}`); }
  getUserByUserName(userName: string) { return this.http.get<any>(`${this.url}/userbyname/${encodeURIComponent(userName)}`); }
  // create expects receiverUserName and content
  create(model: { receiverUserName: string; content: string }) { return this.http.post<any>(this.url, model); }
  getConversation(otherUserId: string) { return this.http.get<any>(`${this.url}/conversation/${otherUserId}`); }
  getPaged(page = 1, pageSize = 20) { return this.http.get<any>(`${this.url}/paged`, { params: this.buildParams({ page, pageSize }) }); }
  getUnread() { return this.http.get<any>(`${this.url}/unread`); }
  markAsRead(id: number) { return this.http.put(`${this.url}/${id}/read`, {}); }
}
