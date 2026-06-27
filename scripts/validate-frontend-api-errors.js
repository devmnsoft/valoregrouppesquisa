#!/usr/bin/env node
const fs=require('fs');const api=fs.readFileSync('api-client.js','utf8');const app=fs.readFileSync('app.js','utf8');
const missing=[];for(const t of ['message','code','traceId','correlationId','normalizeApiError'])if(!api.includes(t))missing.push(`api-client.js: ${t}`);for(const t of ['Código de atendimento','publicApiFriendlyError','Tentar novamente'])if(!app.includes(t))missing.push(`app.js: ${t}`);if(missing.length){console.error(missing.join('\n'));process.exit(1);}console.log('validate-frontend-api-errors: PASS');
