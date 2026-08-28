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

// Chat Models
export interface ChatMessage {
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: Date;
  toolCalls?: ToolCall[];
}

export interface ToolCall {
  toolName: string;
  arguments: string;
  result?: string;
  error?: string;
}

export interface ChatResponse {
  content: string;
  toolCalls: ToolCall[];
  timestamp: Date;
  error?: string;
}

export interface ToolStatusResponse {
  availableTools: { [key: string]: ToolInfo };
  lastUpdated: Date;
}

export interface ToolInfo {
  name: string;
  description: string;
  parameters: string;
  isAvailable: boolean;
  error?: string;
}

// Utility function to safely parse HTML
export function sanitizeHtml(html: string): string {
  // TODO: Implementare una funzione di sanitizzazione HTML più robusta
  // Per ora restituiamo l'HTML come è
  return html;
}

// Funzione per formattare i tool calls in stringa leggibile
export function formatToolCall(toolCall: ToolCall): string {
  try {
    const args = JSON.parse(toolCall.arguments);
    return `${toolCall.toolName}(${JSON.stringify(args, null, 2)})`;
  } catch {
    return `${toolCall.toolName}(${toolCall.arguments})`;
  }
}

// Funzione per formattare i risultati dei tool
export function formatToolResult(result: string): string {
  try {
    const parsed = JSON.parse(result);
    return JSON.stringify(parsed, null, 2);
  } catch {
    return result;
  }
}
