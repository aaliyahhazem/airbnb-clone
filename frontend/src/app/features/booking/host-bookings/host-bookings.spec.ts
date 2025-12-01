import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HostBookings } from './host-bookings';

describe('HostBookings', () => {
  let component: HostBookings;
  let fixture: ComponentFixture<HostBookings>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostBookings]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HostBookings);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
