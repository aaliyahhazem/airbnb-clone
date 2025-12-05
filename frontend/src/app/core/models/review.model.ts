export interface CreateReviewVM {
  bookingId: number;
  rating: number;
  comment: string;
}

export interface ReviewVM {
  id: number;
  bookingId: number;
  guestId: string;
  rating: number;
  comment: string;
  createdAt: string;
}
