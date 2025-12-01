import { Injectable } from '@angular/core';
import { BaseService } from '../api/base.service';
import { HttpClient } from '@angular/common/http';
import { CreatePaymentIntentVm, CreatePaymentVM, CreateStripePaymentVM } from '../../models/payment';
import { Observable } from 'rxjs/internal/Observable';

@Injectable({
  providedIn: 'root',
})
export class PaymentService extends BaseService 
{
  private readonly url = `${this.apiBase}/payment`;

  constructor(http: HttpClient) {super(http);}
   initiatePayment(model: CreatePaymentVM): Observable<PaymentResponse> {
    return this.http.post<PaymentResponse>(`${this.url}/initiate`, model);
  }

  confirmPayment(body: { bookingId: number; transactionId: string }) {
    return this.http.post<PaymentResponse>(`${this.url}/confirm`, body);
  }

  createStripeIntent(model: CreateStripePaymentVM): Observable<{ success: boolean; result?: CreatePaymentIntentVm; errorMessage?: string; }> {
    return this.http.post<any>(`${this.url}/stripe/create-intent`, model);
  }

  cancelStripePayment(paymentIntentId: string) {
    return this.http.post(`${this.url}/stripe/cancel/${paymentIntentId}`, {});
  }

  refund(id: number) {
    return this.http.post(`${this.url}/${id}/refund`, {});
  }
  
}
