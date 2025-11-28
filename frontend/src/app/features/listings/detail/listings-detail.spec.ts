import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ListingsDetail } from './listings-detail';

describe('ListingsDetail', () => {
  let component: ListingsDetail;
  let fixture: ComponentFixture<ListingsDetail>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ListingsDetail]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ListingsDetail);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
