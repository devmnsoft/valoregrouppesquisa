# Publicação IIS do Valora.Web

1. Instalar .NET 8 Hosting Bundle no Windows Server.
2. Criar site no IIS apontando para a pasta publicada do Valora.Web.
3. Configurar Application Pool como **No Managed Code** e identidade com permissão de leitura na pasta.
4. Definir variáveis: `ASPNETCORE_ENVIRONMENT=Production`, `Api__BaseUrl=https://api.valoragroup.mnsoft.com.br`, `WebApp__PublicUrl=https://valoragroup.mnsoft.com.br`.
5. Publicar com `dotnet publish backend/Valora.Web/Valora.Web.csproj -c Release -o publish/valora-web` ou script dry-run.
6. Validar `web.config`, `wwwroot`, `Views`, `/web-config.js`, `/health/web` e `/health/web/version`.
7. Logs: habilitar stdout temporariamente apenas durante incidente e desabilitar após coleta.
8. Rollback: manter release anterior em pasta versionada, trocar physical path do site e reciclar pool.
9. Smoke test: executar `tools\windows\web\06-validar-web-publicado-smoke.bat` com `VALORA_WEB_URL`.
