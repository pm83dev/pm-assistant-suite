import { Component, effect, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DashboardManagementService } from '../../services/dashboard/dashboard-management.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent {
  dashboard = inject(DashboardManagementService);

  constructor() {
    effect(() => {
      this.dashboard.totalClienti();
      this.dashboard.totalProgetti();
      this.dashboard.totalOre();
      this.dashboard.oreUltimoMese();
      this.dashboard.meseCorrente();
      this.dashboard.progettiConTotali();
    });
  }

  cambiaMese(delta: number): void {
    this.dashboard.cambiaMese(delta);
  }
}
