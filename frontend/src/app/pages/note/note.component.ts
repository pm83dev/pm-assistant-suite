import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { getBadgeBorder as _getBd, getBadgeBackground as _getBg } from '../../utils/badge-color';
import { NoteManagementService } from '../../services/note/note-management.service';
import { Nota } from '../../models/models';

@Component({
  selector: 'app-note',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './note.component.html',
  styleUrls: ['./note.component.css'],
})
export class NoteComponent {

  private noteManagement = inject(NoteManagementService);

  note = this.noteManagement.note;
  progetti = this.noteManagement.progetti;
  clienti = this.noteManagement.clienti;
  loading = this.noteManagement.loading;

  selectedNota: Nota | null = null;
  showForm = false;
  editMode = false;

  form = {
    titolo: '',
    contenuto: '',
    progettoId: 0,
  };

  openNewForm(): void {
    this.editMode = false;
    this.form = { titolo: '', contenuto: '', progettoId: this.progetti()[0]?.id || 0 };
    this.showForm = true;
  }

  openEditForm(nota: Nota): void {
    this.editMode = true;
    this.selectedNota = nota;
    this.form = {
      titolo: nota.titolo || '',
      contenuto: nota.contenuto,
      progettoId: nota.progettoId,
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
        progettoId: this.form.progettoId,
      };
      this.noteManagement.updateNota(updated).subscribe({
        next: () => { this.closeForm(); },
      });
    } else {
      this.noteManagement
        .addNota({
          dataCreazione: new Date().toISOString(),
          contenuto: this.form.contenuto,
          titolo: this.form.titolo || undefined,
          progettoId: this.form.progettoId,
        })
        .subscribe({
          next: () => { this.closeForm(); },
        });
    }
  }

  delete(id: number): void {
    if (confirm('Sei sicuro di voler eliminare questa nota?')) {
      this.noteManagement.deleteNota(id).subscribe({
        error: (err) => console.error("Errore nell'eliminazione:", err),
      });
    }
  }

  closeForm(): void {
    this.showForm = false;
    this.selectedNota = null;
  }

  getProgettoNome = this.noteManagement.getProgettoNome.bind(this.noteManagement);
  getClienteNomeDaProgetto = this.noteManagement.getClienteNomeDaProgetto.bind(this.noteManagement);

  getBadgeBackground = _getBg;
  getBadgeBorder = _getBd;

  formatDate(date: string): string {
    if (!date) return '';
    return new Date(date).toLocaleDateString('it-IT', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }
}
