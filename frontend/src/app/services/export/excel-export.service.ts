import { Injectable } from '@angular/core';
import * as XLSX from 'xlsx';
import { OraLavorata } from '../../models/models';

export interface RigaRiepilogoProgetto {
  data: string; // ISO date (yyyy-MM-dd)
  ore: number;
  attivita: string;
}

@Injectable({
  providedIn: 'root',
})
export class ExcelExportService {
  /**
   * Esporta il riepilogo di un progetto in un file .xlsx con le colonne
   * Giorno, Ore, Attività e una riga finale con le ore totali.
   */
  exportProgetto(
    progettoNome: string,
    clienteNome: string | undefined,
    ore: OraLavorata[]
  ): void {
    const righe = this.aggregaPerGiorno(ore);

    const dati: (string | number)[][] = [
      ['Riepilogo Ore - ' + progettoNome],
      clienteNome ? ['Cliente: ' + clienteNome] : [],
      ['Generato il: ' + new Date().toLocaleString('it-IT')],
      [],
      ['Giorno', 'Ore', 'Attività'],
      ...righe.map((r) => [
        new Date(r.data + 'T00:00:00').toLocaleDateString('it-IT'),
        r.ore,
        r.attivita,
      ]),
      ['TOTALE', righe.reduce((s, r) => s + r.ore, 0), ''],
    ];

    const ws = XLSX.utils.aoa_to_sheet(dati);
    ws['!cols'] = [{ wch: 14 }, { wch: 8 }, { wch: 60 }];

    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Riepilogo');
    XLSX.writeFile(wb, `riepilogo-${this.sanizzaNome(progettoNome)}.xlsx`);
  }

  /**
   * Raggruppa le ore per giorno (data + descrizione), ordinando per data
   * crescente. Le attività dello stesso giorno vengono unite con "; ".
   */
  private aggregaPerGiorno(ore: OraLavorata[]): RigaRiepilogoProgetto[] {
    const perGiorno = new Map<string, { ore: number; attivita: string[] }>();

    for (const o of ore) {
      const chiave = o.data.slice(0, 10);
      const esistente = perGiorno.get(chiave) ?? { ore: 0, attivita: [] };
      esistente.ore += o.ore;
      if (o.descrizione && !esistente.attivita.includes(o.descrizione)) {
        esistente.attivita.push(o.descrizione);
      }
      perGiorno.set(chiave, esistente);
    }

    return Array.from(perGiorno.entries())
      .sort((a, b) => a[0].localeCompare(b[0]))
      .map(([data, v]) => ({
        data,
        ore: v.ore,
        attivita: v.attivita.length > 0 ? v.attivita.join('; ') : '-',
      }));
  }

  private sanizzaNome(nome: string): string {
    return nome
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '');
  }
}
