import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { OraLavorata, TotaleOre } from '../../models/models';

@Injectable({
  providedIn: 'root',
})
export class OreService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/time-tracking';

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
  getRiepilogoProgetti() {
    return this.http.get<any[]>(`${this.baseUrl}/ore/riepilogo-progetti`);
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
}
