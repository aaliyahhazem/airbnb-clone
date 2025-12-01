import { TestBed } from '@angular/core/testing';

import { BookingStoreService } from './booking-store-service';

describe('BookingStoreService', () => {
  let service: BookingStoreService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(BookingStoreService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
