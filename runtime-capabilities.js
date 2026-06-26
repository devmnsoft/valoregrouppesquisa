(function(){
'use strict';
const EMAIL_TRANSPORTS=new Set(['disabled','local-outbox','firebase-functions','external-api']);
const WHATSAPP_TRANSPORTS=new Set(['manual','disabled','external-api']);
function normalizeTransport(value){return EMAIL_TRANSPORTS.has(value)?value:'disabled';}
function normalizeWhatsappTransport(value){return WHATSAPP_TRANSPORTS.has(value)?value:'manual';}
function getRuntimeCapabilities(){
  const cfg=window.ValoraConfig||{};
  const transport=normalizeTransport(cfg.EMAIL_TRANSPORT||'disabled');
  const gateway=cfg.COMMUNICATION_GATEWAY||{};
  const gatewayBase=gateway.baseUrl||cfg.EXTERNAL_API_BASE_URL||'';
  const gatewayEnabled=gateway.enabled===true&&Boolean(gatewayBase);
  const localApi=cfg.LOCAL_API_ENABLED===true&&transport==='local-outbox';
  const functions=cfg.ENABLE_CLOUD_FUNCTIONS===true&&transport==='firebase-functions';
  const externalApi=transport==='external-api'&&gatewayEnabled;
  const whatsappTransport=normalizeWhatsappTransport(cfg.WHATSAPP_TRANSPORT||'manual');
  const whatsappExternal=whatsappTransport==='external-api'&&gatewayEnabled;
  const configuredProvider=cfg.PUBLIC_SUBMISSION_PROVIDER||(cfg.STORAGE_MODE==='local'?'local':(gatewayEnabled?'external-api':''));
  const provider=['external-api','firestore-client','local'].includes(configuredProvider)?configuredProvider:'';
  const firestoreFallback=provider==='firestore-client'&&cfg.STORAGE_MODE==='firebase'&&cfg.FIREBASE_ENABLED===true;
  const publicSubmitExternal=provider==='external-api'&&gatewayEnabled;
  const canUseFunctions=cfg.ENABLE_CLOUD_FUNCTIONS===true;
  const canSubmitPublic=provider==='local'||publicSubmitExternal||firestoreFallback||canUseFunctions;
  return Object.freeze({
    environment:cfg.RUNTIME_ENV||'unknown',
    storageMode:cfg.STORAGE_MODE||'local',
    firebasePlan:cfg.FIREBASE_PLAN||'none',
    localApi:Object.freeze({enabled:localApi,baseUrl:cfg.LOCAL_API_BASE_URL||''}),
    cloudFunctions:Object.freeze({enabled:cfg.ENABLE_CLOUD_FUNCTIONS===true}),
    email:Object.freeze({transport,available:localApi||functions||externalApi,canSend:localApi||functions||externalApi,canReadStatus:localApi||functions,canConfigureTransport:localApi,canEditTemplates:true,hasOutbox:localApi}),
    whatsapp:Object.freeze({transport:whatsappTransport,available:whatsappTransport==='manual'||whatsappExternal,canSendAutomatic:whatsappExternal,canShareManual:whatsappTransport==='manual',canSend:whatsappExternal,manualShare:whatsappTransport==='manual'}),
    communicationGateway:Object.freeze({enabled:gatewayEnabled,baseUrl:gatewayBase,mode:gateway.mode||'disabled',canSendResult:transport==='external-api'&&gatewayEnabled&&gateway.sendResultOnSurveyCompleted===true,allowManualResend:gateway.allowManualResend===true,timeoutMs:Number(gateway.timeoutMs||20000)}),
    publicSubmission:Object.freeze({provider,canSubmit:canSubmitPublic,requiresGateway:provider==='external-api',canFallbackToFirestoreClient:firestoreFallback}),
    logs:Object.freeze({canPersistRemote:cfg.observability?.remoteLogsEnabled===true&&cfg.ENABLE_CLOUD_FUNCTIONS===true})
  });
}
function hasRuntimeCapability(capability){const runtime=getRuntimeCapabilities();const map={emailSending:runtime.email.canSend,emailTransportConfig:runtime.email.canConfigureTransport,emailOutbox:runtime.email.hasOutbox,remoteLogging:runtime.logs.canPersistRemote};return map[capability]!==false;}
window.ValoraRuntime=Object.freeze({getCapabilities:getRuntimeCapabilities,hasRuntimeCapability});
})();
