import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Cliente, OraLavorata, Progetto } from '../../models/models';
import { ClientiService } from '../../services/clienti/clienti.service';
import { OreService } from '../../services/ore/ore.service';
import { ProgettiService } from '../../services/progetti/progetti.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  totalClienti: number = 0;
  totalProgetti: number = 0;
  totalOre: number = 0;
  oreUltimoMese: number = 0;
  meseCorrente: string = '';
  dataRiferimento: Date = new Date();
  progettiConTotali: { progetto: Progetto; totaleOre: number }[] = [];

  private projectService = inject(ProgettiService);
  private clientService = inject(ClientiService);
  private oreService = inject(OreService);

  ngOnInit() {
    this.loadStats();
    this.getProgettiConOreTotali();
  }

  cambiaMese(delta: number): void {
    this.dataRiferimento.setMonth(this.dataRiferimento.getMonth() + delta);
    this.loadStats();
  }

  getProgettiConOreTotali(): void {
    this.oreService.getRiepilogoProgetti().subscribe({
      next: (data: any[]) => {
        this.progettiConTotali = data.map((item) => ({
          progetto: {
            id: item.progettoId,
            nome: item.progettoNome,
            clienteId: item.clienteId,
            clienteNome: item.clienteNome,
          },
          totaleOre: item.totaleOre,
        }));
        this.totalProgetti = data.length;
      },
    });
  }

  loadStats(): void {
    this.clientService
      .getClient()
      .subscribe({ next: (c: Cliente[]) => (this.totalClienti = c.length) });
    this.projectService.getProgetti().subscribe({
      next: (p: Progetto[]) => {
        this.totalProgetti = p.length;
      },
    });
    this.oreService.getOreLavorate().subscribe({
      next: (o: OraLavorata[]) => {
        this.totalOre = o.reduce((s, x) => s + x.ore, 0);
      },
    });

    const anno = this.dataRiferimento.getFullYear();
    const mese = this.dataRiferimento.getMonth();
    const da = new Date(anno, mese, 1);
    const a = new Date(anno, mese + 1, 0, 23, 59, 59);
    this.meseCorrente = da.toLocaleDateString('it-IT', { month: 'long', year: 'numeric' });

    this.oreService.getOreByRange(da, a).subscribe({
      next: (o: OraLavorata[]) => {
        this.oreUltimoMese = o.reduce((s, x) => s + x.ore, 0);
      },
    });
  }
}
