(function(){
'use strict';
window.ValoraConfig=Object.freeze({
  APP_VERSION:'8.6.8',
  RUNTIME_ENV:'local',
  STORAGE_MODE:'local',
  FIREBASE_ENABLED:false,
  FIREBASE_PLAN:'none',
  ENABLE_CLOUD_FUNCTIONS:false,
  REQUIRE_AUTH_SERVER_VALIDATION:false,
  LOCAL_API_ENABLED:true,
  LOCAL_API_BASE_URL:'',
  EMAIL_TRANSPORT:'local-outbox',
  observability:{enabled:true,consoleEnabled:true,consoleLevel:'debug',persistLogs:true,remoteLogsEnabled:false,telegramEnabled:false,legacyTraceEnabled:true,maskSensitiveData:true,maxLocalLogs:3000,environment:'local'},
  FIREBASE_CONFIG:{},
  STORE_KEY:'valoraPulseFinal800'
});
})();
