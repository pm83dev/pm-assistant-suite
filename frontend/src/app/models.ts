// Time Tracking Models
export interface Cliente {
  id: number;
  nome: string;
  email?: string;
  telefono?: string;
  indirizzo?: string;
}

export interface Progetto {
  id: number;
  nome: string;
  descrizione?: string;
  clienteId: number;
  clienteNome?: string;
}

export interface OraLavorata {
  id: number;
  data: string;
  ore: number;
  descrizione?: string;
  progettoId: number;
}

export interface Nota {
  id: number;
  dataCreazione: string;
  contenuto: string;
  titolo?: string;
  progettoId: number;
}

export interface TotaleOre {
  progettoId: number;
  totaleOre: number;
}
