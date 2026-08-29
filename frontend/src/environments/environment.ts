// This file serves as a fallback for any other build configuration.
// For development builds, use environment.development.ts
// For production builds, use environment.production.ts
// Vite swaps the correct file at build time via the 'define' plugin.

export const environment = {
  production: false,
  apiOreTracking: '/api',
  apiPmAssistant: 'http://localhost:5000',
  jwt: {
    issuer: 'https://example.com',
    audience: 'https://example.com',
    key: 'default-key',
  },
  wsUrl: 'ws://localhost:5000',
};
