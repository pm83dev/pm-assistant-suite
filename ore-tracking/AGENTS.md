# AGENTS.md

## Scopo del progetto
Workspace contenente un'API REST in C# (.NET 8) e un frontend Angular (v20) per la **gestione attività e tracciamento ore**.

## Stack e versioni reali
- **Backend**: .NET 8.0, ASP.NET Core Web API, OpenApi/Swashbuckle
- **Frontend**: Angular 20.3, TypeScript ~5.9, Vite build
- **Dipendenze Backend**: Microsoft.AspNetCore.OpenApi (8.0.28), Swashbuckle.AspNetCore (6.6.2)

## Comandi Build/Run
### Backend (TestApi)
```bash
cd TestApi
dotnet build
dotnet run
```

### Frontend (TestFrontend)
```bash
cd TestFrontend
npm install
ng serve          # avvia dev server (port 4200)
ng build          # produce output in dist/my-app/
```

## Struttura cartelle principale
- `TestApi/` → API REST per Time Tracking (Program.cs, TimeTrackingController.cs, TimeTrackingModels.cs, DataContext.cs, Services.cs, IDataRepository.cs)
- `TestFrontend/` → Angular app (src/app/, package.json, angular.json)
- File root: AGENTS.md

## Note operative per agente AI
### Backend
- L'API espone `/api/time-tracking` con CRUD per: **Clienti**, **Progetti**, **OreLavorate**, **Note**
- SQLite come database (app.db)
- CORS abilitato per sviluppo
- Swagger disponibile su `/swagger`
- **RIMOSSO**: DataModel, DataController e dati fittizi di test

### Frontend
- Il frontend è focalizzato sulla **gestione attività e tracciamento ore**
- Pagine disponibili: Dashboard, Clienti, Progetti, Ore, Note
- TimeTrackingService gestisce tutte le chiamate API
- **RIMOSSO**: DataService (dati fittizi), pagine Home/About/Contact
- Routes: `/dashboard`, `/clienti`, `/progetti`, `/ore`, `/note`

### Entità dominio
- **Cliente**: Id, Nome, Email, Telefono, Indirizzo
- **Progetto**: Id, Nome, Descrizione, ClienteId (FK)
- **OraLavorata**: Id, Data, Ore, Descrizione, ProgettoId (FK)
- **Nota**: Id, DataCreazione, Contenuto, Titolo, ProgettoId (FK)
