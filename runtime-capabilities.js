(function(){
'use strict';
const EMAIL_TRANSPORTS=new Set(['disabled','local-outbox','firebase-functions','external-api']);
const WHATSAPP_TRANSPORTS=new Set(['manual','disabled','external-api']);
function normalizeTransport(value){return EMAIL_TRANSPORTS.has(value)?value:'disabled';}
function normalizeWhatsappTransport(value){return WHATSAPP_TRANSPORTS.has(value)?value:'manual';}
function normalizeProvider(value,fallback){return ['external-api','firebase-functions','cloud-functions','local'].includes(value)?(value==='cloud-functions'?'firebase-functions':value):fallback;}
function getRuntimeCapabilities(){
  const cfg=window.ValoraConfig||{};
  const transport=normalizeTransport(cfg.EMAIL_TRANSPORT||'disabled');
  const gateway=cfg.COMMUNICATION_GATEWAY||{};
  const gatewayBase=(gateway.baseUrl||cfg.EXTERNAL_API_BASE_URL||'').replace(/\/+$/,'');
  const gatewayEnabled=gateway.enabled===true&&Boolean(gatewayBase);
  const localApi=cfg.LOCAL_API_ENABLED===true&&transport==='local-outbox';
  const functionsEnabled=cfg.ENABLE_CLOUD_FUNCTIONS===true;
  const functions=functionsEnabled&&transport==='firebase-functions';
  const externalApi=transport==='external-api'&&gatewayEnabled;
  const whatsappTransport=normalizeWhatsappTransport(cfg.WHATSAPP_TRANSPORT||'manual');
  const whatsappExternal=whatsappTransport==='external-api'&&gatewayEnabled;
  const isSparkProduction=cfg.RUNTIME_ENV==='production'&&String(cfg.FIREBASE_PLAN||'').toLowerCase()==='spark';
  const defaultProvider=cfg.RUNTIME_ENV==='local'?'local':(gatewayEnabled?'external-api':'local');
  const validationProvider=normalizeProvider(cfg.PUBLIC_SURVEY_VALIDATION_PROVIDER,defaultProvider);
  const submissionProvider=normalizeProvider(cfg.PUBLIC_SUBMISSION_PROVIDER,defaultProvider);
  const resultProvider=normalizeProvider(cfg.RESULT_PROVIDER,defaultProvider);
  const usesCloudFunctions=!isSparkProduction&&functionsEnabled&&[validationProvider,submissionProvider,resultProvider].includes('firebase-functions');
  const usesGateway=[validationProvider,submissionProvider,resultProvider].includes('external-api');
  const canValidateSurvey=validationProvider==='local'||(validationProvider==='external-api'&&gatewayEnabled)||(validationProvider==='firebase-functions'&&usesCloudFunctions);
  const canSubmitResponse=submissionProvider==='local'||(submissionProvider==='external-api'&&gatewayEnabled)||(submissionProvider==='firebase-functions'&&usesCloudFunctions);
  const canLoadResult=resultProvider==='local'||(resultProvider==='external-api'&&gatewayEnabled)||(resultProvider==='firebase-functions'&&usesCloudFunctions);
  return Object.freeze({
    environment:cfg.RUNTIME_ENV||'unknown',
    storageMode:cfg.STORAGE_MODE||'local',
    firebasePlan:cfg.FIREBASE_PLAN||'none',
    localApi:Object.freeze({enabled:localApi,baseUrl:cfg.LOCAL_API_BASE_URL||''}),
    cloudFunctions:Object.freeze({enabled:functionsEnabled}),
    email:Object.freeze({transport,available:localApi||functions||externalApi,canSend:localApi||functions||externalApi,canReadStatus:localApi||functions||externalApi,canConfigureTransport:localApi,canEditTemplates:true,hasOutbox:localApi}),
    whatsapp:Object.freeze({transport:whatsappTransport,available:whatsappTransport==='manual'||whatsappExternal,canSendAutomatic:whatsappExternal,canShareManual:whatsappTransport==='manual',canSend:whatsappExternal,manualShare:whatsappTransport==='manual'}),
    communicationGateway:Object.freeze({enabled:gatewayEnabled,baseUrl:gatewayBase,mode:gateway.mode||'disabled',canSendResult:transport==='external-api'&&gatewayEnabled&&gateway.sendResultOnSurveyCompleted===true,allowManualResend:gateway.allowManualResend===true,timeoutMs:Number(gateway.timeoutMs||20000)}),
    publicSubmission:Object.freeze({provider:submissionProvider,canSubmit:canSubmitResponse,requiresGateway:submissionProvider==='external-api'}),
    publicJourney:Object.freeze({validationProvider,submissionProvider,resultProvider,canValidateSurvey,canSubmitResponse,canLoadResult,usesGateway,usesCloudFunctions}),
    logs:Object.freeze({canPersistRemote:cfg.observability?.remoteLogsEnabled===true&&functionsEnabled})
  });
}
function hasRuntimeCapability(capability){const runtime=getRuntimeCapabilities();const map={emailSending:runtime.email.canSend,emailTransportConfig:runtime.email.canConfigureTransport,emailOutbox:runtime.email.hasOutbox,remoteLogging:runtime.logs.canPersistRemote};return map[capability]!==false;}
window.ValoraRuntime=Object.freeze({getCapabilities:getRuntimeCapabilities,hasRuntimeCapability});
})();
