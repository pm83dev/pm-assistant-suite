import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Cliente, Progetto } from '../../models/models';
import { ClientiService } from '../../services/clienti/clienti.service';
import { ProgettiService } from '../../services/progetti/progetti.service';
import { getBadgeBorder as _getBd, getBadgeBackground as _getBg } from '../../utils/badge-color';

@Component({
  selector: 'app-progetti',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './progetti.component.html',
  styleUrls: ['./progetti.component.css'],
})
export class ProgettiComponent implements OnInit {
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

  private projectService = inject(ProgettiService);
  private clientService = inject(ClientiService);

  constructor() {}

  ngOnInit(): void {
    this.loadClienti();
    this.loadProgetti();
  }

  loadClienti(): void {
    this.clientService.getClient().subscribe({
      next: (data: Cliente[]) => (this.clienti = data),
    });
  }

  loadProgetti(): void {
    this.projectService.getProgetti().subscribe({
      next: (data: Progetto[]) => (this.progetti = data),
      error: (err: unknown) => console.error('Errore nel caricamento progetti:', err),
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
      this.projectService.updateProgetto(updated).subscribe({
        next: () => {
          this.loadProgetti();
          this.closeForm();
        },
      });
    } else {
      this.projectService.addProgetto(this.form).subscribe({
        next: () => {
          this.loadProgetti();
          this.closeForm();
        },
      });
    }
  }

  delete(id: number): void {
    if (confirm('Sei sicuro di voler eliminare questo progetto?')) {
      this.projectService.deleteProgetto(id).subscribe({
        next: () => this.loadProgetti(),
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
