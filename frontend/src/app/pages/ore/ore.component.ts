import { CommonModule } from '@angular/common';
import { Component, effect, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Cliente, OraLavorata, Progetto } from '../../models/models';
import { OreManagementService } from '../../services/ore/ore-management.service';
import { getBadgeBorder as _getBd, getBadgeBackground as _getBg } from '../../utils/badge-color';

interface MonthDay {
  day: number;
  date: Date;
  ore: number;
  hasEntries: boolean;
  entries?: OraLavorata[];
}

interface CalendarMonth {
  month: string;
  year: number;
  days: MonthDay[];
}

@Component({
  selector: 'app-ore',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ore.component.html',
  styleUrls: ['./ore.component.css'],
})
export class OreComponent {
  oreLavorate: OraLavorata[] = [];
  progetti: Progetto[] = [];
  clienti: Cliente[] = [];
  selectedOra: OraLavorata | null = null;
  showForm = false;
  editMode = false;

  form = {
    data: new Date().toISOString().split('T')[0],
    ore: 1,
    descrizione: '',
    progettoId: 0,
  };

  calendarView: CalendarMonth[] = [];
  currentMonthIndex = 0;
  animationDirection: 'prev' | 'next' = 'next';
  selectedDayEntries: OraLavorata[] = [];
  selectedDayDate = '';
  private selectedDayDateRaw: Date | null = null;
  selectedClienteId = 0;
  selectedProgettoId = 0;
  visibleMonth: CalendarMonth | null = null;

  private oreManagementService = inject(OreManagementService);
  private calendarInitialized = false;

  constructor() {
    // Tiene sincronizzati i dati locali con lo stato del servizio, che si aggiorna
    // in modo asincrono (caricamento iniziale via HTTP, aggiunte/modifiche/eliminazioni)
    effect(() => {
      this.oreLavorate = this.oreManagementService.oreLavorate();
      this.progetti = this.oreManagementService.progetti();
      this.clienti = this.oreManagementService.clienti();
      this.calendarView = this.oreManagementService.calendarView();

      // Il mese visualizzato viene impostato sul mese corrente solo al primo
      // caricamento, per non annullare la navigazione manuale dell'utente
      if (!this.calendarInitialized && this.calendarView.length > 0) {
        this.currentMonthIndex = this.oreManagementService.getCurrentMonthIndex();
        this.calendarInitialized = true;
      }

      this.updateVisibleMonth();
      this.syncSelectedDayEntries();
    });
  }

  getAnimationDirection(): 'prev' | 'next' {
    return this.animationDirection;
  }

  prevMonth(): void {
    if (this.currentMonthIndex > 0) {
      this.animationDirection = 'prev';
      this.currentMonthIndex--;
      this.updateVisibleMonth();
    }
  }

  nextMonth(): void {
    if (this.currentMonthIndex < this.calendarView.length - 1) {
      this.animationDirection = 'next';
      this.currentMonthIndex++;
      this.updateVisibleMonth();
    }
  }

  goToCurrentMonth(): void {
    if (this.calendarView.length > 0) {
      const today = new Date();
      const todayKey = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}`;
      const idx = this.calendarView.findIndex(
        (m) =>
          `${m.year}-${String(this.oreManagementService.getMonthNumber(m.month) + 1).padStart(2, '0')}` ===
          todayKey,
      );
      this.currentMonthIndex = idx >= 0 ? idx : this.calendarView.length - 1;
      this.updateVisibleMonth();
    }
  }

  private updateVisibleMonth(): void {
    if (
      this.calendarView.length > 0 &&
      this.currentMonthIndex >= 0 &&
      this.currentMonthIndex < this.calendarView.length
    ) {
      this.visibleMonth = this.calendarView[this.currentMonthIndex];
    }
  }

  private formatDateForInput(date: Date): string {
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  }

  /**
   * Riallinea le voci del giorno attualmente aperto con i dati più recenti
   * (es. dopo una modifica/eliminazione), chiudendo il pannello se il giorno
   * non ha più registrazioni.
   */
  private syncSelectedDayEntries(): void {
    if (!this.selectedDayDate || this.selectedDayEntries.length === 0) return;

    const ids = new Set(this.selectedDayEntries.map((e) => e.id));
    const refreshed = this.oreLavorate.filter((o) => ids.has(o.id));

    if (refreshed.length === 0) {
      this.closeDayDetails();
    } else {
      this.selectedDayEntries = refreshed;
    }
  }

  isToday(date: Date): boolean {
    const today = new Date();
    return date.toDateString() === today.toDateString();
  }

  showDayDetails(entries: OraLavorata[], date: Date): void {
    this.selectedDayEntries = entries;
    this.selectedDayDateRaw = date;
    this.selectedDayDate = date.toLocaleDateString('it-IT', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  }

  closeDayDetails(): void {
    this.selectedDayEntries = [];
    this.selectedDayDate = '';
    this.selectedDayDateRaw = null;
  }

  getDayTooltip(day: MonthDay): string {
    if (!day.hasEntries || !day.entries) {
      return '';
    }

    const entries = day.entries;
    const tooltipLines = [`Giorno: ${day.date.toLocaleDateString('it-IT')}`];

    entries.forEach((entry) => {
      const clienteNome = this.getClienteNomeDaProgetto(entry.progettoId);
      const progettoNome = this.getProgettoNome(entry.progettoId);
      tooltipLines.push(
        `${clienteNome} - ${progettoNome}: ${entry.ore}h${entry.descrizione ? ` - ${entry.descrizione}` : ''}`,
      );
    });

    return tooltipLines.join('\n');
  }

  openNewForm(): void {
    this.editMode = false;
    this.form = {
      data: this.formatDateForInput(new Date()),
      ore: 1,
      descrizione: '',
      progettoId: this.progetti[0]?.id || 0,
    };
    this.showForm = true;
  }

  openFormForDay(day: MonthDay): void {
    if (day.day === 0) return;

    if (day.hasEntries) {
      // Giorno con registrazioni: mostra solo il dettaglio, l'apertura del form
      // avviene tramite il pulsante "Aggiungi ore" nel pannello dei dettagli
      this.showDayDetails(day.entries || [], day.date);
      return;
    }

    this.openFormForDate(day.date);
  }

  /**
   * Apre il form di nuova registrazione precompilato con la data indicata
   */
  openFormForDate(date: Date): void {
    this.editMode = false;
    this.form = {
      data: this.formatDateForInput(date),
      ore: 1,
      descrizione: '',
      progettoId: this.progetti[0]?.id || 0,
    };
    this.showForm = true;
  }

  /**
   * Apre il form per aggiungere altre ore al giorno attualmente selezionato
   */
  addOraForSelectedDay(): void {
    if (!this.selectedDayDateRaw) return;
    this.openFormForDate(this.selectedDayDateRaw);
  }

  openEditForm(ora: OraLavorata): void {
    this.editMode = true;
    this.selectedOra = ora;
    this.form = {
      data: ora.data.split('T')[0],
      ore: ora.ore,
      descrizione: ora.descrizione || '',
      progettoId: ora.progettoId,
    };
    this.showForm = true;
  }

  save(): void {
    if (this.editMode && this.selectedOra) {
      this.oreManagementService.updateOra({ ...this.selectedOra, ...this.form }).subscribe({
        next: () => this.closeForm(),
        error: (err) => console.error('Errore nel salvataggio:', err),
      });
    } else {
      this.oreManagementService.addOra({ ...this.form }).subscribe({
        next: () => this.closeForm(),
        error: (err) => console.error('Errore nel salvataggio:', err),
      });
    }
  }

  delete(id: number): void {
    if (!confirm('Sei sicuro di voler eliminare questa registrazione?')) return;
    this.oreManagementService.deleteOra(id).subscribe({
      error: (err) => console.error("Errore nell'eliminazione:", err),
    });
  }

  closeForm(): void {
    this.showForm = false;
    this.selectedOra = null;
  }

  /**
   * Esporta il report PDF del mese corrente per il cliente selezionato
   */
  exportPdf(): void {
    if (this.selectedClienteId === 0) return;
    if (!this.visibleMonth) return;
    this.oreManagementService.exportPdfService(this.visibleMonth, this.selectedClienteId);
  }

  /**
   * Restituisce il totale delle ore lavorate nel mese corrente
   */
  getTotalOre(): number {
    if (!this.visibleMonth) return 0;
    return this.oreManagementService.getTotalOreForMonth(this.visibleMonth);
  }

  /**
   * Restituisce il totale dell'importo del mese corrente
   */
  getTotalImporto(): number {
    return this.getTotalOre() * this.oreManagementService.tariffaOraria;
  }

  /**
   * Restituisce il totale delle ore per un progetto nel mese corrente
   */
  getTotalOrePerProject(projectId: number): number {
    if (!this.visibleMonth) return 0;
    return this.oreManagementService.getTotalOrePerProjectForMonth(projectId, this.visibleMonth);
  }

  /**
   * Restituisce il nome del cliente associato a un progetto
   */
  getClienteNomeDaProgetto(progettoId: number): string {
    return this.oreManagementService.getClienteNomeDaProgetto(progettoId);
  }

  /**
   * Restituisce il nome di un progetto dato l'ID
   */
  getProgettoNome(progettoId: number): string {
    return this.oreManagementService.getProgettoNome(progettoId);
  }

  /**
   * Wrapper per getBadgeBackground (usato nel template)
   */
  getBadgeBackground(name: string): string {
    return _getBg(name);
  }

  /**
   * Wrapper per getBadgeBorder (usato nel template)
   */
  getBadgeBorder(name: string): string {
    return _getBd(name);
  }
}
