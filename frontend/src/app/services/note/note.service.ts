import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Nota } from '../../models/models';

@Injectable({
  providedIn: 'root',
})
export class NoteService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/time-tracking';

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
