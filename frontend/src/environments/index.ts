/**
 * Environment loader
 *
 * Questo modulo carica l'environment corretto in base alla modalità di build:
 * - `development` → environment.development.ts
 * - `production` → environment.production.ts
 * - altri casi → environment.ts (fallback)
 *
 * Vite inietta automaticamente `import.meta.env.MODE` al build time.
 */

// Determina quale file di environment caricare
const envMode = import.meta.env.MODE || 'development';

let env: typeof import('./environment.ts').environment;

switch (envMode) {
  case 'production':
    env = (await import('./environment.production.ts')).environment;
    break;
  case 'development':
  default:
    env = (await import('./environment.development.ts')).environment;
    break;
}

export { env as environment };
