# Specifiche Progetto: Secretary AI Assistant (.NET 8 + llama.cpp) — v11

## 0. Fuori Scope (Esplicito)
-   *Generazione/Invio Fatture: La fatturazione avviene manualmente su **Fiscozen*. Il sistema non genera PDF fiscali, XML FatturaPA o invia nulla via SDI/email al cliente.
-   *Calendario, LinkedIn*: Fasi successive.
-   *Account plasmac e hexagon: L'implementazione del codice è ammessa, ma il **collegamento a queste due mailbox resta disattivato* finché non arriva l'autorizzazione esplicita dei rispettivi proprietari (tenant M365 di terzi).

## 1. Panoramica
Assistente personale per freelance su quattro aree:
1.  *Gestione Operativa Quotidiana*: Ore e task via Telegram con reminder proattivo.
2.  *Modulo Email*: Monitoraggio caselle Outlook/M365 tramite Microsoft Graph API, notifiche Telegram e bozze di risposta validate dall'utente.
3.  *Riconciliazione Mensile*: Confronto log giornalieri vs PDF cliente, riepilogo testuale per Fiscozen.
4.  *Modulo Guide (App Web)*: Applicazione HTML/JS ospitata su Vercel, sincronizzata con un repository GitHub esistente. Il flusso di aggiornamento prevede una fase di staging per garantire la sicurezza dei contenuti prima del deploy pubblico.

## 2. Stack Tecnologico
-   *Backend*: .NET 8 (Minimal APIs).
-   *Scheduling*: IHostedService per reminder giornaliero, rinnovo subscription Graph e nudge mensile.
-   *LLM Engine*: llama.cpp (http://localhost:8080/v1/chat/completions).
    -   *Modello*: Qwen3.6-35B-A3B-Q4_K_M.gguf.
    -   *Robustezza*: Riuso del motore di parsing JSON/tool-calling esistente (retry, gestione errori).
-   *Email Integration: **Microsoft Graph API*.
    -   Utilizzo di *Webhook* per notifiche push.
    -   Scrittura bozze native via createDraft.
    -   Invio mail tramite sendMail.
-   *Modulo Guide: Applicazione statica HTML/JS ospitata su **Vercel, con CI/CD automatico da repository **GitHub*.
    -   *Flusso di Deploy*: Push su branch staging → Verifica manuale → Merge su main → Deploy pubblico.
-   *Parsing PDF (Input)*: PdfPig.
-   *Database/Storage*: Google Sheets API v4 (DailyLogs, Todos, AuditLog, Clients, EmailQueue). File Markdown locali per la persistenza delle Guide.
-   *Interfaccia Utente*: Telegram Bot + Web App (Guide).

## 3. Architettura delle Cartelle

text
/src
├── Controllers/                 # Minimal API endpoints (Telegram Webhook, Graph Webhook, Health)
├── Services/
│   ├── LlmService.cs            # Wrapper HTTP a llama.cpp + motore parsing/tool esistente
│   ├── TelegramBot.cs           # Messaggi in entrata/uscita, comandi, approvazioni
│   ├── GraphAuthService.cs      # Gestione auth: app-only (tenant proprio) + delegata (tenant terzi)
│   ├── EmailMonitorService.cs   # Ricezione webhook Graph, notifica, generazione bozze
│   ├── PdfParser.cs             # Estrazione testo da PDF mensile cliente
│   ├── DailyEntryManager.cs     # CRUD inserimenti giornalieri su Sheets
│   ├── TodoQueryService.cs      # Interrogazione e filtraggio Todo
│   ├── MonthlyConsolidator.cs   # Confronto PDF cliente vs DailyLogs
│   ├── MonthlySummaryService.cs # Riepilogo testuale per Fiscozen (NO PDF, NO invio)
│   ├── GuideService.cs          # Lettura/scrittura file Markdown + gestione branch Git
│   └── SchedulerHostedService.cs# Job: reminder 18:00, rinnovo subscription Graph, nudge mensile
├── Models/
│   ├── Domain/                  # DailyLog, ProjectTodo, AuditLog, ClientInfo, EmailMessage, MailAccount
│   └── Dtos/
├── Config/                      # appsettings.json
└── Program.cs

/guides-app                       # Cartella esistente per l'app web delle guide (non da creare)
├── index.html                   # UI principale
├── style.css                    # Stili
├── app.js                       # Logica JS (fetch API, rendering Markdown)
└── vercel.json                  # Configurazione Vercel esistente


## 4. Modello Dati (Google Sheets)

### DailyLogs, Todos, AuditLog, Clients
Invariati rispetto alla v9.

### EmailQueue
| Colonna | Intestazione      | Tipo     | Descrizione                                             |
| ------- | ----------------- | -------- | ------------------------------------------------------- |
| A       | Id                | String   | ID univoco bozza                                        |
| B       | Account           | String   | Quale mailbox (pm-softwareautomation, plasmac, hexagon) |
| C       | ToAddress         | String   | Destinatario                                            |
| D       | Subject           | String   | Oggetto                                                 |
| E       | BodyDraft         | String   | Corpo bozza generata dal LLM                            |
| F       | Status            | Enum     | Pending, Approved, Rejected, Sent                       |
| G       | CreatedAt         | DateTime | Data creazione                                          |
| H       | OriginalMessageId | String   | ID mail originale (per thread reply via Graph)          |

### Guide (File Locali + App Web)
I file Markdown risiedono in /guides/ sul server. L'app web (/guides-app) li legge tramite un endpoint API protetto o direttamente da GitHub se la struttura lo permette.

## 5. Specifiche Funzionali

### A. Autenticazione Email (GraphAuthService)
Invariata rispetto alla v9. Due modalità distinte: App-Only per pm-softwareautomation.com (abilitato), Delegated per plasmac/hexagon (disabilitato).

### B. Modulo Email (EmailMonitorService)
Invariato rispetto alla v9. Webhook Graph, notifica Telegram, generazione bozze validate.

### C. Input Giornaliero (DailyEntryManager)
Invariato rispetto alla v7/v8. Validazione aggregata ore e politica di creazione clienti a conferma esplicita.

### D. Reminder Proattivo
Invariato rispetto alla v7/v8 — cron 18:00, Europe/Rome.

### E. Query Interattiva (TodoQueryService)
Invariato rispetto alla v7/v8.

### F. Riconciliazione Mensile (MonthlyConsolidator + MonthlySummaryService)
Invariato rispetto alla v7/v8. Output solo testuale per Fiscozen.

### G. Modulo Guide (App Web + Backend Integration)
-   *Architettura*: Applicazione statica HTML/JS ospitata su Vercel, sincronizzata con repository GitHub esistente.
-   *Consultazione*: L'utente accede all'app via browser per leggere le guide tecniche.
-   *Aggiornamento Assistito (Flusso Sicuro)*:
    1.  Tramite Telegram, l'utente richiede una modifica: "Aggiorna la guida Angular: ora uso Signals".
    2.  GuideService genera un *diff testuale* e lo mostra su Telegram per verifica.
    3.  L'utente conferma la modifica.
    4.  Il backend scrive il nuovo contenuto nel file Markdown locale (/guides/angular.md) e effettua un commit/push sul branch *staging*.
    5.  Vercel rebuilda l'app con i dati di staging (o il sistema notifica che la modifica è in staging).
    6.  *Pubblicazione*: L'utente deve eseguire manualmente il comando /publish [nome-guida] su Telegram per approvare il merge da staging a main.
    7.  Il backend effettua il merge e il push su main, innescando il deploy pubblico finale.
-   *Sicurezza*: Questo flusso evita la pubblicazione automatica di contenuti non verificati in produzione.

## 6. Configurazione (appsettings.json)

json
{
  "Logging": {
    "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" }
  },
  "AllowedHosts": "*",
  "GoogleSheets": {
    "SheetId": "ID_DEL_TUO_FOGLIO_GOOGLE",
    "DailyLogsSheetName": "DailyLogs",
    "TodosSheetName": "Todos",
    "AuditLogSheetName": "AuditLog",
    "ClientsSheetName": "Clients",
    "EmailQueueSheetName": "EmailQueue",
    "ServiceAccountJsonPath": "./keys/service-account.json"
  },
  "Guides": { 
    "BasePath": "/home/pm/guides",
    "GitHubRepo": "pm-softwareautomation/guides",
    "StagingBranch": "staging",
    "ProductionBranch": "main",
    "GitUserEmail": "${GIT_USER_EMAIL}",
    "GitUserName": "${GIT_USER_NAME}"
  },
  "VercelApp": {
    "BaseUrl": "https://guides-pm-softwareautomation.vercel.app"
  },
  "LLM": {
    "BaseUrl": "http://localhost:8080",
    "ModelName": "Qwen3.6-35B-A3B-Q4_K_M.gguf",
    "Temperature": 0.1,
    "MaxTokens": 4096,
    "TopP": 0.9,
    "RetryAttempts": 3
  },
  "Telegram": {
    "BotToken": "${TELEGRAM_BOT_TOKEN}",
    "WebhookUrl": "https://your-domain.com/api/telegram/webhook"
  },
  "GraphAccounts": [
    {
      "Name": "pm-softwareautomation",
      "AuthType": "AppOnly",
      "TenantId": "${GRAPH_PM_TENANT_ID}",
      "ClientId": "${GRAPH_PM_CLIENT_ID}",
      "ClientSecret": "${GRAPH_PM_CLIENT_SECRET}",
      "Enabled": true
    },
    {
      "Name": "plasmac",
      "AuthType": "Delegated",
      "Enabled": false,
      "_note": "Attivare solo dopo autorizzazione esplicita del proprietario"
    },
    {
      "Name": "hexagon",
      "AuthType": "Delegated",
      "Enabled": false,
      "_note": "Attivare solo dopo autorizzazione esplicita del proprietario"
    }
  ],
  "Reminder": { "Enabled": true, "Time": "18:00", "TimeZone": "Europe/Rome" }
}


## 7. Flussi di Esempio

### Scenario 1 — Aggiornamento Guida Sicuro (Staging → Production)
1.  Utente → Telegram: "Aggiorna la guida Angular: nuovi progetti usano Signals standalone."
2.  GuideService propone il diff sul file /guides/angular.md.
3.  Utente conferma su Telegram.
4.  Backend scrive il file e effettua git push al branch staging.
5.  Vercel rebuilda l'app con i dati di staging (visibile solo tramite preview URL o flag interno).
6.  Telegram: "✅ Modifica applicata in staging. Esegui /publish angular per renderla pubblica."
7.  Utente → Telegram: /publish angular.
8.  Backend effettua il merge staging -> main e push. Vercel deploya la versione live.

### Scenario 2 — Consultazione Guide
1.  Utente apre https://guides-pm-softwareautomation.vercel.app.
2.  L'app JS fa fetch degli indici delle guide (tramite API backend o direttamente da GitHub se pubblico).
3.  Visualizzazione del contenuto Markdown renderizzato in HTML.

### Scenario 3 — Notifica Email (Account Proprio)
Invariato rispetto alla v9.

## 8. Requisiti Non Funzionali
-   *Performance query*: Filtri su Sheets ottimizzati per volumi crescenti.
-   *Sicurezza*: Client secret Graph, service account Google, token Telegram in variabili d'ambiente/secret manager, mai committati. Credenziali Git protette.
-   *Audit*: AuditLog append-only.
-   *Resilienza auth*: Fallimento di token/subscription su un account notificato via Telegram, mai silenzioso; nessun retry infinito.
-   *Nessuna azione fiscale automatica*.

## 9. Roadmap Implementativa
1.  Setup .NET 8, appsettings.json, avvio llama.cpp.
2.  Creazione fogli Google (DailyLogs, Todos, AuditLog, Clients, EmailQueue) e Service Account.
3.  GoogleSheetsService multi-foglio.
4.  GraphAuthService — *solo modalità app-only* per pm-softwareautomation.com.
5.  EmailMonitorService con webhook Graph + rinnovo subscription automatico.
6.  Verifica configurazione esistente repository GitHub e Vercel (nessuna nuova infrastruttura).
7.  Implementazione GuideService con logica di scrittura file, branch staging e comando /publish.
8.  ReminderHostedService + Telegram base.
9.  DailyEntryManager, TodoQueryService.
10. MonthlyConsolidator + MonthlySummaryService.
11. Testing end-to-end: flusso guida (Telegram -> Git Staging -> Publish -> Vercel Live), flusso email, flusso ore.