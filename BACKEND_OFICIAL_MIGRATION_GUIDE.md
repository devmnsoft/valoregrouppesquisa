# Guia de Migração — Backend Oficial Valora

## Fonte oficial

A partir desta sprint, `backend/Valora.sln` é a solução oficial da nova versão .NET do Valora. Toda nova feature deve ser implementada em `backend`.

## Estruturas oficiais

- API oficial: `backend/Valora.Api`.
- Application oficial: `backend/Valora.Application`.
- Domain oficial: `backend/Valora.Domain`.
- Infrastructure oficial: `backend/Valora.Infrastructure`.
- Testes oficiais: `backend/Valora.Tests`.
- Frontend oficial: `backend/Valora.Web` com ASP.NET Core MVC/Razor, Bootstrap 5, JavaScript puro e jQuery/AJAX.
- Banco oficial: PostgreSQL/Dapper no schema `valorapesquisa`, com scripts em `database/postgresql`.

## Papel do `backend-v2`

`backend-v2` é apenas referência temporária. Ele não deve receber novas evoluções, não deve ser usado como base de build oficial e não deve gerar outro frontend ou solution paralela.

## Regras de consolidação

- Não criar `backend-v3` ou qualquer solution paralela.
- Não mover a nova versão para outro diretório.
- Não apagar o legado JavaScript/Firebase da raiz.
- Não apagar `backend-v2` ainda.
- Não criar React, Vue, Angular, Vite ou SPA.
- Não expor senha, hashes, refresh tokens, segredo SMTP, connection string, stack trace ou payload sensível.
- Não retornar dados fake em telas/endpoints oficiais.

## Validação oficial

Execute:

```bash
npm run backend:official-validate
```

Quando houver SDK .NET disponível, execute também:

```bash
dotnet build backend/Valora.sln
dotnet test backend/Valora.sln
```

## Sprint SaaS + relatórios + certificados + LGPD + e-mail
A evolução oficial permanece em `backend/Valora.sln`. Novos recursos operacionais usam Dapper/PostgreSQL no schema `valorapesquisa`, controllers oficiais da API e Web MVC/Razor. SMTP real deve usar apenas variáveis `VALORA_SMTP_*`; a UI não expõe senha SMTP.

## Importação controlada legado/Firebase/localStorage

A importação oficial ocorre apenas em `backend/Valora.sln` e usa PostgreSQL. Fontes aceitas: export Firestore em JSON, export localStorage/database.sample e JSON manual. O Firebase real não é chamado.

Fluxo operacional: registrar fonte, criar batch, executar dry-run, revisar divergências/conflitos, reconciliar, aplicar com `confirmApply=true` e perfil `admin_valora`, validar prontidão, e manter rollback por batch. Rollback exige `confirmRollback=true`.

Nenhuma tela ou API exibe senha, hash, token, refresh token, connection string, segredo SMTP, stack trace ou payload bruto sensível. Tokens públicos legados devem ser mascarados/regenerados.

## Sprint homologação/cutover backend oficial (2026-07-02)

- Homologação local: copie `.env.example` para `.env`, ajuste apenas credenciais locais e execute `tools/linux/backend-hml-01-subir-postgres.sh` ou `tools\windows\backend-hml-01-subir-postgres.bat`.
- Banco PostgreSQL: aplique `database/postgresql/scriptbd_completo.sql` e migrações com `tools/linux/backend-hml-02-aplicar-banco.sh` ou `tools\windows\backend-hml-02-aplicar-banco.bat`.
- API/Web: rode `tools/linux/backend-hml-03-rodar-api.sh` e `tools/linux/backend-hml-04-rodar-web.sh` (equivalentes Windows disponíveis).
- Testes integrados: defina `VALORA_TEST_POSTGRES_CONNECTION` somente para base local/homologação descartável e execute `dotnet test backend/Valora.sln` em ambiente com .NET SDK 8.
- Migração com amostra: use `docs/migration-samples/*.json`; todo apply real exige dry-run, relatório de divergências, ausência de conflito bloqueante, confirmação explícita, batch, rollback e auditoria.
- Backup: execute `tools/linux/backend-hml-06-backup.sh` ou `tools\windows\backend-hml-06-backup.bat`; dumps ficam fora do versionamento.
- Restore: use `BACKUP_RESTORE_RUNBOOK.md`; exige `CONFIRM_RESTORE=RESTORE_LOCAL_HML` e, em produção, também `CONFIRM_PRODUCTION_RESTORE=true`.
- Health: valide `/health`, `/health/database`, `/health/migration`, `/health/email`, `/health/storage` e `/health/version`; na Web MVC use `/Operations/Health`, `/Operations/Version` e `/Operations/Checks`.
- Checklist/cutover/rollback: siga `HOMOLOGACAO_CUTOVER_CHECKLIST.md`, `CUTOVER_PLAN.md`, `ROLLBACK_PLAN.md` e `LEGACY_RETIREMENT_PLAN.md`. O cutover não é automático nesta sprint.
- Validação: execute `npm run backend:homologation-cutover-validate` junto dos validadores oficiais.

## Release Candidate 0.9.0-rc1

Esta documentação passa a considerar o Release Candidate `0.9.0-rc1` como pacote de homologação real da versão oficial localizada em `backend/Valora.sln` e `database/postgresql`. O legado e `backend-v2` permanecem apenas como referência histórica e não fazem parte do build oficial.

Antes de produção, execute em ambiente completo: `dotnet restore backend/Valora.sln`, `dotnet build backend/Valora.sln`, `dotnet test backend/Valora.sln`, validadores npm oficiais, PostgreSQL homologação, health checks HTTP, fluxos SaaS/pesquisa/relatórios/LGPD/e-mail/importação, backup/restore descartável e scripts `tools/*/backend-prd-*`.

Não versionar `.env`, dumps, logs sensíveis, dados reais, certificados reais ou secrets. Não executar cutover produtivo automático; seguir `CUTOVER_PLAN.md`, `ROLLBACK_PLAN.md`, `BACKUP_RESTORE_RUNBOOK.md`, `RELEASE_CANDIDATE_NOTES.md` e `PILOT_USERS_HOMOLOGATION_PLAN.md`.
