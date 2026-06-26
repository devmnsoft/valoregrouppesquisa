@echo off
setlocal
cd /d %~dp0\..\..
npm run check
npm run architecture:check
npm run migration:validate
npm run backend:build
npm run backend:test
npm run build:prod
