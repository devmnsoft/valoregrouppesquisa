(function () {
  'use strict';

  window.ValoraConfig = Object.freeze({
    APP_VERSION: '8.8.0',
    RUNTIME_ENV: 'production',
    APP_PUBLIC_URL: 'https://valoragroup.mnsoft.com.br',
    STORAGE_MODE: 'firebase',
    DATA_PROVIDER: 'firebase',
    ALLOW_API_PRODUCTION_CUTOVER: false,
    HYBRID_PRIMARY_PROVIDER: 'firebase',
    API_TIMEOUT_MS: 20000,
    API_BASE_URL: 'https://api.valoragroup.mnsoft.com.br',
    FIREBASE_ENABLED: true,
    FIREBASE_PLAN: 'blaze',
    ENABLE_CLOUD_FUNCTIONS: true,
    REQUIRE_AUTH_SERVER_VALIDATION: false,
    LOCAL_API_ENABLED: false,
    LOCAL_API_BASE_URL: '',
    PUBLIC_SUBMISSION_PROVIDER: 'auto', PUBLIC_SURVEY_VALIDATION_PROVIDER: 'auto', RESULT_PROVIDER: 'auto',
    EMAIL_TRANSPORT: 'auto',
    WHATSAPP_TRANSPORT: 'manual',
    WHATSAPP_CONTACT_URL: 'https://wa.me/5591992545353?text=Ol%C3%A1%2C%20vi%20minha%20devolutiva%20Valora%20Insight%E2%84%A2%20e%20quero%20falar%20com%20o%20Valora%20Group.',
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

// Final public result production defaults. No secrets are committed here.
window.ValoraProductionPublicResultFinalFix = Object.freeze({
  publicProductName: 'Valora Insight™',
  whatsappNumber: '5591992545353',
  certificateFeatureEnabled: false,
  emailProviderDefault: 'http_api'
});

// Valora Insight™ devolutiva engine final audit reviewed

// Public result links use ?result=<responseId>&rt=<raw-token>; hashes are never exposed.
window.ValoraProductionPublicResultLinkContract = Object.freeze({ resultParam: 'result', tokenParam: 'rt', productName: 'Valora Insight™' });

// Legacy final audit: production sharing uses PUBLIC_APP_URL plus raw bearer tokens only.
