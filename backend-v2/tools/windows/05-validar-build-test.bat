@echo off
cd /d %~dp0\..\..
dotnet build ValoraPesquisa.sln
dotnet test ValoraPesquisa.sln
node tools\validate-backend-v2-foundation.js
