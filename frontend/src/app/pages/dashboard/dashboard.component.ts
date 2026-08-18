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

  private timeService = inject(TimeTrackingService);

  ngOnInit() {
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
    const da = new Date();
    da.setMonth(da.getMonth() - 1);
    this.timeService.getOreByRange(da, new Date()).subscribe({
      next: (o: OraLavorata[]) => {
        this.oreUltimoMese = o.reduce((s, x) => s + x.ore, 0);
      },
    });
  }
}
