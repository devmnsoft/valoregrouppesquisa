#!/usr/bin/env node
const {ok,app}=require('./_legacy-final-validators');
ok(/https:\/\/wa\.me\//.test(app),'WhatsApp uses wa.me');
ok(/function publicWhatsappContactUrl/.test(app)&&/function whatsappLink/.test(app)&&/function openWhatsapp/.test(app),'WhatsApp contact helpers exist');
ok(/shareSurveyWhatsapp/.test(app)&&/preparePublicSurveyLink/.test(app),'survey WhatsApp share prepares public link');
ok(/shareResultWhatsapp/.test(app)&&/resultToken/.test(app),'result WhatsApp share requires token');
