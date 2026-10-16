import { Component, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  DashboardManagementService,
  PuntoGrafico,
} from '../../services/dashboard/dashboard-management.service';
import { ExcelExportService, RigaRiepilogoProgetto } from '../../services/export/excel-export.service';
import { OraLavorata } from '../../models/models';

interface ProgettoRiga {
  progetto: { id: number; nome: string; clienteId: number; clienteNome?: string };
  totaleOre: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent {
  dashboard = inject(DashboardManagementService);
  private excelExport = inject(ExcelExportService);
  loading = false;

  /** Id del progetto con dettaglio espanso (null = nessuno). */
  progettoAperto = signal<number | null>(null);

  constructor() {
    effect(() => {
      this.dashboard.totalClienti();
      this.dashboard.totalProgetti();
      this.dashboard.totalOre();
      this.dashboard.oreUltimoMese();
      this.dashboard.meseCorrente();
      this.dashboard.progettiConTotali();
      this.dashboard.orePerGiorno();
      this.dashboard.orePerProgettoGrafico();
      this.loading = this.dashboard.loading();
    });
  }

  cambiaMese(delta: number): void {
    this.dashboard.cambiaMese(delta);
  }

  /** Apre/chiude il dettaglio di un progetto. */
  toggleProgetto(progettoId: number): void {
    this.progettoAperto.set(this.progettoAperto() === progettoId ? null : progettoId);
  }

  /** Ore registrate per un progetto (dal cache del servizio). */
  oreProgetto(progettoId: number): OraLavorata[] {
    return this.dashboard.orePerProgetto().get(progettoId) ?? [];
  }

  /** Righe di dettaglio per progetto: aggrega le ore per giorno. */
  righeProgetto(progettoId: number): RigaRiepilogoProgetto[] {
    const perGiorno = new Map<string, { ore: number; attivita: string[] }>();
    for (const o of this.oreProgetto(progettoId)) {
      const chiave = o.data.slice(0, 10);
      const esistente = perGiorno.get(chiave) ?? { ore: 0, attivita: [] };
      esistente.ore += o.ore;
      if (o.descrizione && !esistente.attivita.includes(o.descrizione)) {
        esistente.attivita.push(o.descrizione);
      }
      perGiorno.set(chiave, esistente);
    }
    return Array.from(perGiorno.entries())
      .sort((a, b) => a[0].localeCompare(b[0]))
      .map(([data, v]) => ({
        data: new Date(data + 'T00:00:00').toLocaleDateString('it-IT'),
        ore: v.ore,
        attivita: v.attivita.length > 0 ? v.attivita.join('; ') : '-',
      }));
  }

  /** Altezza (in %) della barra nel grafico a barre. */
  altezzaBarra(valore: number, punti: PuntoGrafico[]): number {
    const max = Math.max(...punti.map((p) => p.valore), 0.001);
    return Math.max((valore / max) * 100, valore > 0 ? 2 : 0);
  }

  /** Larghezza (in %) della barra di progresso per progetto. */
  percentualeProgetto(valore: number, punti: PuntoGrafico[]): number {
    const max = Math.max(...punti.map((p) => p.valore), 0.001);
    return (valore / max) * 100;
  }

  /** Esporta in Excel il riepilogo di un progetto. */
  esportaProgetto(item: ProgettoRiga, event: Event): void {
    event.stopPropagation();
    this.excelExport.exportProgetto(
      item.progetto.nome,
      item.progetto.clienteNome,
      this.oreProgetto(item.progetto.id)
    );
  }
}
