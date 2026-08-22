import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Cliente } from '../../models/models';

@Injectable({
  providedIn: 'root',
})
export class ClientiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/time-tracking';

   getClient() {
      return this.http.get<Cliente[]>(`${this.baseUrl}/clienti`);
    }
    getCliente(id: number) {
      return this.http.get<Cliente>(`${this.baseUrl}/clienti/${id}`);
    }
    addCliente(cliente: Omit<Cliente, 'id'>) {
      return this.http.post<Cliente>(`${this.baseUrl}/clienti`, cliente);
    }
    updateCliente(cliente: Cliente) {
      return this.http.put<void>(`${this.baseUrl}/clienti/${cliente.id}`, cliente);
    }
    deleteCliente(id: number) {
      return this.http.delete<void>(`${this.baseUrl}/clienti/${id}`);
    }
}
