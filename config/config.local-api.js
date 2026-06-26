(function () {
  'use strict';
  window.ValoraConfig = Object.freeze({
    APP_VERSION: '8.6.8', RUNTIME_ENV: 'local-api', APP_PUBLIC_URL: 'http://127.0.0.1:8095', STORAGE_MODE: 'api',
    DATA_PROVIDER: 'api', HYBRID_PRIMARY_PROVIDER: 'api', API_BASE_URL: 'http://127.0.0.1:5080', EXTERNAL_API_BASE_URL: 'http://127.0.0.1:5080',
    FIREBASE_ENABLED: true, FIREBASE_PLAN: 'spark', ENABLE_CLOUD_FUNCTIONS: false, REQUIRE_AUTH_SERVER_VALIDATION: false, LOCAL_API_ENABLED: true, LOCAL_API_BASE_URL: 'http://127.0.0.1:5080',
    PUBLIC_SURVEY_VALIDATION_PROVIDER: 'api', PUBLIC_SUBMISSION_PROVIDER: 'api', RESULT_PROVIDER: 'api', EMAIL_TRANSPORT: 'external-api', WHATSAPP_TRANSPORT: 'manual',
    COMMUNICATION_GATEWAY: { enabled: false, baseUrl: '', mode: 'disabled', sendResultOnSurveyCompleted: false, allowManualResend: false, timeoutMs: 20000 },
    observability: { enabled: true, consoleEnabled: true, consoleLevel: 'debug', persistLogs: true, remoteLogsEnabled: false, telegramEnabled: false, legacyTraceEnabled: true, maskSensitiveData: true, maxLocalLogs: 3000, environment: 'local-api' },
    FIREBASE_CONFIG: {}, STORE_KEY: 'valoraPulseFinal800'
  });
})();
