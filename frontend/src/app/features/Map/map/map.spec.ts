import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MapService } from '../../../core/services/map/map';

describe('Map', () => {
  let component: MapService;
  let fixture: ComponentFixture<MapService>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Map]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MapService);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
