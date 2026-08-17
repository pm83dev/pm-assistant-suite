import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Cliente, OraLavorata, Progetto } from '../../models';
import { TimeTrackingService } from '../../time-tracking.service';
import { getBadgeBorder as _getBd, getBadgeBackground as _getBg } from '../../utils/badge-color';

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
  selectedDayEntries: OraLavorata[] = [];
  selectedDayDate: string = '';

  constructor(private service: TimeTrackingService) {}

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
    const sortedKeys = Array.from(monthMap.keys()).sort();

    for (const key of sortedKeys) {
      const [yearStr, monthStr] = key.split('-');
      const year = parseInt(yearStr);
      const month = parseInt(monthStr) - 1;
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
    }
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
      data: new Date().toISOString().split('T')[0],
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

    const dataOra = new Date(this.form.data);
    dataOra.setHours(0, 0, 0, 0);

    if (this.editMode && this.selectedOra) {
      const updated: OraLavorata = {
        ...this.selectedOra,
        data: dataOra.toISOString(),
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
          data: dataOra.toISOString(),
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
    return this.oreLavorate.reduce((sum, o) => sum + o.ore, 0);
  }

  // Wrapper per le funzioni di colore
  getBadgeBackground(name: string): string {
    return _getBg(name);
  }

  getBadgeBorder(name: string): string {
    return _getBd(name);
  }
}
