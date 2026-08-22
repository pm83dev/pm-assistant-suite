import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Cliente } from '../../models/models';
import { ClientiService } from '../../services/clienti/clienti.service';

@Component({
  selector: 'app-clienti',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './clienti.component.html',
  styleUrls: ['./clienti.component.css'],
})
export class ClientiComponent implements OnInit {
  clienti: Cliente[] = [];
  selectedCliente: Cliente | null = null;
  showForm = false;
  editMode = false;

  form = {
    nome: '',
    email: '',
    telefono: '',
    indirizzo: '',
  };

  private clientiService = inject(ClientiService);

  constructor() {}

  ngOnInit(): void {
    this.loadClienti();
  }

  loadClienti(): void {
    this.clientiService.getClient().subscribe({
      next: (data: Cliente[]) => (this.clienti = data),
      error: (err: unknown) => console.error('Errore nel caricamento clienti:', err),
    });
  }

  openNewForm(): void {
    this.editMode = false;
    this.form = { nome: '', email: '', telefono: '', indirizzo: '' };
    this.showForm = true;
  }

  openEditForm(cliente: Cliente): void {
    this.editMode = true;
    this.selectedCliente = cliente;
    this.form = {
      nome: cliente.nome,
      email: cliente.email || '',
      telefono: cliente.telefono || '',
      indirizzo: cliente.indirizzo || '',
    };
    this.showForm = true;
  }

  save(): void {
    if (!this.form.nome.trim()) return;

    if (this.editMode && this.selectedCliente) {
      const updated: Cliente = {
        ...this.selectedCliente,
        nome: this.form.nome,
        email: this.form.email || undefined,
        telefono: this.form.telefono || undefined,
        indirizzo: this.form.indirizzo || undefined,
      };
      this.clientiService.updateCliente(updated).subscribe({
        next: () => {
          this.loadClienti();
          this.closeForm();
        },
      });
    } else {
      this.clientiService.addCliente(this.form).subscribe({
        next: () => {
          this.loadClienti();
          this.closeForm();
        },
      });
    }
  }

  delete(id: number): void {
    if (confirm('Sei sicuro di voler eliminare questo cliente?')) {
      this.clientiService.deleteCliente(id).subscribe({
        next: () => this.loadClienti(),
      });
    }
  }

  closeForm(): void {
    this.showForm = false;
    this.selectedCliente = null;
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('it-IT');
  }
}
