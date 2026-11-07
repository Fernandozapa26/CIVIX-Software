import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Materiales } from './materiales';

describe('Materiales', () => {
  let component: Materiales;
  let fixture: ComponentFixture<Materiales>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Materiales],
      // Materiales usa MaterialesService, que necesita HttpClient.
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(Materiales);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
