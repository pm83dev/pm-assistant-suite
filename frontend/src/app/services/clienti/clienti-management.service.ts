import { inject, Injectable, signal } from '@angular/core';
import { tap } from 'rxjs';
import { Cliente } from '../../models/models';
import { ClientiService } from './clienti.service';

@Injectable({
  providedIn: 'root',
})
export class ClienteManagementService {
  private clientiService = inject(ClientiService);

  // 1. STATO PRIVATO (I veri contenitori dei dati)
  #clienti = signal<Cliente[]>([]);
  #loading = signal<boolean>(false);

  // 2. STATO PUBBLICO (Solo lettura per i componenti)
  clienti = this.#clienti.asReadonly();
  loading = this.#loading.asReadonly();

  constructor() {
    this.loadClienti();
  }

  private loadClienti(): void {
    this.#loading.set(true);
    this.clientiService.getClient().subscribe({
      next: (data) => this.#clienti.set(data),
      error: (err) => console.error('Errore nel caricamento clienti:', err),
      complete: () => this.#loading.set(false),
    });
  }

  // Actions: tengono #clienti sincronizzato dopo ogni chiamata HTTP andata a buon fine
  addCliente(cliente: Omit<Cliente, 'id'>) {
    return this.clientiService
      .addCliente(cliente)
      .pipe(tap((created) => this.#clienti.update((list) => [...list, created])));
  }

  updateCliente(cliente: Cliente) {
    return this.clientiService
      .updateCliente(cliente)
      .pipe(
        tap(() =>
          this.#clienti.update((list) => list.map((c) => (c.id === cliente.id ? cliente : c))),
        ),
      );
  }

  deleteCliente(id: number) {
    return this.clientiService
      .deleteCliente(id)
      .pipe(tap(() => this.#clienti.update((list) => list.filter((c) => c.id !== id))));
  }
}
