import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ListingCard } from './listing-card';

describe('Properties', () => {
  let component: ListingCard;
  let fixture: ComponentFixture<ListingCard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ListingCard]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ListingCard);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
