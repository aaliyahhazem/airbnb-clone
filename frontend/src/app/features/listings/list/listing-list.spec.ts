import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ListingList } from './listing-list';

describe('ListingList', () => {
  let component: ListingList;
  let fixture: ComponentFixture<ListingList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ListingList]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ListingList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
