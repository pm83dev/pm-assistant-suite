import { effect, inject, Injectable, signal } from '@angular/core';
import jspdf from 'jspdf';
import autoTable from 'jspdf-autotable';
import { forkJoin, tap } from 'rxjs';
import { Cliente, OraLavorata, Progetto } from '../../models/models';
import { ClientiService } from '../clienti/clienti.service';
import { ProgettiService } from '../progetti/progetti.service';
import { OreService } from './ore.service';

export interface MonthView {
  month: string;
  year: number;
  days: MonthDay[];
}

export interface MonthDay {
  day: number;
  date: Date;
  ore: number;
  hasEntries: boolean;
  entries?: OraLavorata[];
}

//signals

const TARIFFA_ORARIA = 31.25;

@Injectable({
  providedIn: 'root',
})
export class OreManagementService {
  readonly tariffaOraria = TARIFFA_ORARIA;

  private oreService = inject(OreService);
  private progettiService = inject(ProgettiService);
  private clientiService = inject(ClientiService);

  // 1. STATO PRIVATO (I veri contenitori dei dati)
  #oreLavorate = signal<OraLavorata[]>([]);
  #progetti = signal<Progetto[]>([]);
  #clienti = signal<Cliente[]>([]);
  #loading = signal<boolean>(false);

  // 2. STATO PUBBLICO (Solo lettura per i componenti)
  oreLavorate = this.#oreLavorate.asReadonly();
  progetti = this.#progetti.asReadonly();
  clienti = this.#clienti.asReadonly();
  loading = this.#loading.asReadonly();

  // Calendar state
  #calendarView = signal<MonthView[]>([]);
  #currentMonthIndex = signal<number>(0);
  calendarView = this.#calendarView.asReadonly();
  currentMonthIndexSig = this.#currentMonthIndex.asReadonly();

  constructor() {
    this.initDataLoading();
    // Ricostruisce il calendario ogni volta che le ore lavorate cambiano
    // (caricamento iniziale asincrono oppure aggiunta/modifica/eliminazione)
    effect(() => {
      this.buildCalendar(this.#oreLavorate());
    });
  }

  private initDataLoading(): void {
    this.#loading.set(true);
    forkJoin({
      ore: this.oreService.getOreLavorate(),
      progetti: this.progettiService.getProgetti(),
      clienti: this.clientiService.getClient(),
    })
      .subscribe({
        next: ({ ore, progetti, clienti }) => {
          this.#oreLavorate.set(ore);
          this.#progetti.set(progetti);
          this.#clienti.set(clienti);
        },
        error: (err) => {
          console.error('Errore nel caricamento:', err);
        },
      })
      .add(() => {
        this.#loading.set(false);
      });
  }

  private buildCalendar(ore: OraLavorata[]): void {
    const dayMap = new Map<string, OraLavorata[]>();

    for (const ora of ore) {
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

    const calendarView: MonthView[] = [];
    const today = new Date();
    const currentMonthKey = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}`;

    // Range: 24 months before and after
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
        const oreSum = entries.reduce((sum, e) => sum + e.ore, 0);
        monthDays.push({
          day: d,
          date,
          ore: oreSum,
          hasEntries: entries.length > 0,
          entries: entries.length > 0 ? entries : undefined,
        });
      }

      calendarView.push({
        month: monthNames[month],
        year,
        days: monthDays,
      });

      month++;
      if (month >= 12) {
        month = 0;
        year++;
      }
    }

    this.#calendarView.set(calendarView);

    const idx = calendarView.findIndex(
      (m) =>
        `${m.year}-${String(this.getMonthNumber(m.month) + 1).padStart(2, '0')}` ===
        currentMonthKey,
    );
    this.#currentMonthIndex.set(idx >= 0 ? idx : calendarView.length - 1);
  }

  // Business Logic Methods
  getVisibleMonth(index: number): MonthView | undefined {
    return this.#calendarView()[index];
  }

  getTotalOreForMonth(month: MonthView): number {
    const monthIndex = this.getMonthNumber(month.month);
    return this.oreLavorate()
      .filter((o: OraLavorata) => {
        const d = new Date(o.data);
        return d.getFullYear() === month.year && d.getMonth() === monthIndex;
      })
      .reduce((sum: number, o: OraLavorata) => sum + o.ore, 0);
  }

  getTotalOrePerProjectForMonth(projectId: number, month: MonthView): number {
    const monthIndex = this.getMonthNumber(month.month);
    return this.oreLavorate()
      .filter((o: OraLavorata) => o.progettoId === projectId)
      .filter((o: OraLavorata) => {
        const d = new Date(o.data);
        return d.getFullYear() === month.year && d.getMonth() === monthIndex;
      })
      .reduce((sum: number, o: OraLavorata) => sum + o.ore, 0);
  }

  getProgettoNome(progettoId: number): string {
    const p = this.progetti().find((p: Progetto) => p.id === progettoId);
    return p ? p.nome : 'N/D';
  }

  getClienteNomeDaProgetto(progettoId: number): string {
    const p = this.progetti().find((p: Progetto) => p.id === progettoId);
    if (!p) return 'N/D';
    const c = this.clienti().find((c: Cliente) => c.id === p.clienteId);
    return c ? c.nome : 'N/D';
  }

  // Actions
  addOra(ora: Omit<OraLavorata, 'id'>) {
    return this.oreService
      .addOraLavorata(ora)
      .pipe(tap((created) => this.#oreLavorate.update((list) => [...list, created])));
  }

  updateOra(ora: OraLavorata) {
    return this.oreService.updateOraLavorata(ora).pipe(
      tap(() =>
        this.#oreLavorate.update((list) => list.map((o) => (o.id === ora.id ? ora : o))),
      ),
    );
  }

  deleteOra(id: number) {
    return this.oreService
      .deleteOraLavorata(id)
      .pipe(tap(() => this.#oreLavorate.update((list) => list.filter((o) => o.id !== id))));
  }

  // Getters per lo stato pubblico (necessari per il componente)
  getCalendarView(): MonthView[] {
    return this.#calendarView();
  }

  getCurrentMonthIndex(): number {
    return this.#currentMonthIndex();
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('it-IT');
  }

  // Export pdf
  exportPdfService(month: MonthView, selectedClienteId: number): void {
    if (!month || selectedClienteId === 0) return;

    const cliente = this.clienti().find((c: Cliente) => c.id === selectedClienteId);
    if (!cliente) return;

    const monthIndex = this.getMonthNumber(month.month);

    const progettiIds = new Set(
      this.progetti()
        .filter((p: Progetto) => p.clienteId === selectedClienteId)
        .map((p: Progetto) => p.id),
    );

    const entries = this.oreLavorate()
      .filter((o: OraLavorata) => progettiIds.has(o.progettoId))
      .filter((o: OraLavorata) => {
        const d = new Date(o.data);
        return d.getFullYear() === month.year && d.getMonth() === monthIndex;
      })
      .sort(
        (a: OraLavorata, b: OraLavorata) => new Date(a.data).getTime() - new Date(b.data).getTime(),
      );

    if (entries.length === 0) {
      alert('Nessuna ora registrata per questo cliente nel mese selezionato.');
      return;
    }

    const totaleOre = entries.reduce((sum: number, o: OraLavorata) => sum + o.ore, 0);
    const totaleImporto = totaleOre * TARIFFA_ORARIA;

    const doc = new jspdf();

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
      body: entries.map((o: OraLavorata) => [
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

  // Helpers per UI
  public getMonthNumber(monthName: string): number {
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
}
