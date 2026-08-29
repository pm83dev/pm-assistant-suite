# Environment Configuration

Questo progetto utilizza un sistema di environment basato su Vite per gestire le configurazioni di build in modo diverso tra sviluppo e produzione.

## Struttura

```
src/environments/
├── index.ts                     # Loader dinamico (importa da qui)
├── environment.ts               # Fallback per configurazioni personalizzate
├── environment.development.ts   # Configurazione per sviluppo (default)
├── environment.production.ts    # Configurazione per produzione
└── README.md                    # Questo file
```

## Come funziona

1. Il file `index.ts` usa `import.meta.env.MODE` (iniezione automatica di Vite) per determinare la modalità di build
2. In base alla modalità, importa il file di environment corretto
3. I servizi importano l'environment da `@environments/index` o dal percorso relativo

## Utilizzo nei servizi

```typescript
import { environment } from '../../../environments/index';

// Usa le variabili di environment
const apiUrl = environment.apiOreTracking;
const pmUrl = environment.apiPmAssistant;
const wsUrl = environment.wsUrl;
const isProd = environment.production;
```

## Variabili disponibili

| Variabile | Descrizione | Development | Production |
|-----------|-------------|-------------|------------|
| `production` | Flag modalità build | `false` | `true` |
| `apiOreTracking` | Base URL per ore-tracking API | `/api` | `/api` |
| `apiPmAssistant` | Base URL per pm-assistant API | `http://localhost:5000` | `https://pm-assistant.example.com` |
| `jwt.issuer` | JWT issuer | `https://example.com` | `https://example.com` |
| `jwt.audience` | JWT audience | `https://example.com` | `https://example.com` |
| `jwt.key` | JWT secret key | `dev-secret-key` | `prod-secret-key` |
| `wsUrl` | WebSocket endpoint | `ws://localhost:5000` | `wss://pm-assistant.example.com` |

## Comandi di build

```bash
# Sviluppo (usa environment.development.ts)
npm run start:dev
npm run build:dev

# Produzione (usa environment.production.ts)
npm run start:prod
npm run build:prod
```

## Come aggiungere nuove variabili

1. Aggiungi la variabile in **tutti e tre** i file:
   - `environment.development.ts`
   - `environment.production.ts`
   - `environment.ts` (fallback)

2. Usa la variabile nei servizi importando:
   ```typescript
   import { environment } from '../../../environments/index';
   ```

## Aggiungere un nuovo ambiente personalizzato

Per aggiungere un ambiente personalizzato (es. `staging`):

1. Crea `environment.staging.ts`
2. Aggiungi un case nel `switch` di `index.ts`
3. Build con: `vite build --mode staging`

## Note importanti

- **Non mettere mai credenziali reali nei file di environment**
- I file di environment sono inclusi nel commit (sono codice, non segreti)
- Per segreti reali, usa variabili d'ambiente del server o un secret manager
- Il proxy per lo sviluppo è configurato in `proxy.conf.json`
