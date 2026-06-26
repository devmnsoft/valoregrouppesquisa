#!/usr/bin/env node
const fs=require('fs');
const req=['Dockerfile','docker-compose.yml','docker-compose.postgres.yml','backend/Valora.Api/appsettings.Development.json','BACKEND_ENVIRONMENT.md','tools/windows/backend/01-restaurar-pacotes.bat','tools/windows/backend/02-build-backend.bat','tools/windows/backend/03-rodar-api-windows.bat','tools/windows/backend/04-testar-api-health.bat','tools/windows/backend/05-testar-api-public-survey.bat','tools/windows/docker/01-subir-ambiente-docker.bat','tools/windows/docker/02-parar-ambiente-docker.bat','tools/windows/docker/03-logs-api-docker.bat','tools/windows/docker/04-testar-health-docker.bat'];
const miss=req.filter(f=>!fs.existsSync(f)); if(miss.length){console.error('Ausentes: '+miss.join(', '));process.exit(1)}
console.log('validate-docker-windows-runtime: PASS');
