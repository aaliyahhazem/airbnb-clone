import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HomeListingCard } from './home-listing-card';

describe('HomeListingCard', () => {
  let component: HomeListingCard;
  let fixture: ComponentFixture<HomeListingCard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HomeListingCard]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HomeListingCard);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
