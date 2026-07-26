# ASP.NET Web Observability

- logs do Valora.Web: Serilog em console no Docker e stdout/IIS stdout quando habilitado no `web.config` somente para diagnóstico temporário.
- logs da Valora.Api: console do container `valora-api`, Event Viewer/IIS quando hospedada no Windows, e logs estruturados com correlationId.
- correlationId: o Web preserva `X-Correlation-Id` quando recebido e health retorna `correlationId` para rastrear tela até API.
- IIS: verificar Event Viewer, stdoutLogFile configurado, permissões da pasta de logs e reciclagens do Application Pool.
- Docker: usar `docker compose logs -f valora-web valora-api` e conferir variáveis `Api__BaseUrl` e `WebApp__PublicUrl`.
- Investigar 500: obter correlationId, validar `/health/web`, `/health/web/version`, logs do Web, depois `/health` e `/health/database` da API.
- API offline: validar DNS/porta/firewall, `Api__BaseUrl`, CORS e health da API antes de reprocessar operação do usuário.
- CORS: confirmar origem pública do Web na configuração da API e validar preflight no navegador.
- token expirado: reproduzir login, limpar storage do navegador e verificar redirecionamento 401 para login sem expor token.
