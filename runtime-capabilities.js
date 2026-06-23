(function(){
'use strict';
const EMAIL_TRANSPORTS=new Set(['disabled','local-outbox','firebase-functions','external-api']);
function normalizeTransport(value){return EMAIL_TRANSPORTS.has(value)?value:'disabled';}
function getRuntimeCapabilities(){
  const cfg=window.ValoraConfig||{};
  const transport=normalizeTransport(cfg.EMAIL_TRANSPORT||'disabled');
  const localApi=cfg.LOCAL_API_ENABLED===true&&transport==='local-outbox';
  const functions=cfg.ENABLE_CLOUD_FUNCTIONS===true&&transport==='firebase-functions';
  const externalApi=transport==='external-api'&&Boolean(cfg.EXTERNAL_API_BASE_URL);
  return Object.freeze({
    environment:cfg.RUNTIME_ENV||'unknown',
    storageMode:cfg.STORAGE_MODE||'local',
    firebasePlan:cfg.FIREBASE_PLAN||'none',
    localApi:Object.freeze({enabled:localApi,baseUrl:cfg.LOCAL_API_BASE_URL||''}),
    cloudFunctions:Object.freeze({enabled:cfg.ENABLE_CLOUD_FUNCTIONS===true}),
    email:Object.freeze({transport,available:localApi||functions||externalApi,canReadStatus:localApi||functions||externalApi,canConfigureTransport:localApi,canEditTemplates:true,canSend:localApi||functions||externalApi,hasOutbox:localApi}),
    logs:Object.freeze({canPersistRemote:cfg.observability?.remoteLogsEnabled===true&&cfg.ENABLE_CLOUD_FUNCTIONS===true})
  });
}
function hasRuntimeCapability(capability){
  const runtime=getRuntimeCapabilities();
  const map={emailSending:runtime.email.canSend,emailTransportConfig:runtime.email.canConfigureTransport,emailOutbox:runtime.email.hasOutbox,remoteLogging:runtime.logs.canPersistRemote};
  return map[capability]!==false;
}
window.ValoraRuntime=Object.freeze({getCapabilities:getRuntimeCapabilities,hasRuntimeCapability});
})();
