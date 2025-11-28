import { TestBed } from '@angular/core/testing';

import { FavoriteStoreService } from './favorite-store-service';

describe('FavoriteStoreService', () => {
  let service: FavoriteStoreService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(FavoriteStoreService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
