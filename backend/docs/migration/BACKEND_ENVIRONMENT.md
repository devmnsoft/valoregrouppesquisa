# Backend Environment

A API ASP.NET Core da Sprint 11 é configurada por `appsettings.json`, `appsettings.Development.json` e variáveis de ambiente. O arquivo `backend/.env.example` documenta apenas valores locais de desenvolvimento, sem segredo real.

## Local controlado

- PostgreSQL: `localhost:5434`, database `valoradb`, usuário `valora`.
- Provider frontend para teste: `DATA_PROVIDER=api` ou `DATA_PROVIDER=hybrid` em configuração local.
- Produção permanece Firebase e `ALLOW_API_PRODUCTION_CUTOVER=false`.

## Segurança

- Trocar `Jwt__Secret` em qualquer ambiente não local.
- Não colocar SMTP, service account Firebase ou token WhatsApp no frontend.
- E-mail transacional deve ficar no backend/gateway.
