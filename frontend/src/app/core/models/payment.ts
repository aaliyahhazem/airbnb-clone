export interface CreatePaymentVM {
  bookingId: number;
  amount: number;
  paymentMethod: string;
}

export interface CreateStripePaymentVM {
  bookingId: number;
  amount: number;
  currency: string;
  description?: string;
}

export interface CreatePaymentIntentVm {
  clientSecret: string;
  paymentIntentId: string;
  amount: number;
  currency: string;
}

export interface PaymentResponse {
  success: boolean;
  result?: any;
  errorMessage?: string;
}
