import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StackedCards } from './stacked-cards';

describe('StackedCards', () => {
  let component: StackedCards;
  let fixture: ComponentFixture<StackedCards>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StackedCards]
    })
    .compileComponents();

    fixture = TestBed.createComponent(StackedCards);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
