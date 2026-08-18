import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Progetto, Nota, Cliente } from '../../models';
import { TimeTrackingService } from '../../time-tracking.service';
import { getBadgeBackground as _getBg, getBadgeBorder as _getBd } from '../../utils/badge-color';

@Component({
  selector: 'app-note',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './note.component.html',
  styleUrls: ['./note.component.css']
})
export class NoteComponent implements OnInit {
  note: Nota[] = [];
  progetti: Progetto[] = [];
  clienti: Cliente[] = [];
  selectedNota: Nota | null = null;
  showForm = false;
  editMode = false;

  form = {
    titolo: '',
    contenuto: '',
    progettoId: 0
  };

  constructor(private service: TimeTrackingService) {}

  ngOnInit(): void {
    this.loadClienti();
    this.loadProgetti();
    this.loadNote();
  }

  loadProgetti(): void {
    this.service.getProgetti().subscribe({
      next: (data: Progetto[]) => this.progetti = data
    });
  }

  loadClienti(): void {
    this.service.getClient().subscribe({
      next: (data: Cliente[]) => this.clienti = data
    });
  }

  loadNote(): void {
    this.service.getNote().subscribe({
      next: (data: Nota[]) => this.note = data,
      error: (err: unknown) => console.error('Errore nel caricamento note:', err)
    });
  }

  openNewForm(): void {
    this.editMode = false;
    this.form = { titolo: '', contenuto: '', progettoId: this.progetti[0]?.id || 0 };
    this.showForm = true;
  }

  openEditForm(nota: Nota): void {
    this.editMode = true;
    this.selectedNota = nota;
    this.form = {
      titolo: nota.titolo || '',
      contenuto: nota.contenuto,
      progettoId: nota.progettoId
    };
    this.showForm = true;
  }

  save(): void {
    if (!this.form.contenuto.trim() || this.form.progettoId === 0) return;

    if (this.editMode && this.selectedNota) {
      const updated: Nota = {
        ...this.selectedNota,
        titolo: this.form.titolo || undefined,
        contenuto: this.form.contenuto,
        progettoId: this.form.progettoId
      };
      this.service.updateNota(updated).subscribe({
        next: () => { this.loadNote(); this.closeForm(); }
      });
    } else {
      this.service.addNota({
        dataCreazione: new Date().toISOString(),
        contenuto: this.form.contenuto,
        titolo: this.form.titolo || undefined,
        progettoId: this.form.progettoId
      }).subscribe({
        next: () => { this.loadNote(); this.closeForm(); }
      });
    }
  }

  delete(id: number): void {
    if (confirm('Sei sicuro di voler eliminare questa nota?')) {
      this.service.deleteNota(id).subscribe({
        next: () => this.loadNote()
      });
    }
  }

  closeForm(): void {
    this.showForm = false;
    this.selectedNota = null;
  }

  getProgettoNome(progettoId: number): string {
    const p = this.progetti.find(p => p.id === progettoId);
    return p ? p.nome : 'N/D';
  }

  getClienteNomeDaProgetto(progettoId: number): string {
    const p = this.progetti.find(p => p.id === progettoId);
    if (!p) return 'N/D';
    const c = this.clienti.find(c => c.id === p.clienteId);
    return c ? c.nome : 'N/D';
  }

  // Wrapper per le funzioni di colore
  getBadgeBackground(name: string): string {
    return _getBg(name);
  }

  getBadgeBorder(name: string): string {
    return _getBd(name);
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('it-IT', {
      day: '2-digit', month: '2-digit', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }
}
