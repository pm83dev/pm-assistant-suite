import { Injectable, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { ClientiService } from '../clienti/clienti.service';
import { ProgettiService } from '../progetti/progetti.service';
import { OreService } from '../ore/ore.service';
import { withRetry } from '../http/retry';
import { OraLavorata } from '../../models/models';

interface ProgettoConOre {
  progetto: { id: number; nome: string; clienteId: number; clienteNome?: string };
  totaleOre: number;
}

export interface PuntoGrafico {
  label: string;
  valore: number;
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
  #orePerProgetto = signal<Map<number, OraLavorata[]>>(new Map());
  #orePerGiorno = signal<PuntoGrafico[]>([]);
  #orePerProgettoGrafico = signal<PuntoGrafico[]>([]);
  #loading = signal<boolean>(false);

  totalClienti = this.#totalClienti.asReadonly();
  totalProgetti = this.#totalProgetti.asReadonly();
  totalOre = this.#totalOre.asReadonly();
  oreUltimoMese = this.#oreUltimoMese.asReadonly();
  meseCorrente = this.#meseCorrente.asReadonly();
  progettiConTotali = this.#progettiConTotali.asReadonly();
  orePerProgetto = this.#orePerProgetto.asReadonly();
  orePerGiorno = this.#orePerGiorno.asReadonly();
  orePerProgettoGrafico = this.#orePerProgettoGrafico.asReadonly();
  loading = this.#loading.asReadonly();

  constructor() {
    this.loadData();
  }

  /**
   * (R)carica clienti, progetti, ore e riepilogo dal backend. Chiamata
   * all'avvio e ad ogni cambio di pagina (vedi app.config.ts) per garantire
   * dati freschi.
   */
  loadData(): void {
    this.#loading.set(true);
    withRetry(
      forkJoin({
        clienti: this.clientiService.getClient(),
        progetti: this.progettiService.getProgetti(),
        ore: this.oreService.getOreLavorate(),
        riepilogo: this.oreService.getRiepilogoProgetti(),
      })
    ).subscribe({
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
        this.#orePerProgetto.set(this.aggregaOrePerProgetto(ore));
        this.#orePerGiorno.set(this.calcolaOrePerGiorno(ore, 14));
        this.#orePerProgettoGrafico.set(
          riepilogo
            .map((item) => ({ label: item.progettoNome, valore: item.totaleOre }))
            .sort((a, b) => b.valore - a.valore)
        );
      },
      error: (err) => {
        console.error('Errore nel caricamento dashboard:', err);
      },
    }).add(() => {
      this.#loading.set(false);
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

  /** Raggruppa le ore per progettoId, ordinando per data crescente. */
  private aggregaOrePerProgetto(ore: OraLavorata[]): Map<number, OraLavorata[]> {
    const mappa = new Map<number, OraLavorata[]>();
    for (const o of ore) {
      const lista = mappa.get(o.progettoId) ?? [];
      lista.push(o);
      mappa.set(o.progettoId, lista);
    }
    for (const lista of mappa.values()) {
      lista.sort((a, b) => a.data.localeCompare(b.data));
    }
    return mappa;
  }

  /**
   * Aggrega le ore per giorno (yyyy-MM-dd) e restituisce gli ultimi
   * `numGiorni` giorni con ore, inclusi i giorni senza attività (valore 0).
   */
  private calcolaOrePerGiorno(ore: OraLavorata[], numGiorni: number): PuntoGrafico[] {
    const perGiorno = new Map<string, number>();
    for (const o of ore) {
      const chiave = o.data.slice(0, 10);
      perGiorno.set(chiave, (perGiorno.get(chiave) ?? 0) + o.ore);
    }

    const oggi = new Date();
    const punti: PuntoGrafico[] = [];
    for (let i = numGiorni - 1; i >= 0; i--) {
      const d = new Date(oggi.getFullYear(), oggi.getMonth(), oggi.getDate() - i);
      const chiave = d.toISOString().slice(0, 10);
      punti.push({
        label: d.toLocaleDateString('it-IT', { day: '2-digit', month: '2-digit' }),
        valore: perGiorno.get(chiave) ?? 0,
      });
    }
    return punti;
  }
}
