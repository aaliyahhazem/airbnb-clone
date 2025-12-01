import { Injectable } from '@angular/core';
import { BaseService } from '../api/base.service';
import { HttpClient } from '@angular/common/http';
import { BookingResponse, BookingsResponse, CreateBookingVM } from '../../models/booking';
import { Observable } from 'rxjs/internal/Observable';

@Injectable({
  providedIn: 'root',
})
export class BookingService extends BaseService {
  private readonly url = `${this.apiBase}/booking`;

  constructor(http: HttpClient) {super(http);}

  createBooking(model: CreateBookingVM): Observable<BookingResponse> {
    return this.http.post<BookingResponse>(this.url, model);
  }
   getMyBookings(): Observable<BookingsResponse> {
    return this.http.get<BookingsResponse>(`${this.url}/me`);
  }

  // Get booking by id (for payment page deep-links)
  getById(id: number): Observable<BookingResponse> {
    return this.http.get<BookingResponse>(`${this.url}/${id}`);
  }

  getHostBookings(): Observable<BookingsResponse> {
    return this.http.get<BookingsResponse>(`${this.url}/host/me`);
  }

  cancelBooking(id: number) {
    return this.http.post<BookingResponse>(`${this.url}/${id}/cancel`, {});
  }

  
}
