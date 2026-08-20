import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Cliente, OraLavorata, Progetto } from '../../models';
import { TimeTrackingService } from '../../time-tracking.service';

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

  private timeService = inject(TimeTrackingService);

  ngOnInit() {
    this.loadStats();
  }

  cambiaMese(delta: number): void {
    this.dataRiferimento.setMonth(this.dataRiferimento.getMonth() + delta);
    this.loadStats();
  }

  loadStats(): void {
    this.timeService
      .getClient()
      .subscribe({ next: (c: Cliente[]) => (this.totalClienti = c.length) });
    this.timeService.getProgetti().subscribe({
      next: (p: Progetto[]) => {
        this.totalProgetti = p.length;
      },
    });
    this.timeService.getOreLavorate().subscribe({
      next: (o: OraLavorata[]) => {
        this.totalOre = o.reduce((s, x) => s + x.ore, 0);
      },
    });

    const anno = this.dataRiferimento.getFullYear();
    const mese = this.dataRiferimento.getMonth();
    const da = new Date(anno, mese, 1);
    const a = new Date(anno, mese + 1, 0, 23, 59, 59);
    this.meseCorrente = da.toLocaleDateString('it-IT', { month: 'long', year: 'numeric' });

    this.timeService.getOreByRange(da, a).subscribe({
      next: (o: OraLavorata[]) => {
        this.oreUltimoMese = o.reduce((s, x) => s + x.ore, 0);
      },
    });
  }
}
