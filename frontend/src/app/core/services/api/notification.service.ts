import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseService } from './base.service';
import { NotificationDto } from '../../models/notification';

@Injectable({ providedIn: 'root' })
export class NotificationService extends BaseService {
  private readonly url = `${this.apiBase}/notification`;

  constructor(http: HttpClient) { super(http); }

  getAll(): Observable<any> { return this.http.get<any>(this.url); }
  getForCurrentUser(): Observable<any> { return this.http.get<any>(`${this.url}/user`); }
  getById(id: number): Observable<any> { return this.http.get<any>(`${this.url}/${id}`); }
  create(model: Partial<NotificationDto>): Observable<any> { return this.http.post<any>(this.url, model); }
  getPaged(page = 1, pageSize = 10) { return this.http.get<any>(`${this.url}/paged`, { params: this.buildParams({ page, pageSize }) }); }
  getUnread() { return this.http.get<any>(`${this.url}/unread`); }
  markAsRead(id: number) { return this.http.put(`${this.url}/${id}/read`, {}); }
  markAllAsRead() { return this.http.put(`${this.url}/read-all`, {}); }
  sendPending() { return this.http.post(`${this.url}/send-pending`, {}); }
}
