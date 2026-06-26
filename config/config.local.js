(function () {
  'use strict';

  window.ValoraConfig = Object.freeze({
    APP_VERSION: '8.6.8', RUNTIME_ENV: 'local', APP_PUBLIC_URL: 'http://127.0.0.1:8095', STORAGE_MODE: 'local', FIREBASE_ENABLED: false, FIREBASE_PLAN: 'none', ENABLE_CLOUD_FUNCTIONS: false, REQUIRE_AUTH_SERVER_VALIDATION: false, LOCAL_API_ENABLED: true, LOCAL_API_BASE_URL: '', EMAIL_TRANSPORT: 'local-outbox', WHATSAPP_TRANSPORT: 'manual', EXTERNAL_API_BASE_URL: '',
    COMMUNICATION_GATEWAY: { enabled: false, baseUrl: '', mode: 'disabled', sendResultOnSurveyCompleted: false, allowManualResend: false, timeoutMs: 15000 },
    observability: { enabled: true, consoleEnabled: true, consoleLevel: 'debug', persistLogs: true, remoteLogsEnabled: false, telegramEnabled: false, legacyTraceEnabled: true, maskSensitiveData: true, maxLocalLogs: 3000, environment: 'local' },
    FIREBASE_CONFIG: {}, STORE_KEY: 'valoraPulseFinal800'
  });
})();

// Fase 1 PostgreSQL: manter Firebase por padrão; use 'api' ou 'hybrid' para testes controlados.
window.VALORA_CONFIG = window.VALORA_CONFIG || {};
window.VALORA_CONFIG.DATA_PROVIDER = window.VALORA_CONFIG.DATA_PROVIDER || 'firebase';
window.VALORA_CONFIG.API_BASE_URL = window.VALORA_CONFIG.API_BASE_URL || 'https://api.valoragroup.mnsoft.com.br';
