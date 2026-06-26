(function () {
  'use strict';

  window.ValoraConfig = Object.freeze({
    APP_VERSION: '8.6.8', RUNTIME_ENV: 'local-firebase', APP_PUBLIC_URL: 'http://127.0.0.1:8095', STORAGE_MODE: 'firebase',
    DATA_PROVIDER: 'firebase',
    API_BASE_URL: 'https://api.valoragroup.mnsoft.com.br', FIREBASE_ENABLED: true, FIREBASE_PLAN: 'spark', ENABLE_CLOUD_FUNCTIONS: false, REQUIRE_AUTH_SERVER_VALIDATION: false, LOCAL_API_ENABLED: false, LOCAL_API_BASE_URL: '', PUBLIC_SUBMISSION_PROVIDER: 'external-api',
    EMAIL_TRANSPORT: 'external-api', WHATSAPP_TRANSPORT: 'manual', EXTERNAL_API_BASE_URL: 'http://127.0.0.1:8097',
    COMMUNICATION_GATEWAY: { enabled: true, baseUrl: 'http://127.0.0.1:8097', mode: 'server-validated', sendResultOnSurveyCompleted: true, allowManualResend: true, timeoutMs: 20000 },
    observability: { enabled: true, consoleEnabled: true, consoleLevel: 'debug', persistLogs: true, remoteLogsEnabled: false, telegramEnabled: false, legacyTraceEnabled: true, maskSensitiveData: true, maxLocalLogs: 3000, environment: 'local-firebase' },
    FIREBASE_CONFIG: { apiKey: 'AIzaSyAcPTvJGCSVBYncINSTlxU1cfCR92_mhkU', authDomain: 'gestordepesquisa.firebaseapp.com', projectId: 'gestordepesquisa', storageBucket: 'gestordepesquisa.firebasestorage.app', messagingSenderId: '319806178218', appId: '1:319806178218:web:e8a0c3f39825e6d9d4a1d3', measurementId: 'G-KP0VV5MSHJ' },
    STORE_KEY: 'valoraPulseFinal800'
  });
})();
