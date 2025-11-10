import { TestBed } from '@angular/core/testing';

import { ListingsList } from '../list/listing-list';
describe('Listings', () => {
  let service: ListingsList;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ListingsList);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
