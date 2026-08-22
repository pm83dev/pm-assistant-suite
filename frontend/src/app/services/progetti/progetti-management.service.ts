import { inject, Injectable, signal } from "@angular/core";
import { ProgettiService } from "./progetti.service";
import { ClientiService } from "../clienti/clienti.service";
import { Cliente, Progetto } from "../../models/models";
import { forkJoin, tap } from "rxjs";

@Injectable({
  providedIn: 'root',
})
export class ProjectManagementService {

  private projectService = inject(ProgettiService);
  private clientService = inject(ClientiService);

   // 1. STATO PRIVATO (I veri contenitori dei dati)
  #progetti = signal<Progetto[]>([]);
  #clienti = signal<Cliente[]>([]);
  #loading = signal<boolean>(false);

  // 2. STATO PUBBLICO (Solo lettura per i componenti)
  progetti = this.#progetti.asReadonly();
  clienti = this.#clienti.asReadonly();
  loading = this.#loading.asReadonly();

  constructor(){
    this.initDataLoading();
  }
 private initDataLoading(): void {
     this.#loading.set(true);
     forkJoin({
       progetti: this.projectService.getProgetti(),
       clienti: this.clientService.getClient(),
     })
       .subscribe({
         next: ({progetti, clienti }) => {
           this.#progetti.set(progetti);
           this.#clienti.set(clienti);
         },
         error: (err) => {
           console.error('Errore nel caricamento:', err);
         },
       })
       .add(() => {
         this.#loading.set(false);
       });
   }

  // Actions: tengono #progetti sincronizzato dopo ogni chiamata HTTP andata a buon fine
  addProgetto(progetto: Omit<Progetto, 'id'>) {
    return this.projectService
      .addProgetto(progetto)
      .pipe(tap((created) => this.#progetti.update((list) => [...list, created])));
  }

  updateProgetto(progetto: Progetto) {
    return this.projectService.updateProgetto(progetto).pipe(
      tap(() =>
        this.#progetti.update((list) => list.map((p) => (p.id === progetto.id ? progetto : p))),
      ),
    );
  }

  deleteProgetto(id: number) {
    return this.projectService
      .deleteProgetto(id)
      .pipe(tap(() => this.#progetti.update((list) => list.filter((p) => p.id !== id))));
  }

}
