(function () {
  'use strict';

  window.ValoraConfig = Object.freeze({
    APP_VERSION: '8.6.8',
    RUNTIME_ENV: 'production',
    APP_PUBLIC_URL: 'https://valoragroup.mnsoft.com.br',
    STORAGE_MODE: 'firebase',
    DATA_PROVIDER: 'firebase',
    ALLOW_API_PRODUCTION_CUTOVER: false,
    HYBRID_PRIMARY_PROVIDER: 'firebase',
    API_TIMEOUT_MS: 20000,
    API_BASE_URL: 'https://api.valoragroup.mnsoft.com.br',
    FIREBASE_ENABLED: true,
    FIREBASE_PLAN: 'spark',
    ENABLE_CLOUD_FUNCTIONS: false,
    REQUIRE_AUTH_SERVER_VALIDATION: false,
    LOCAL_API_ENABLED: false,
    LOCAL_API_BASE_URL: '',
    PUBLIC_SUBMISSION_PROVIDER: 'external-api', PUBLIC_SURVEY_VALIDATION_PROVIDER: 'external-api', RESULT_PROVIDER: 'external-api',
    EMAIL_TRANSPORT: 'external-api',
    WHATSAPP_TRANSPORT: 'manual',
    WHATSAPP_CONTACT_URL: 'https://wa.me/5591992545353?text=Ol%C3%A1%2C%20quero%20conhecer%20os%20planos%20completos%20da%20Valora.',
    FREE_SURVEY_LINK_EXPIRATION_DAYS: 3650,
    EXTERNAL_API_BASE_URL: 'https://api.valoragroup.mnsoft.com.br',
    COMMUNICATION_GATEWAY: {
      enabled: true,
      baseUrl: 'https://api.valoragroup.mnsoft.com.br',
      mode: 'server-validated',
      sendResultOnSurveyCompleted: true,
      allowManualResend: true,
      timeoutMs: 20000
    },
    observability: { enabled: true, consoleEnabled: false, consoleLevel: 'warn', persistLogs: true, remoteLogsEnabled: false, telegramEnabled: false, legacyTraceEnabled: false, maskSensitiveData: true, maxLocalLogs: 3000, environment: 'production' },
    FIREBASE_CONFIG: {
      apiKey: 'AIzaSyAcPTvJGCSVBYncINSTlxU1cfCR92_mhkU',
      authDomain: 'gestordepesquisa.firebaseapp.com',
      projectId: 'gestordepesquisa',
      storageBucket: 'gestordepesquisa.firebasestorage.app',
      messagingSenderId: '319806178218',
      appId: '1:319806178218:web:e8a0c3f39825e6d9d4a1d3',
      measurementId: 'G-KP0VV5MSHJ'
    },
    STORE_KEY: 'valoraPulseFinal800'
  });
})();
