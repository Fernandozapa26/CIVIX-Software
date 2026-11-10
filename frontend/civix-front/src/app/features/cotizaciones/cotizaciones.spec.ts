import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { Cotizaciones } from './cotizaciones';

describe('Cotizaciones', () => {
  let component: Cotizaciones;
  let fixture: ComponentFixture<Cotizaciones>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Cotizaciones],
      // Usa CotizacionesService y ObrasService (HttpClient) y RouterLink.
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(Cotizaciones);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
