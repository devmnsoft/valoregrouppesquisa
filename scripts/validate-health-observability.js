#!/usr/bin/env node
const fs=require('fs');const s=fs.readFileSync('backend/Valora.Api/Controllers/HealthController.cs','utf8');
const required=['/health','/health/database','/health/logging','/health/migration','/health/version','correlationId'];const missing=required.filter(x=>!s.includes(x));if(missing.length){console.error(missing.join('\n'));process.exit(1);}console.log('validate-health-observability: PASS');
