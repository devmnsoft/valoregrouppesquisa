#!/usr/bin/env node
const {has}=require('./validator-utils');
has('backend/Valora.Web/wwwroot/js/pages/free-diagnostics-page.js','data-whatsapp','window.open','Falar com especialista Valora');
has('FREE_SURVEY_WHATSAPP_AUDIT.md','free_survey.whatsapp_cta_clicked','Não abrir WhatsApp automaticamente');
console.log('validate-free-diagnostics-whatsapp-audit: PASS');
