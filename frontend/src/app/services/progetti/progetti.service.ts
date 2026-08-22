import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Progetto } from '../../models/models';


@Injectable({
  providedIn: 'root',
})
export class ProgettiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/time-tracking';

  getProgetti() {
      return this.http.get<Progetto[]>(`${this.baseUrl}/progetti`);
    }
    getProgettiByCliente(clienteId: number) {
      return this.http.get<Progetto[]>(`${this.baseUrl}/progetti/cliente/${clienteId}`);
    }
    getProgetto(id: number) {
      return this.http.get<Progetto>(`${this.baseUrl}/progetti/${id}`);
    }
    addProgetto(progetto: Omit<Progetto, 'id'>) {
      return this.http.post<Progetto>(`${this.baseUrl}/progetti`, progetto);
    }
    updateProgetto(progetto: Progetto) {
      return this.http.put<void>(`${this.baseUrl}/progetti/${progetto.id}`, progetto);
    }
    deleteProgetto(id: number) {
      return this.http.delete<void>(`${this.baseUrl}/progetti/${id}`);
    }

}
