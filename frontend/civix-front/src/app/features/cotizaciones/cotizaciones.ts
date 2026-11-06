import { Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

interface Cotizacion {
  id: number;
  cliente: string;
  descripcion: string;
  fecha: string;
  total: number;
  estado: string;
}

@Component({
  selector: 'app-cotizaciones',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './cotizaciones.html',
  styleUrl: './cotizaciones.scss'
})
export class Cotizaciones {

  readonly cotizaciones = signal<Cotizacion[]>([]);

}