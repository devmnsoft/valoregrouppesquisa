# Diagnóstico inicial — Release Candidate Backend Oficial

Data: 2026-07-02
Versão alvo: `0.9.0-rc1`

## 1. Estado atual da solution `backend/Valora.sln`
A solution oficial existe em `backend/Valora.sln` e referencia os projetos oficiais `Valora.Api`, `Valora.Application`, `Valora.Domain`, `Valora.Infrastructure`, `Valora.Tests` e `Valora.Web`. Nesta estação, a validação .NET real ficou bloqueada porque o comando `dotnet` não está instalado.

## 2. Estado atual dos validadores Node
Os validadores oficiais existentes foram executados com Node `v24.15.0` e npm `11.4.2`. Os validadores `backend:official-validate`, `backend:reports-email-validate`, `backend:migration-import-validate`, `backend:homologation-cutover-validate` e `check:critical` passaram antes das alterações de RC.

## 3. Estado atual dos scripts Windows/Linux
Existem scripts de homologação para Windows e Linux em `tools/windows/backend-hml-*` e `tools/linux/backend-hml-*`. Antes desta sprint não existiam os scripts oficiais `backend-prd-*` de build, publish, package e healthcheck para produção.

## 4. Estado atual do Docker/PostgreSQL de homologação
`docker-compose.yml` define PostgreSQL de homologação, mas Docker não está instalado neste ambiente (`docker: command not found`). A aplicação do schema também ficou bloqueada por ausência de `psql`.

## 5. Estado atual dos testes unitários
Os testes estão em `backend/Valora.Tests`. A execução real ficou bloqueada por ausência do .NET SDK.

## 6. Estado atual dos testes integrados
Há testes integrados para homologação PostgreSQL, mas a execução com banco real ficou bloqueada por ausência de Docker/PostgreSQL local.

## 7. Estado atual do SQL oficial
O SQL oficial existe em `scriptbd_completo.sql`, `database/postgresql/scriptbd_completo.sql` e scripts complementares em `database/postgresql/*.sql`.

## 8. Estado atual dos health checks
A API declara endpoints `/health`, `/health/database`, `/health/migration`, `/health/email`, `/health/storage` e `/health/version`. O Web MVC declara páginas operacionais `/Operations/Health`, `/Operations/Version` e `/Operations/Checks`.

## 9. Estado atual da importação com amostras
As amostras sanitizadas existem em `docs/migration-samples/`. O validador de importação passou, mas o fluxo com PostgreSQL real ficou bloqueado pela ausência de Docker/PostgreSQL.

## 10. Estado atual do backup/restore
Existem scripts de backup/restore de homologação para Linux e Windows. A execução real ficou bloqueada pela ausência de PostgreSQL/pg_dump/psql.

## 11. Estado atual do Web MVC
`backend/Valora.Web` é o front oficial ASP.NET MVC. Não foi criado novo frontend, SPA, React, Vue, Angular ou Vite.

## 12. Estado atual da API
`backend/Valora.Api` é a API oficial ASP.NET Core. O build/runtime não pôde ser exercitado neste ambiente por ausência de .NET SDK.

## 13. Gaps documentados que ainda precisam ser resolvidos
- Instalar .NET SDK 8+ para restore/build/test reais.
- Instalar Docker e cliente PostgreSQL para homologação local real.
- Executar API/Web e validar fluxos ponta a ponta em ambiente com dependências completas.
- Gerar pacote real em ambiente com .NET SDK.

## 14. Riscos para homologação
- Ambiente incompleto pode atrasar validação ponta a ponta.
- Fluxos que dependem de SMTP, PostgreSQL e publicação precisam de execução assistida antes de usuários piloto.

## 15. Riscos para produção
- Não liberar produção sem restore/build/test reais, smoke test, backup/restore e aprovação manual de cutover.
- Não empacotar secrets, dumps, logs sensíveis ou dados reais.

## 16. Plano objetivo da sprint
1. Registrar evidências iniciais.
2. Criar versão e notas de RC.
3. Criar scripts oficiais de build/publish/package/healthcheck.
4. Criar validador de Release Candidate.
5. Atualizar documentação e checklists.
6. Executar comandos possíveis e documentar bloqueios reais.
