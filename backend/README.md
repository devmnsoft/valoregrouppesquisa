# Valora Backend Oficial

`backend/Valora.sln` é a base única e oficial da nova versão .NET do Valora.

## Projetos

- `Valora.Api`: API oficial.
- `Valora.Application`: DTOs, contratos e regras de aplicação.
- `Valora.Domain`: entidades e tipos de domínio.
- `Valora.Infrastructure`: Dapper, PostgreSQL, repositories, segurança e e-mail.
- `Valora.Tests`: testes oficiais.
- `Valora.Web`: front oficial ASP.NET Core MVC/Razor com Bootstrap 5, JavaScript puro e jQuery/AJAX.

## Banco

A persistência oficial usa PostgreSQL, Dapper, schema `valorapesquisa` e scripts em `database/postgresql`.

## Referência temporária

`../backend-v2` permanece preservado somente como referência histórica/técnica. Não implemente novas features nele.

## Comandos

```bash
dotnet build backend/Valora.sln
dotnet test backend/Valora.sln
npm run backend:official-validate
```

## Operacional pós-resposta
A solução oficial inclui contratos iniciais para relatórios, certificados, exportações, LGPD e e-mail. Use:

```bash
npm run backend:official-validate
npm run backend:reports-email-validate
```

SMTP real deve ser configurado por `VALORA_SMTP_HOST`, `VALORA_SMTP_PORT`, `VALORA_SMTP_SECURITY`, `VALORA_SMTP_USERNAME`, `VALORA_SMTP_PASSWORD`, `VALORA_SMTP_FROM_EMAIL` e `VALORA_SMTP_FROM_NAME`. Em desenvolvimento, a fila pode operar como `DevelopmentOutbox` sem expor senha na Web.

## Importação do legado

Use `POST /migration/batches`, `POST /migration/batches/{batchId}/dry-run`, `POST /migration/batches/{batchId}/apply`, `POST /migration/batches/{batchId}/rollback` e `GET /migration/batches/{batchId}/cutover-readiness`. Toda fonte deve ser JSON exportado; não há chamada ao Firebase real.
