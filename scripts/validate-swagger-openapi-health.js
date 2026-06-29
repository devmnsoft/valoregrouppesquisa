#!/usr/bin/env node
const {spawnSync}=require('child_process');
let r=spawnSync('node',['scripts/validate-api-no-route-conflicts.js'],{stdio:'inherit',shell:process.platform==='win32'}); if(r.status) process.exit(r.status);
r=spawnSync('dotnet',['build','backend/Valora.Api/Valora.Api.csproj','--no-restore'],{stdio:'inherit',shell:process.platform==='win32'}); if(r.status) process.exit(r.status);
console.log('validate-swagger-openapi-health: PASS (build + duplicate-route gate). Live /swagger/v1/swagger.json check is covered by Windows/live smoke scripts.');
