import { Component, effect, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Cliente } from '../../models/models';
import { ClienteManagementService } from '../../services/clienti/clienti-management.service';

@Component({
  selector: 'app-clienti',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './clienti.component.html',
  styleUrls: ['./clienti.component.css'],
})
export class ClientiComponent {
  selectedCliente: Cliente | null = null;
  showForm = false;
  editMode = false;

  form = {
    nome: '',
    email: '',
    telefono: '',
    indirizzo: '',
  };

  clientiManagementService = inject(ClienteManagementService);

  constructor() {
    effect(() => {
      this.clientiManagementService.clienti();
      this.clientiManagementService.loading();
    });
  }

  get clienti(): Cliente[] {
    return this.clientiManagementService.clienti();
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
      this.clientiManagementService.updateCliente(updated).subscribe({
        next: () => this.closeForm(),
        error: (err) => console.error('Errore nel salvataggio:', err),
      });
    } else {
      this.clientiManagementService.addCliente(this.form).subscribe({
        next: () => this.closeForm(),
        error: (err) => console.error('Errore nel salvataggio:', err),
      });
    }
  }

  delete(id: number): void {
    if (confirm('Sei sicuro di voler eliminare questo cliente?')) {
      this.clientiManagementService.deleteCliente(id).subscribe({
        error: (err) => console.error("Errore nell'eliminazione:", err),
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
