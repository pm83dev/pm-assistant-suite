import { Injectable } from '@angular/core';

export enum LogLevel {
  DEBUG = 0,
  INFO = 1,
  WARN = 2,
  ERROR = 3
}

export interface LogEntry {
  level: LogLevel;
  message: string;
  timestamp: Date;
  context?: string;
  data?: any;
  userId?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ChatLoggerService {
  private logs: LogEntry[] = [];
  private maxLogs = 1000;
  private logLevel = LogLevel.INFO;

  constructor() {
    // Carica i log salvati
    this.loadLogs();
  }

  debug(message: string, context?: string, data?: any): void {
    this.log(LogLevel.DEBUG, message, context, data);
  }

  info(message: string, context?: string, data?: any): void {
    this.log(LogLevel.INFO, message, context, data);
  }

  warn(message: string, context?: string, data?: any): void {
    this.log(LogLevel.WARN, message, context, data);
  }

  error(message: string, context?: string, data?: any): void {
    this.log(LogLevel.ERROR, message, context, data);
  }

  private log(level: LogLevel, message: string, context?: string, data?: any): void {
    const entry: LogEntry = {
      level,
      message,
      timestamp: new Date(),
      context,
      data
    };

    this.logs.push(entry);

    // Mantiene il numero massimo di log
    if (this.logs.length > this.maxLogs) {
      this.logs.shift();
    }

    // Salva i log
    this.saveLogs();

    // Console logging solo per livelli significativi in produzione
    if (this.shouldLogToConsole(level)) {
      this.consoleLog(level, message, context, data);
    }
  }

  private shouldLogToConsole(level: LogLevel): boolean {
    // In produzione, logga solo WARN e ERROR
    // In development, logga tutto
    return true; // TODO: Aggiungere una configurazione di ambiente
  }

  private consoleLog(level: LogLevel, message: string, context?: string, data?: any): void {
    const timestamp = new Date().toISOString();
    const prefix = `[${timestamp}] [${this.getLevelName(level)}]`;
    
    if (context) {
      console.log(`${prefix} [${context}] ${message}`, data || '');
    } else {
      console.log(`${prefix} ${message}`, data || '');
    }
  }

  private getLevelName(level: LogLevel): string {
    switch (level) {
      case LogLevel.DEBUG: return 'DEBUG';
      case LogLevel.INFO: return 'INFO';
      case LogLevel.WARN: return 'WARN';
      case LogLevel.ERROR: return 'ERROR';
      default: return 'UNKNOWN';
    }
  }

  getLogs(level?: LogLevel, limit?: number): LogEntry[] {
    let filteredLogs = this.logs;

    if (level !== undefined) {
      filteredLogs = filteredLogs.filter(log => log.level === level);
    }

    if (limit !== undefined) {
      filteredLogs = filteredLogs.slice(-limit);
    }

    return [...filteredLogs].reverse(); // Ritorna in ordine cronologico inverso
  }

  getLogsByContext(context: string, level?: LogLevel): LogEntry[] {
    let filteredLogs = this.logs.filter(log => log.context === context);

    if (level !== undefined) {
      filteredLogs = filteredLogs.filter(log => log.level === level);
    }

    return [...filteredLogs].reverse();
  }

  clearLogs(): void {
    this.logs = [];
    this.saveLogs();
  }

  exportLogs(): string {
    const exportData = {
      exportedAt: new Date().toISOString(),
      totalLogs: this.logs.length,
      logs: this.logs
    };

    return JSON.stringify(exportData, null, 2);
  }

  importLogs(jsonData: string): void {
    try {
      const importData = JSON.parse(jsonData);
      if (importData.logs && Array.isArray(importData.logs)) {
        this.logs = importData.logs.map((log: any) => ({
          ...log,
          timestamp: new Date(log.timestamp)
        }));
        this.saveLogs();
      }
    } catch (error) {
      console.error('Error importing logs:', error);
    }
  }

  private saveLogs(): void {
    try {
      localStorage.setItem('chat-logs', JSON.stringify(this.logs));
    } catch (error) {
      console.warn('Could not save logs to localStorage:', error);
    }
  }

  private loadLogs(): void {
    try {
      const savedLogs = localStorage.getItem('chat-logs');
      if (savedLogs) {
        const parsedLogs = JSON.parse(savedLogs);
        this.logs = parsedLogs.map((log: any) => ({
          ...log,
          timestamp: new Date(log.timestamp)
        }));
      }
    } catch (error) {
      console.warn('Could not load logs from localStorage:', error);
      this.logs = [];
    }
  }

  // Metodi per il monitoraggio delle prestazioni
  recordPerformanceMetric(metricName: string, duration: number, context?: string): void {
    this.info(`Performance metric: ${metricName}`, context, {
      duration,
      timestamp: new Date()
    });
  }

  recordUserAction(action: string, data?: any): void {
    this.info(`User action: ${action}`, 'user-action', data);
  }

  recordApiCall(endpoint: string, method: string, status: number, duration: number): void {
    this.info(`API call: ${method} ${endpoint}`, 'api-call', {
      status,
      duration,
      timestamp: new Date()
    });
  }
}