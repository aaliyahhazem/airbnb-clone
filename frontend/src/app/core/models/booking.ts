export interface CreateBookingVM {
  listingId: number;
  checkInDate: string | Date;
  checkOutDate: string | Date;
  guests: number;
  paymentMethod: string;
}

export interface GetBookingVM {
  id: number;
  listingId: number;
  checkInDate: string | Date;
  checkOutDate: string | Date;
  totalPrice: number;
  bookingStatus: string;
  paymentStatus: string;
  clientSecret?: string;
  paymentIntentId?: string;
}

export interface BookingResponse {
  success: boolean;
  result?: GetBookingVM;
  errorMessage?: string;
}

export interface BookingsResponse {
  success: boolean;
  result?: GetBookingVM[];
  errorMessage?: string;
}