import { inject, Injectable, signal } from '@angular/core';
import { NoteService } from './note.service';
import { ProgettiService } from '../progetti/progetti.service';
import { ClientiService } from '../clienti/clienti.service';
import { Cliente, Nota, Progetto } from '../../models/models';
import { forkJoin, tap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class NoteManagementService {

  private noteService = inject(NoteService);
  private projectService = inject(ProgettiService);
  private clientService = inject(ClientiService);

  #note = signal<Nota[]>([]);
  #progetti = signal<Progetto[]>([]);
  #clienti = signal<Cliente[]>([]);
  #loading = signal<boolean>(false);

  note = this.#note.asReadonly();
  progetti = this.#progetti.asReadonly();
  clienti = this.#clienti.asReadonly();
  loading = this.#loading.asReadonly();

  constructor() {
    this.initDataLoading();
  }

  private initDataLoading(): void {
    this.#loading.set(true);
    forkJoin({
      note: this.noteService.getNote(),
      progetti: this.projectService.getProgetti(),
      clienti: this.clientService.getClient(),
    })
      .subscribe({
        next: ({ note, progetti, clienti }) => {
          this.#note.set(note);
          this.#progetti.set(progetti);
          this.#clienti.set(clienti);
        },
        error: (err) => {
          console.error('Errore nel caricamento note:', err);
        },
      })
      .add(() => {
        this.#loading.set(false);
      });
  }

  addNota(nota: Omit<Nota, 'id'>) {
    return this.noteService
      .addNota(nota)
      .pipe(tap((created) => this.#note.update((list) => [...list, created])));
  }

  updateNota(nota: Nota) {
    return this.noteService.updateNota(nota).pipe(
      tap(() =>
        this.#note.update((list) => list.map((n) => (n.id === nota.id ? nota : n))),
      ),
    );
  }

  deleteNota(id: number) {
    return this.noteService
      .deleteNota(id)
      .pipe(tap(() => this.#note.update((list) => list.filter((n) => n.id !== id))));
  }

  getProgettoNome(progettoId: number): string {
    const p = this.#progetti().find((p) => p.id === progettoId);
    return p ? p.nome : 'N/D';
  }

  getClienteNomeDaProgetto(progettoId: number): string {
    const p = this.#progetti().find((p) => p.id === progettoId);
    if (!p) return 'N/D';
    const c = this.#clienti().find((c) => c.id === p.clienteId);
    return c ? c.nome : 'N/D';
  }
}
