import { inject, Injectable } from '@angular/core';
import { BehaviorSubject, combineLatest } from 'rxjs';
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

@Injectable({
  providedIn: 'root',
})
export class OreManagementService {
  private oreService = inject(OreService);
  private progettiService = inject(ProgettiService);
  private clientiService = inject(ClientiService);

  // State management
  private oreLavorateSubject = new BehaviorSubject<OraLavorata[]>([]);
  private progettiSubject = new BehaviorSubject<Progetto[]>([]);
  private clientiSubject = new BehaviorSubject<Cliente[]>([]);

  oreLavorate$ = this.oreLavorateSubject.asObservable();
  progetti$ = this.progettiSubject.asObservable();
  clienti$ = this.clientiSubject.asObservable();

  // Calendar state
  private calendarViewSubject = new BehaviorSubject<MonthView[]>([]);
  calendarView$ = this.calendarViewSubject.asObservable();

  private currentMonthIndexSubject = new BehaviorSubject<number>(0);
  currentMonthIndex$ = this.currentMonthIndexSubject.asObservable();

  constructor() {
    this.initDataLoading();
    this.setupCalendarSync();
  }

  private initDataLoading(): void {
    combineLatest([
      this.oreService.getOreLavorate(),
      this.progettiService.getProgetti(),
      this.clientiService.getClient(),
    ]).subscribe({
      next: ([ore, progetti, clienti]) => {
        this.oreLavorateSubject.next(ore);
        this.progettiSubject.next(progetti);
        this.clientiSubject.next(clienti);
        this.buildCalendar(ore);
      },
      error: (err) => console.error('Errore nel caricamento dati iniziali:', err),
    });
  }

  private setupCalendarSync(): void {
    this.oreLavorate$.subscribe((ore) => this.buildCalendar(ore));
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

    this.calendarViewSubject.next(calendarView);

    const idx = calendarView.findIndex(
      (m) =>
        `${m.year}-${String(this.getMonthNumber(m.month) + 1).padStart(2, '0')}` ===
        currentMonthKey,
    );
    this.currentMonthIndexSubject.next(idx >= 0 ? idx : calendarView.length - 1);
  }


  // Business Logic Methods
  getVisibleMonth(index: number): MonthView | undefined {
    return this.calendarViewSubject.value[index];
  }

  getTotalOreForMonth(month: MonthView): number {
    const monthIndex = this.getMonthNumber(month.month);
    return this.oreLavorateSubject.value
      .filter((o) => {
        const d = new Date(o.data);
        return d.getFullYear() === month.year && d.getMonth() === monthIndex;
      })
      .reduce((sum, o) => sum + o.ore, 0);
  }

  getTotalOrePerProjectForMonth(projectId: number, month: MonthView): number {
    const monthIndex = this.getMonthNumber(month.month);
    return this.oreLavorateSubject.value
      .filter((o) => o.progettoId === projectId)
      .filter((o) => {
        const d = new Date(o.data);
        return d.getFullYear() === month.year && d.getMonth() === monthIndex;
      })
      .reduce((sum, o) => sum + o.ore, 0);
  }

  getProgettoNome(progettoId: number): string {
    const p = this.progettiSubject.value.find((p) => p.id === progettoId);
    return p ? p.nome : 'N/D';
  }

  getClienteNomeDaProgetto(progettoId: number): string {
    const p = this.progettiSubject.value.find((p) => p.id === progettoId);
    if (!p) return 'N/D';
    const c = this.clientiSubject.value.find((c) => c.id === p.clienteId);
    return c ? c.nome : 'N/D';
  }

  // Actions
  addOra(ora: Omit<OraLavorata, 'id'>) {
    return this.oreService.addOraLavorata(ora);
  }

  updateOra(ora: OraLavorata) {
    return this.oreService.updateOraLavorata(ora);
  }

  deleteOra(id: number) {
    return this.oreService.deleteOraLavorata(id);
  }

  // Getters per lo stato pubblico (necessari per il componente)
  getCalendarView(): MonthView[] {
    return this.calendarViewSubject.value;
  }

  getCurrentMonthIndex(): number {
    return this.currentMonthIndexSubject.value;
  }

  // Helpers per UI
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
}
