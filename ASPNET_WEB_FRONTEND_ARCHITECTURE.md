# Arquitetura Frontend ASP.NET

Valora.Web é MVC/Razor, sem acesso direto a banco, repositories, Dapper, EF ou Firebase. O navegador consome somente Valora.Api por jQuery AJAX através de `ajax-client.js`, com token em `sessionStorage`, `X-Correlation-Id`, `requestJson` e `requestBinary`.
