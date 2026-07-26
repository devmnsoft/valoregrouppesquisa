@echo off
echo Validar config.js com DATA_PROVIDER=firebase e ALLOW_API_PRODUCTION_CUTOVER=false
node scripts\validate-cutover-readiness.js
