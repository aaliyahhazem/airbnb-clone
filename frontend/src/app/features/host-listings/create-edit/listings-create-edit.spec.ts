import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ListingsCreateEdit } from './listings-create-edit';

describe('ListingsCreateEdit', () => {
  let component: ListingsCreateEdit;
  let fixture: ComponentFixture<ListingsCreateEdit>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ListingsCreateEdit]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ListingsCreateEdit);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
