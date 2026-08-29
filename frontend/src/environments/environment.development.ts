export const environment = {
  production: false,
  // API per ore-tracking (API REST .NET 8 standalone)
  apiOreTracking: '/api',
  // API per pm-assistant (Telegram bot, LLM, ecc.)
  apiPmAssistant: 'http://localhost:5000',
  // JWT configuration
  jwt: {
    issuer: 'https://example.com',
    audience: 'https://example.com',
    key: 'dev-secret-key',
  },
  // WebSocket (futuro)
  wsUrl: 'ws://localhost:5000',
};
