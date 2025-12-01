import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateBooking } from './create-booking';

describe('CreateBooking', () => {
  let component: CreateBooking;
  let fixture: ComponentFixture<CreateBooking>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateBooking]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CreateBooking);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
