import { Injectable } from '@angular/core';
import { BaseService } from '../api/base.service';
import { HttpClient } from '@angular/common/http';
import { BookingResponse, BookingsResponse, CreateBookingVM, GetBookingVM } from '../../models/booking';
import { Observable } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { of } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class BookingService extends BaseService {
  private readonly url = `${this.apiBase}/booking`;


  constructor(http: HttpClient) {
    super(http);
  }

  createBooking(model: CreateBookingVM): Observable<BookingResponse> {
    return this.http.post<any>(this.url, model).pipe(
      map(response => {
        return {
          success: response?.success ?? false,
          result: response?.result || response?.data || null,
          errorMessage: response?.errorMessage || response?.message || null
        } as BookingResponse;
      }),
      catchError(error => {
        console.error('Booking creation error:', error);
        return of({
          success: false,
          result: null,
          errorMessage: error?.error?.errorMessage || error?.message || 'Failed to create booking'
        } as BookingResponse);
      })
    );
  }

  getById(id: number): Observable<BookingResponse> {
    return this.http.get<any>(`${this.url}/${id}`).pipe(
      map(response => {

        return {
          success: response?.success ?? true,
          result: response?.result || response?.data || response,
          errorMessage: response?.errorMessage || response?.message || null
        } as BookingResponse;
      }),
      catchError(error => {
        console.error('? GetById error:', error);
        return of({
          success: false,
          result: null,
          errorMessage: error?.error?.errorMessage || error?.message || 'Failed to load booking'
        } as BookingResponse);
      })
    );
  }

  getMyBookings(): Observable<BookingsResponse> {
    return this.http.get<any>(`${this.url}/me`).pipe(
      map(response => ({
        success: response?.success ?? true,
        result: Array.isArray(response)
          ? response
          : response?.result || response?.data || [],
        errorMessage: response?.errorMessage || null
      } as BookingsResponse)),
      catchError(error => of({
        success: false,
        result: [],
        errorMessage: error?.error?.errorMessage || 'Failed to load bookings'
      } as BookingsResponse))
    );
  }

  getHostBookings(): Observable<BookingsResponse> {
    return this.http.get<any>(`${this.url}/host/me`).pipe(
      map(response => ({
        success: response?.success ?? true,
        result: Array.isArray(response)
          ? response
          : response?.result || response?.data || [],
        errorMessage: response?.errorMessage || null
      } as BookingsResponse)),
      catchError(error => of({
        success: false,
        result: [],
        errorMessage: error?.error?.errorMessage || 'Failed to load bookings'
      } as BookingsResponse))
    );
  }

  cancelBooking(id: number): Observable<BookingResponse> {
    return this.http.post<any>(`${this.url}/${id}/cancel`, {}).pipe(
      map(response => ({
        success: response?.success ?? true,
        result: response?.result || response,
        errorMessage: response?.errorMessage || null
      } as BookingResponse)),
      catchError(error => of({
        success: false,
        result: null,
        errorMessage: error?.error?.errorMessage || 'Failed to cancel booking'
      } as BookingResponse))
    );
  }
}