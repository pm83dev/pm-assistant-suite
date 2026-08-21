import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';
import { Cliente, OraLavorata, Progetto } from '../../models';
import { TimeTrackingService } from '../../time-tracking.service';
import { getBadgeBorder as _getBd, getBadgeBackground as _getBg } from '../../utils/badge-color';

const TARIFFA_ORARIA = 31.25;

interface MonthView {
  month: string;
  year: number;
  days: MonthDay[];
}

interface MonthDay {
  day: number;
  date: Date;
  ore: number;
  hasEntries: boolean;
  entries?: OraLavorata[];
}

@Component({
  selector: 'app-ore',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ore.component.html',
  styleUrls: ['./ore.component.css'],
})
export class OreComponent implements OnInit {
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

  calendarView: MonthView[] = [];
  currentMonthIndex: number = 0;
  animationDirection: 'prev' | 'next' = 'next';
  selectedDayEntries: OraLavorata[] = [];
  selectedDayDate: string = '';
  selectedClienteId = 0;

  constructor(private service: TimeTrackingService) {}

  get visibleMonth(): MonthView | undefined {
    return this.calendarView[this.currentMonthIndex];
  }

  getAnimationDirection(): 'prev' | 'next' {
    return this.animationDirection;
  }

  prevMonth(): void {
    if (this.currentMonthIndex > 0) {
      this.animationDirection = 'prev';
      this.currentMonthIndex--;
    }
  }

  nextMonth(): void {
    if (this.currentMonthIndex < this.calendarView.length - 1) {
      this.animationDirection = 'next';
      this.currentMonthIndex++;
    }
  }

  goToCurrentMonth(): void {
    if (this.calendarView.length > 0) {
      const today = new Date();
      const todayKey = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}`;
      const idx = this.calendarView.findIndex(
        (m) =>
          `${m.year}-${String(this.getMonthNumber(m.month) + 1).padStart(2, '0')}` === todayKey,
      );
      this.currentMonthIndex = idx >= 0 ? idx : this.calendarView.length - 1;
    }
  }

  private getMonthNumber(monthName: string): number {
    const months = [
      'Gennaio',
      'Febbraio',
      'Marzo',
      'Aprile',
      'Maggio',
      'Giugno',
      'Luglio',
      'Agosto',
      'Settembre',
      'Ottobre',
      'Novembre',
      'Dicembre',
    ];
    return months.indexOf(monthName);
  }

  private formatDateForInput(date: Date): string {
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  }

  ngOnInit(): void {
    this.loadClienti();
    this.loadProgetti();
    this.loadOre();
  }

  loadProgetti(): void {
    this.service.getProgetti().subscribe({
      next: (data: Progetto[]) => (this.progetti = data),
    });
  }

  loadClienti(): void {
    this.service.getClient().subscribe({
      next: (data: Cliente[]) => (this.clienti = data),
    });
  }

  loadOre(): void {
    this.service.getOreLavorate().subscribe({
      next: (data: OraLavorata[]) => {
        this.oreLavorate = data;
        this.buildCalendar();
      },
      error: (err: unknown) => console.error('Errore nel caricamento ore:', err),
    });
  }

  buildCalendar(): void {
    const dayMap = new Map<string, OraLavorata[]>();

    for (const ora of this.oreLavorate) {
      const date = new Date(ora.data);
      const key = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
      if (!dayMap.has(key)) {
        dayMap.set(key, []);
      }
      dayMap.get(key)!.push(ora);
    }

    const monthMap = new Map<string, OraLavorata[]>();

    for (const [key, entries] of dayMap) {
      const [yearStr, monthStr] = key.split('-');
      const monthKey = `${yearStr}-${monthStr}`;
      if (!monthMap.has(monthKey)) {
        monthMap.set(monthKey, []);
      }
      monthMap.get(monthKey)!.push(...entries);
    }

    this.calendarView = [];

    // Aggiungi il mese corrente se non è già presente
    const today = new Date();
    const currentMonthKey = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}`;
    if (!monthMap.has(currentMonthKey)) {
      monthMap.set(currentMonthKey, []);
    }

    // Determina il range di mesi: da 24 mesi prima di oggi a 24 mesi dopo
    let minYear = today.getFullYear();
    let minMonth = today.getMonth();
    for (let i = 0; i < 24; i++) {
      minMonth--;
      if (minMonth < 0) {
        minMonth = 11;
        minYear--;
      }
    }
    let maxYear = today.getFullYear();
    let maxMonth = today.getMonth();
    for (let i = 0; i < 24; i++) {
      maxMonth++;
      if (maxMonth >= 12) {
        maxMonth = 0;
        maxYear++;
      }
    }

    // Genera tutti i mesi nel range
    const monthNames = [
      'Gennaio',
      'Febbraio',
      'Marzo',
      'Aprile',
      'Maggio',
      'Giugno',
      'Luglio',
      'Agosto',
      'Settembre',
      'Ottobre',
      'Novembre',
      'Dicembre',
    ];

    let year = minYear;
    let month = minMonth;

    while (year < maxYear || (year === maxYear && month <= maxMonth)) {
      const daysInMonth = new Date(year, month + 1, 0).getDate();
      const firstDay = new Date(year, month, 1).getDay();
      const startOffset = firstDay === 0 ? 6 : firstDay - 1;

      const monthDays: MonthDay[] = [];

      for (let i = 0; i < startOffset; i++) {
        monthDays.push({ day: 0, date: new Date(), ore: 0, hasEntries: false });
      }

      for (let d = 1; d <= daysInMonth; d++) {
        const date = new Date(year, month, d);
        const dayKey = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
        const entries = dayMap.get(dayKey) || [];
        const ore = entries.reduce((sum, e) => sum + e.ore, 0);
        monthDays.push({
          day: d,
          date,
          ore,
          hasEntries: entries.length > 0,
          entries: entries.length > 0 ? entries : undefined,
        });
      }

      this.calendarView.push({
        month: monthNames[month],
        year,
        days: monthDays,
      });

      // Avanza al mese successivo
      month++;
      if (month >= 12) {
        month = 0;
        year++;
      }
    }

    // Posiziona la vista sul mese corrente
    const todayKey = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}`;
    const idx = this.calendarView.findIndex(
      (m) => `${m.year}-${String(this.getMonthNumber(m.month) + 1).padStart(2, '0')}` === todayKey,
    );
    this.currentMonthIndex = idx >= 0 ? idx : this.calendarView.length - 1;
  }

  isToday(date: Date): boolean {
    const today = new Date();
    return date.toDateString() === today.toDateString();
  }

  showDayDetails(entries: OraLavorata[], date: Date): void {
    this.selectedDayEntries = entries;
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
      this.showDayDetails(day.entries || [], day.date);
    }

    this.editMode = false;
    this.form = {
      data: this.formatDateForInput(day.date),
      ore: 1,
      descrizione: '',
      progettoId: this.progetti[0]?.id || 0,
    };
    this.showForm = true;
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
    if (this.form.progettoId === 0 || this.form.ore <= 0) return;

    // Usa la stringa YYYY-MM-DD direttamente per evitare shift di fuso orario
    const dataStr = this.form.data;

    if (this.editMode && this.selectedOra) {
      const updated: OraLavorata = {
        ...this.selectedOra,
        data: dataStr,
        ore: this.form.ore,
        descrizione: this.form.descrizione || undefined,
        progettoId: this.form.progettoId,
      };
      this.service.updateOraLavorata(updated).subscribe({
        next: () => {
          this.loadOre();
          this.closeForm();
        },
      });
    } else {
      this.service
        .addOraLavorata({
          data: dataStr,
          ore: this.form.ore,
          descrizione: this.form.descrizione || undefined,
          progettoId: this.form.progettoId,
        })
        .subscribe({
          next: () => {
            this.loadOre();
            this.closeForm();
          },
        });
    }
  }

  delete(id: number): void {
    if (confirm('Sei sicuro di voler eliminare questa registrazione?')) {
      this.service.deleteOraLavorata(id).subscribe({
        next: () => this.loadOre(),
      });
    }
  }

  closeForm(): void {
    this.showForm = false;
    this.selectedOra = null;
  }

  getProgettoNome(progettoId: number): string {
    const p = this.progetti.find((p) => p.id === progettoId);
    return p ? p.nome : 'N/D';
  }

  getClienteNomeDaProgetto(progettoId: number): string {
    const p = this.progetti.find((p) => p.id === progettoId);
    if (!p) return 'N/D';
    const c = this.clienti.find((c) => c.id === p.clienteId);
    return c ? c.nome : 'N/D';
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('it-IT');
  }

  getTotalOre(): number {
    const month = this.visibleMonth;
    if (!month) return 0;
    const monthIndex = this.getMonthNumber(month.month);
    return this.oreLavorate
      .filter((o) => {
        const d = new Date(o.data);
        return d.getFullYear() === month.year && d.getMonth() === monthIndex;
      })
      .reduce((sum, o) => sum + o.ore, 0);
  }

  getTotalOrePerProject(projectId: number): number {
    const month = this.visibleMonth;
    if (!month) return 0;
    const monthIndex = this.getMonthNumber(month.month);
    return this.oreLavorate
      .filter((o) => o.progettoId === projectId)
      .filter((o) => {
        const d = new Date(o.data);
        return d.getFullYear() === month.year && d.getMonth() === monthIndex;
      })
      .reduce((sum, o) => sum + o.ore, 0);
  }

  exportPdf(): void {
    const month = this.visibleMonth;
    if (!month || this.selectedClienteId === 0) return;

    const cliente = this.clienti.find((c) => c.id === this.selectedClienteId);
    if (!cliente) return;

    const monthIndex = this.getMonthNumber(month.month);
    const progettiIds = new Set(
      this.progetti.filter((p) => p.clienteId === this.selectedClienteId).map((p) => p.id),
    );

    const entries = this.oreLavorate
      .filter((o) => progettiIds.has(o.progettoId))
      .filter((o) => {
        const d = new Date(o.data);
        return d.getFullYear() === month.year && d.getMonth() === monthIndex;
      })
      .sort((a, b) => new Date(a.data).getTime() - new Date(b.data).getTime());

    if (entries.length === 0) {
      alert('Nessuna ora registrata per questo cliente nel mese selezionato.');
      return;
    }

    const totaleOre = entries.reduce((sum, o) => sum + o.ore, 0);
    const totaleImporto = totaleOre * TARIFFA_ORARIA;

    const doc = new jsPDF();

    doc.setFontSize(16);
    doc.text('Report attività', 14, 18);
    doc.setFontSize(11);
    doc.text(`Cliente: ${cliente.nome}`, 14, 27);
    doc.text(`Periodo: ${month.month} ${month.year}`, 14, 33);
    doc.text(`Fornitore: PM Software & Automation di Miccoli Paolo`, 14, 39);
    doc.text(`Generato il: ${new Date().toLocaleDateString('it-IT')}`, 14, 45);

    autoTable(doc, {
      startY: 51,
      head: [['Data', 'Progetto', 'Attività', 'Ore']],
      body: entries.map((o) => [
        this.formatDate(o.data),
        this.getProgettoNome(o.progettoId),
        o.descrizione || '-',
        o.ore.toFixed(1),
      ]),
      styles: { fontSize: 9 },
      headStyles: { fillColor: [13, 110, 253] },
    });

    const totaliPerProgetto = new Map<number, number>();
    for (const o of entries) {
      totaliPerProgetto.set(o.progettoId, (totaliPerProgetto.get(o.progettoId) || 0) + o.ore);
    }

    const afterTableY =
      (doc as unknown as { lastAutoTable: { finalY: number } }).lastAutoTable.finalY + 10;
    doc.setFontSize(12);
    doc.text('Totale Ore per Progetto', 14, afterTableY);

    autoTable(doc, {
      startY: afterTableY + 4,
      head: [['Progetto', 'Ore']],
      body: Array.from(totaliPerProgetto.entries()).map(([progettoId, ore]) => [
        this.getProgettoNome(progettoId),
        ore.toFixed(1),
      ]),
      styles: { fontSize: 9 },
      headStyles: { fillColor: [13, 110, 253] },
    });

    const finalY =
      (doc as unknown as { lastAutoTable: { finalY: number } }).lastAutoTable.finalY + 10;
    doc.setFontSize(12);
    doc.text(`Totale Ore: ${totaleOre.toFixed(1)}h`, 14, finalY);
    doc.text(`Totale Importo: ${totaleImporto.toFixed(2)}€`, 14, finalY + 7);

    const fileName = `Report_${cliente.nome.replace(/\s+/g, '_')}_${month.month}_${month.year}.pdf`;
    doc.save(fileName);
  }

  // Wrapper per le funzioni di colore
  getBadgeBackground(name: string): string {
    return _getBg(name);
  }

  getBadgeBorder(name: string): string {
    return _getBd(name);
  }
}
