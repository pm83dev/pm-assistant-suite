# Piano di unificazione — Assistente unico (ore + agente locale stile Claude Desktop)

Data: 2026-08-17 (aggiornato lo stesso giorno dopo il rename dei progetti)
Decisioni utente raccolte in sessione:
- Backend ore definitivo: **ore-tracking** (ex `TestAgente`: `Api` + SQLite + `Web` Angular), non Google Sheets.
- L'agente "stile Claude Desktop" non deve limitarsi al coding: deve fare anche supporto/creazione documenti, istruzioni sul sistema operativo, assistenza generica.
- Repo Git: unificato in un unico repo con history pulita, separato dai vecchi `pm-assistant.git`/`test-agente.git`.
- Nomi sistemati: `TestAgente/` → `ore-tracking/`, `TestApi/` → `ore-tracking/Api/` (namespace `OreTracking.Api`, assembly `OreTracking.Api`), `TestFrontend/` → `ore-tracking/Web/` (progetto Angular `ore-web`). Creata `pm-assistant-suite.slnx` in radice con entrambi i progetti .NET.

## 1. Stato attuale verificato

Nella cartella ci sono **due progetti distinti**, non uno solo con leftover:

### 1.1 `pm-assistant/` (unico `.csproj`, ma contiene due sistemi non collegati tra loro)

- **Sistema A — "Secretary AI Assistant" (attivo, è quello che gira con `dotnet run`)**
  `Program.cs` è un host ASP.NET Core (`WebApplication`) che avvia:
  - Bot Telegram (`TelegramBotService`, `AssistantAgentService`) — oggi usa `LlamaClient` per semplice chat completion, **senza tool calling**.
  - Webhook Microsoft Graph (email) — `GraphAuthService`, `Controllers/GraphWebhookController.cs`.
  - Ore/Todo/Riconciliazione su **Google Sheets** — `DailyEntryManager`, `TodoQueryService`, `MonthlyConsolidator`, `MonthlySummaryService`, `GoogleSheetsService`.
  - Modulo Guide (staging → publish via Git/Vercel) — `GuideService`.
  - Scheduler (reminder 18:00, rinnovo subscription Graph) — `SchedulerHostedService`.
  - Questo corrisponde a `Specs.md` ("Secretary AI Assistant v11").

- **Sistema B — "Local Code Agent" (presente nel codice ma NON collegato, di fatto morto)**
  File nella stessa cartella: `ToolDispatcher.cs`, `FileSystemTools.cs`, `TerminalTools.cs`, `AgentTools.cs`, `GitTools.cs`, `WebTools.cs`, `WorkSpacecontext.cs`, `ConsoleHelper.cs`, `SessionManager.cs`.
  Sono compilati (il `.csproj` include tutti i `.cs` per default, esclude solo `AgentWorkspace/`), ma **nessuna riga di `Program.cs` li istanzia o li chiama**. È il vecchio "Local Code Agent" descritto nel `README.md` di `pm-assistant/` e in `CLAUDE.md`, mai ricollegato dopo il merge nel progetto Secretary.
  Questo è il pezzo che serve per fare da "Claude Desktop locale": legge/scrive file, esegue comandi PowerShell (sandbox su `WorkspaceContext`), fa `git`, cerca sul web (`WebTools`).

### 1.2 `ore-tracking/` (ex `TestAgente/`, repo separato dal punto di vista logico ma ora nello stesso repo Git unificato)

- `Api/` (ex `TestApi/`) — ASP.NET Core Web API .NET 8, EF Core + SQLite (`app.db`), namespace `OreTracking.Api`, `TimeTrackingController` con CRUD completo per **Clienti, Progetti, OreLavorate, Note** sotto `/api/time-tracking/*`. Swagger su `/swagger`. Porta default `http://localhost:5108` (profilo `http` in `launchSettings.json`).
- `Web/` (ex `TestFrontend/`) — Angular 20 (Vite), progetto `ore-web`, pagine Dashboard/Clienti/Progetti/Ore/Note, consuma l'API sopra.
- Ha il proprio `AGENTS.md` che conferma: è un'app reale di gestione ore, generata in precedenza usando l'agente su questo stesso workspace (nome coincideva con `pm-assistant/AgentWorkspace/TestAgenteBackend`).

**Conclusione stato attuale**: non serve "unire codice C# in un solo progetto". Servono due collegamenti mancanti:
1. Ricollegare il Sistema B (tool ReAct) dentro `pm-assistant`, così l'assistente può agire (file, terminale, git, web) e non solo chattare.
2. Far parlare `pm-assistant` con `ore-tracking/Api` per le operazioni di ore/clienti/progetti, al posto di Google Sheets.

## 2. Nota di sicurezza (fuori scope, da correggere a parte)

`pm-assistant/appsettings.json` contiene il **token del bot Telegram in chiaro, committato nel repo** (`Telegram:BotToken`). Andrebbe rigenerato e spostato in variabile d'ambiente/secret manager come previsto da `Specs.md` §8. Non l'ho toccato: segnalo solo, decidilo tu.

## 3. Architettura target

Un unico processo `pm-assistant` (ASP.NET host) che espone **un solo punto di ingresso "agente"** (nuovo endpoint di chat con tool-calling, riusato sia da Telegram sia da console/API), con un set di tool ampio:

```
Utente (Telegram / console CLI / eventuale UI web)
        │
        ▼
 AssistantAgentService (nuovo: usa ToolDispatcher, non solo LlamaClient)
        │
        ▼
 ToolDispatcher ── FileSystemTools   (file, documenti locali)
               ├── TerminalTools    (comandi OS/PowerShell, sandbox)
               ├── GitTools
               ├── WebTools         (ricerca web)
               ├── AgentTools       (workspace tree)
               └── OreTrackingTools (NUOVO: HTTP client verso ore-tracking/Api)
                                        │
                                        ▼
                              ore-tracking/Api — SQLite
                              Clienti / Progetti / OreLavorate / Note
                              (resta un processo separato, con la sua UI Angular in ore-tracking/Web)
```

Punti chiave della scelta:
- **`ore-tracking/Api` resta un servizio a sé stante**, non viene fuso dentro `pm-assistant`. Motivi: ha già una UI Angular funzionante, un proprio DB EF/SQLite — fonderlo dentro l'host ASP.NET di `pm-assistant` vorrebbe dire risolvere conflitti di porte/EF/migrazioni senza alcun vantaggio reale. `pm-assistant` gli parla via HTTP, come farebbe qualunque altro client (esattamente come fa già `ore-tracking/Web`).
- Il bot Telegram e (nuova) la CLI console diventano due "canali" che parlano allo **stesso** `AssistantAgentService` con tool calling, così l'utente ha un solo assistente, non due comportamenti diversi a seconda del canale.
- Google Sheets (`GoogleSheetsService`, `DailyEntryManager`) va **disattivato/deprecato** come sorgente ore, ma **non cancellato subito**: `MonthlyConsolidator`/`MonthlySummaryService` (riconciliazione PDF cliente vs log per Fiscozen) oggi leggono da Sheets e vanno riscritti per leggere da `ore-tracking/Api` prima di poter spegnere Sheets del tutto.

## 4. Piano d'azione, passo per passo

1. **Riattivare il Sistema B dentro `pm-assistant`**
   - In `AssistantAgentService.cs`, sostituire la chiamata diretta a `LlamaClient` per il completion con un ciclo ReAct che usa `ToolDispatcher` (già esistente, oggi solo istanziato dal vecchio `Program.cs` del README, non da quello attuale).
   - Decidere la workspace root per `WorkspaceContext` (sandbox file/terminale): proporrei `C:\DevAgentPM` o una cartella dedicata tipo `C:\PmAssistantWorkspace`, non l'intera macchina.
   - Registrare `ToolDispatcher` in DI (`builder.Services.AddSingleton<ToolDispatcher>()`), iniettarlo in `AssistantAgentService`.

2. **Aggiungere `OreTrackingTools.cs`**
   - Nuova classe tool (stesso pattern di `FileSystemTools`/`GitTools`) che wrappa `ore-tracking/Api` via `HttpClient`: `list_clienti`, `create_cliente`, `list_progetti`, `create_progetto`, `log_ora_lavorata`, `list_ore_by_progetto`, `add_nota`, ecc. — un tool per ciascun endpoint utile di `TimeTrackingController`.
   - Aggiungere in `appsettings.json` una sezione tipo:
     ```json
     "TimeTrackingApi": { "BaseUrl": "http://localhost:5108" }
     ```
   - Registrare il tool in `ToolDispatcher` accanto a `_fs`, `_terminal`, `_agent`, `_git`, `_web`.

3. **Spegnere gradualmente il percorso Google Sheets per le ore**
   - Lasciare `GoogleSheetsService`/`DailyEntryManager` nel codice ma toglierli dal flusso "utente scrive ore" (che ora passa da `OreTrackingTools` → `TestApi`).
   - Riscrivere `MonthlyConsolidator`/`MonthlySummaryService` per leggere le ore da `TestApi` invece che da Sheets, prima di poter disattivare Sheets del tutto (serve ancora per confronto con il PDF cliente, §F di `Specs.md`).

4. **Riattivare la modalità "Claude Desktop locale"**
   - Aggiungere una modalità CLI: `dotnet run -- --cli` (o comando REPL separato) che riusa `ConsoleHelper`/`SessionManager` già presenti per un loop interattivo da terminale, con lo stesso `ToolDispatcher` — per uso locale rapido senza passare da Telegram.
   - Estendere i tool oltre al coding, per il caso d'uso "assistente generico" richiesto: `FileSystemTools`/`TerminalTools` già coprono creazione file, esecuzione comandi OS, ricerca; se serve generare documenti Office (docx/xlsx) o PDF in output (non solo parsing, che oggi c'è solo con `PdfPig` in lettura), va aggiunta una libreria di generazione (es. `DocumentFormat.OpenXml` o `QuestPDF`) e un nuovo `DocumentTools.cs`.

5. **`ore-tracking/` avviato separatamente ma nello stesso repo**
   - `ore-tracking/Api` (`dotnet run`) e `ore-tracking/Web` (`ng serve`) restano due processi distinti da `pm-assistant`, ma ora vivono nello stesso repo Git e nella stessa `pm-assistant-suite.slnx` (solo i due progetti .NET sono in solution; Angular si builda/avvia a parte). `pm-assistant` consuma `ore-tracking/Api` solo via HTTP.
   - Se in futuro si vuole un unico comando di avvio, si può aggiungere uno script (`start-all.ps1`) che lancia i tre processi (`pm-assistant`, `ore-tracking/Api`, `ore-tracking/Web`) in parallelo — non prioritario ora. Configurazioni di debug/task già pronte in `.vscode/launch.json` e `.vscode/tasks.json` alla radice.

6. **Test end-to-end da fare dopo l'implementazione**
   - Da Telegram: "segna 3 ore sul progetto X per il cliente Y" → verificare che arrivi una riga in `ore-tracking/Api`/SQLite.
   - Da CLI locale: chiedere di leggere/modificare un file nel workspace sandbox, eseguire un comando PowerShell, verificare che il path traversal resti bloccato.
   - Verificare che il blocco comandi distruttivi di `TerminalTools` (rm -rf, format, ecc.) sia ancora attivo dopo il refactor.
   - Riconciliazione mensile: verificare che `MonthlyConsolidator` produca lo stesso tipo di output testuale per Fiscozen, ma leggendo da `ore-tracking/Api`.

## 5. Decisioni ancora aperte (da confermare quando si arriva al punto)

- Formato documenti da generare (solo Markdown/testo, o anche docx/pdf veri) — impatta quale libreria aggiungere al punto 4.
- Se CLI locale e bot Telegram devono condividere la stessa sessione/cronologia o restare conversazioni indipendenti.
- Se e quando disattivare del tutto Google Sheets, o tenerlo come backup/audit trail parallelo.
- Rotazione del token Telegram esposto nel repo (vedi §2).
