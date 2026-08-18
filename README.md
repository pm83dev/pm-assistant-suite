# Secretary AI Assistant

Assistente personale per freelance su quattro aree principali:

1. **Gestione Operativa Quotidiana**: Ore e task via Telegram con reminder proattivo
2. **Modulo Email**: Monitoraggio caselle Outlook/M365 tramite Microsoft Graph API, notifiche Telegram e bozze di risposta validate dall'utente
3. **Riconciliazione Mensile**: Confronto log giornalieri vs PDF cliente, riepilogo testuale per Fiscozen
4. **Modulo Guide (App Web)**: Applicazione HTML/JS ospitata su Vercel, sincronizzata con repository GitHub

---

## Stack Tecnologico

- **Backend**: .NET 8 (Minimal APIs)
- **LLM Engine**: llama.cpp (`http://localhost:8080/v1/chat/completions`)
  - Modello: `Qwen3.6-35B-A3B-Q4_K_M.gguf`
- **Database/Storage**: Google Sheets API v4
- **Email Integration**: Microsoft Graph API
- **Interfaccia Utente**: Telegram Bot + Web App (Guide)

---

## Prerequisiti

- .NET 8 SDK
- llama-server.exe (con modello Qwen3.6-35B-A3B-Q4_K_M.gguf)
- Google Cloud Service Account con accesso al foglio Google Sheets
- Token Telegram Bot
- Credenziali Microsoft Graph (opzionale, per modulo email)

---

## Configurazione

1. Copiare `pm-assistant/appsettings.Example.json` in `pm-assistant/appsettings.json`
2. Modificare i seguenti valori in `appsettings.json`:

```json
{
  "LLM": {
    "BaseUrl": "http://localhost:8080",
    "ModelName": "Qwen3.6-35B-A3B-Q4_K_M.gguf"
  },
  "GoogleSheets": {
    "SheetId": "ID_DEL_TUO_FOGLIO_GOOGLE",
    "ServiceAccountJsonPath": "./keys/service-account.json"
  },
  "Telegram": {
    "BotToken": "IL_TUO_BOT_TOKEN"
  },
  "GraphAccounts": [
    {
      "Name": "pm-softwareautomation",
      "AuthType": "AppOnly",
      "TenantId": "TENANT_ID",
      "ClientId": "CLIENT_ID",
      "ClientSecret": "CLIENT_SECRET",
      "Enabled": true
    }
  ]
}
```

---

## Avvio

### 1. Avviare llama-server (terminale 1)

```powershell
cd C:\llamacpp
.\llama-server.exe -m Qwen3.6-35B-A3B-Q4_K_M.gguf --jinja -ngl 0 -c 4096 --parallel 1 --host 0.0.0.0 --port 8080
```

### 2. Avviare l'applicazione (terminale 2)

```powershell
cd pm-assistant
dotnet run
```

L'applicazione sarà disponibile su `http://localhost:5000`.

---

## Endpoints API

| Endpoint                       | Metodo | Descrizione                 |
| ------------------------------ | ------ | --------------------------- |
| `/api/health`                  | GET    | Health check                |
| `/api/daily-logs`              | POST   | Registra ore giornaliere    |
| `/api/daily-logs?year=&month=` | GET    | Lista ore per mese          |
| `/api/todos`                   | POST   | Aggiungi task               |
| `/api/todos`                   | GET    | Lista tasks                 |
| `/api/reconcile?year=&month=`  | GET    | Riconciliazione mensile     |
| `/api/summary?year=&month=`    | GET    | Riepilogo Fiscozen          |
| `/api/guides`                  | GET    | Lista guide                 |
| `/api/guides/{name}`           | GET    | Leggi guida                 |
| `/api/guides/{name}/preview`   | POST   | Anteprima modifica guida    |
| `/api/guides/{name}/apply`     | POST   | Applica modifica in staging |
| `/api/guides/{name}/publish`   | POST   | Pubblica guida su main      |
| `/api/chat`                    | POST   | Invia messaggio all'agente  |

---

## Comandi Telegram

| Comando                                     | Descrizione                           |
| ------------------------------------------- | ------------------------------------- |
| `/start`                                    | Benvenuto e lista comandi disponibili |
| `/log <data> <cliente> <ore> <descrizione>` | Registra ore giornaliere              |
| `/logs [anno/mese]`                         | Elenca le attività registrate         |
| `/todo <task>`                              | Aggiungi un task                      |
| `/todos`                                    | Lista tasks                           |
| `/reconcile <anno>/<mese>`                  | Riconciliazione mensile               |
| `/publish <guida>`                          | Pubblica una guida da staging a main  |

---

## Struttura del Progetto

```
pm-assistant-suite/
├── pm-assistant/              # Backend principale (.NET 8)
│   ├── Services/             # Servizi business (Telegram, LLM, Sheets, etc.)
│   ├── Models/               # DTOs e modelli dominio
│   ├── AgentWorkspace/       # Workspace per l'agente locale
│   ├── Program.cs            # Configurazione e routing API
│   └── appsettings.json      # Configurazione
├── ore-tracking/             # Modulo tracking ore (API separata)
├── guides-app/               # App web per consultazione guide (Vercel)
└── Specs.md                  # Specifiche tecniche complete
```

---

## Flusso di Lavoro: Aggiornamento Guida

1. Utente richiede modifica via Telegram: `"Aggiorna la guida Angular: ora uso Signals"`
2. Backend propone il diff sul file Markdown
3. Utente conferma su Telegram
4. Backend scrive il file e effettua push sul branch `staging`
5. Vercel rebuilda l'app con i dati di staging (preview URL)
6. Utente esegue `/publish angular` su Telegram
7. Backend effettua merge `staging -> main` e push
8. Vercel deploya la versione live

---

## Note

- Il modulo email è disabilitato per account di terze parti (`plasmac`, `hexagon`) finché non arriva autorizzazione esplicita
- Nessuna azione fiscale automatica: fatturazione manuale su Fiscozen
- Credenziali (token, secret) devono essere impostate tramite variabili d'ambiente o file `appsettings.json` locale, mai committate su Git

---

## License

MIT
