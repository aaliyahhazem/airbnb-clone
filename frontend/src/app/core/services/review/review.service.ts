import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateReviewVM, ReviewVM } from '../../models/review.model';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ReviewService {

  private apiUrl = environment.apiUrl + '/review';

  constructor(private http: HttpClient) {}

  getReviewsByListing(listingId: number): Observable<ReviewVM[]> {
    return this.http.get<ReviewVM[]>(`${this.apiUrl}/listing/${listingId}`);
  }

  createReview(model: CreateReviewVM): Observable<any> {
    return this.http.post(`${this.apiUrl}`, model);
  }

  deleteReview(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  updateReview(id: number, model: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, model);
  }
}
