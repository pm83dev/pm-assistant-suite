import { Injectable, inject, signal } from '@angular/core';
import { forkJoin, tap } from 'rxjs';
import { ClientiService } from '../clienti/clienti.service';
import { ProgettiService } from '../progetti/progetti.service';
import { OreService } from '../ore/ore.service';

interface ProgettoConOre {
  progetto: { id: number; nome: string; clienteId: number; clienteNome?: string };
  totaleOre: number;
}

@Injectable({
  providedIn: 'root',
})
export class DashboardManagementService {
  private clientiService = inject(ClientiService);
  private progettiService = inject(ProgettiService);
  private oreService = inject(OreService);

  #totalClienti = signal<number>(0);
  #totalProgetti = signal<number>(0);
  #totalOre = signal<number>(0);
  #oreUltimoMese = signal<number>(0);
  #meseCorrente = signal<string>('');
  #progettiConTotali = signal<ProgettoConOre[]>([]);

  totalClienti = this.#totalClienti.asReadonly();
  totalProgetti = this.#totalProgetti.asReadonly();
  totalOre = this.#totalOre.asReadonly();
  oreUltimoMese = this.#oreUltimoMese.asReadonly();
  meseCorrente = this.#meseCorrente.asReadonly();
  progettiConTotali = this.#progettiConTotali.asReadonly();

  constructor() {
    this.loadAll();
  }

  loadAll(): void {
    forkJoin({
      clienti: this.clientiService.getClient(),
      progetti: this.progettiService.getProgetti(),
      ore: this.oreService.getOreLavorate(),
      riepilogo: this.oreService.getRiepilogoProgetti(),
    }).subscribe({
      next: ({ clienti, progetti, ore, riepilogo }) => {
        this.#totalClienti.set(clienti.length);
        this.#totalProgetti.set(progetti.length);
        this.#totalOre.set(ore.reduce((s, x) => s + x.ore, 0));
        this.#progettiConTotali.set(
          riepilogo.map((item) => ({
            progetto: {
              id: item.progettoId,
              nome: item.progettoNome,
              clienteId: item.clienteId,
              clienteNome: item.clienteNome,
            },
            totaleOre: item.totaleOre,
          }))
        );
      },
    });

    this.updateMeseOre(new Date());
  }

  cambiaMese(delta: number): void {
    const d = new Date();
    d.setMonth(d.getMonth() + delta);
    this.updateMeseOre(d);
  }

  private updateMeseOre(dataRiferimento: Date): void {
    const anno = dataRiferimento.getFullYear();
    const mese = dataRiferimento.getMonth();
    const da = new Date(anno, mese, 1);
    const a = new Date(anno, mese + 1, 0, 23, 59, 59);
    const meseCorrente = da.toLocaleDateString('it-IT', { month: 'long', year: 'numeric' });
    this.#meseCorrente.set(meseCorrente);

    this.oreService.getOreByRange(da, a).subscribe({
      next: (ore) => this.#oreUltimoMese.set(ore.reduce((s, x) => s + x.ore, 0)),
    });
  }
}
