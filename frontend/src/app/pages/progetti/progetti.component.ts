import { CommonModule } from '@angular/common';
import { Component, effect, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Cliente, Progetto } from '../../models/models';
import { getBadgeBorder as _getBd, getBadgeBackground as _getBg } from '../../utils/badge-color';
import { ProjectManagementService } from '../../services/progetti/progetti-management.service';

@Component({
  selector: 'app-progetti',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './progetti.component.html',
  styleUrls: ['./progetti.component.css'],
})
export class ProgettiComponent {
  progetti: Progetto[] = [];
  clienti: Cliente[] = [];
  selectedProgetto: Progetto | null = null;
  showForm = false;
  editMode = false;

  form = {
    nome: '',
    descrizione: '',
    clienteId: 0,
  };

  projManagementService = inject(ProjectManagementService);

  constructor() {
    effect(() => {
      this.progetti = this.projManagementService.progetti();
      this.clienti = this.projManagementService.clienti();
    });
  }

  openNewForm(): void {
    this.editMode = false;
    this.form = { nome: '', descrizione: '', clienteId: this.clienti[0]?.id || 0 };
    this.showForm = true;
  }

  openEditForm(progetto: Progetto): void {
    this.editMode = true;
    this.selectedProgetto = progetto;
    this.form = {
      nome: progetto.nome,
      descrizione: progetto.descrizione || '',
      clienteId: progetto.clienteId,
    };
    this.showForm = true;
  }

  save(): void {
    if (!this.form.nome.trim() || this.form.clienteId === 0) return;

    if (this.editMode && this.selectedProgetto) {
      const updated: Progetto = {
        ...this.selectedProgetto,
        nome: this.form.nome,
        descrizione: this.form.descrizione || undefined,
        clienteId: this.form.clienteId,
      };
      this.projManagementService.updateProgetto(updated).subscribe({
        next: () => this.closeForm(),
        error: (err) => console.error('Errore nel salvataggio:', err),
      });
    } else {
      this.projManagementService.addProgetto(this.form).subscribe({
        next: () => this.closeForm(),
        error: (err) => console.error('Errore nel salvataggio:', err),
      });
    }
  }

  delete(id: number): void {
    if (confirm('Sei sicuro di voler eliminare questo progetto?')) {
      this.projManagementService.deleteProgetto(id).subscribe({
        error: (err) => console.error("Errore nell'eliminazione:", err),
      });
    }
  }

  closeForm(): void {
    this.showForm = false;
    this.selectedProgetto = null;
  }

  getClienteNome(clienteId: number): string {
    const c = this.clienti.find((c) => c.id === clienteId);
    return c ? c.nome : 'N/D';
  }

  getNomeClienteSelect(clienteId: number): string {
    const c = this.clienti.find((c) => c.id === clienteId);
    return c ? c.nome : '';
  }

  // Wrapper per le funzioni di colore
  getBadgeBackground(name: string): string {
    return _getBg(name);
  }

  getBadgeBorder(name: string): string {
    return _getBd(name);
  }
}
