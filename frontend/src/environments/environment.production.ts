export const environment = {
  production: true,
  // API per ore-tracking (API REST .NET 8 standalone)
  apiOreTracking: '/api',
  // API per pm-assistant (Telegram bot, LLM, ecc.)
  apiPmAssistant: 'https://pm-assistant.example.com',
  // JWT configuration
  jwt: {
    issuer: 'https://example.com',
    audience: 'https://example.com',
    key: 'prod-secret-key',
  },
  // WebSocket (futuro)
  wsUrl: 'wss://pm-assistant.example.com',
};
