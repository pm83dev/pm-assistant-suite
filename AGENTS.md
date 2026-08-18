# AGENTS.md

## Scopo del progetto
Suite di strumenti per assistenza project management, inclusa:
- **pm-assistant**: applicazione .NET 8 con integrazioni a Google Sheets, Microsoft Graph, Telegram, PDF parsing e generazione Word.
- **ore-tracking**: API REST in .NET 8 + frontend Angular 20 per tracciamento ore e gestione attività.

## Stack e versioni reali
- **Backend principale (pm-assistant)**: .NET 8.0, C#, ASP.NET Core (hosting self-contained)
- **Backend ore-tracking**: .NET 8.0, Entity Framework Core (SQLite), Swashbuckle (Swagger)
- **Frontend**: Angular 20.3, TypeScript ~5.9, Vite build
- **Dipendenze chiave**:
  - Google.Apis.Sheets.v4 (1.68.0.3525)
  - Microsoft.Graph (5.60.0)
  - Telegram.Bot (22.10.2)
  - PdfPig (0.1.8)
  - LibGit2Sharp (0.30.0)
  - Microsoft.AspNetCore.OpenApi (8.0.28)
  - Swashbuckle.AspNetCore (6.6.2)

## Comandi Build/Run

### pm-assistant
```bash
cd pm-assistant
dotnet build
dotnet run
```

### ore-tracking (API + frontend)
```bash
cd ore-tracking/Api
dotnet build
dotnet run
```
Swagger disponibile su `/swagger`.

### Frontend Angular (standalone)
```bash
cd frontend
npm install
npm run dev          # avvia dev server (Vite, port 5173)
npm run build        # produce output in dist/
```

## Struttura cartelle principale
- `pm-assistant/` → applicazione principale (.NET 8)
- `ore-tracking/Api/` → API REST per tracciamento ore (.NET 8)
- `ore-tracking/Web/` → frontend Angular (se presente)
- `frontend/` → frontend Angular separato (eventuale)

## Note operative per agente AI
- Il progetto usa cartelle separate per logica distinta.
- Verificare sempre il `*.csproj` di riferimento prima di eseguire comandi `dotnet`.
- Per il frontend Angular, controllare `angular.json` o `vite.config.ts` per porte e paths.
- I file sensibili (es. chiavi) sono esclusi dal commit (`keys/*`, `.env`, ecc.).
