import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Cliente, Nota, OraLavorata, Progetto, TotaleOre } from './models';

@Injectable({
  providedIn: 'root',
})
export class TimeTrackingService {
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

  getOreLavorate() {
    return this.http.get<OraLavorata[]>(`${this.baseUrl}/ore`);
  }
  getOreByProgetto(progettoId: number) {
    return this.http.get<OraLavorata[]>(`${this.baseUrl}/ore/progetto/${progettoId}`);
  }
  getOreByRange(da: Date, a: Date) {
    return this.http.get<OraLavorata[]>(`${this.baseUrl}/ore/range`, {
      params: { da: da.toISOString(), a: a.toISOString() },
    });
  }
  getTotalOreByProgetto(progettoId: number) {
    return this.http.get<TotaleOre>(`${this.baseUrl}/ore/progetto/${progettoId}/total`);
  }
  addOraLavorata(ora: Omit<OraLavorata, 'id'>) {
    return this.http.post<OraLavorata>(`${this.baseUrl}/ore`, ora);
  }
  updateOraLavorata(ora: OraLavorata) {
    return this.http.put<void>(`${this.baseUrl}/ore/${ora.id}`, ora);
  }
  deleteOraLavorata(id: number) {
    return this.http.delete<void>(`${this.baseUrl}/ore/${id}`);
  }

  getNote() {
    return this.http.get<Nota[]>(`${this.baseUrl}/note`);
  }
  getNoteByProgetto(progettoId: number) {
    return this.http.get<Nota[]>(`${this.baseUrl}/note/progetto/${progettoId}`);
  }
  addNota(nota: Omit<Nota, 'id'>) {
    return this.http.post<Nota>(`${this.baseUrl}/note`, nota);
  }
  updateNota(nota: Nota) {
    return this.http.put<void>(`${this.baseUrl}/note/${nota.id}`, nota);
  }
  deleteNota(id: number) {
    return this.http.delete<void>(`${this.baseUrl}/note/${id}`);
  }
}
