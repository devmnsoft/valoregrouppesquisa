# Release Candidate Notes — Valora Backend Oficial

## 1. Versão sugerida
`0.9.0-rc1`

## 2. Data
2026-07-02

## 3. Escopo
Release Candidate de homologação real da versão oficial baseada exclusivamente em `backend/Valora.sln` e `database/postgresql`.

## 4. Features entregues
- API ASP.NET Core oficial.
- Web MVC ASP.NET oficial.
- Scripts de homologação PostgreSQL.
- Scripts oficiais de build, publish, package e healthcheck de produção.
- Validador automatizado de Release Candidate.
- Documentação de homologação, segurança, backup/restore e plano piloto.

## 5. Gaps conhecidos
- Neste ambiente não há .NET SDK, Docker, `psql` ou `pg_dump`; portanto restore/build/test, banco real, API/Web runtime e pacote real devem ser reexecutados em estação completa.

## 6. Como instalar
1. Instalar .NET SDK 8+.
2. Instalar Docker e cliente PostgreSQL.
3. Copiar `.env.example` para `.env` local de homologação sem versionar secrets.
4. Executar scripts `tools/linux/backend-hml-*` ou `tools/windows/backend-hml-*`.

## 7. Como validar
Executar `dotnet restore backend/Valora.sln`, `dotnet build backend/Valora.sln`, `dotnet test backend/Valora.sln` e todos os validadores `npm run backend:*validate` oficiais.

## 8. Como reverter
Seguir `ROLLBACK_PLAN.md` e `BACKUP_RESTORE_RUNBOOK.md`; restore exige `CONFIRM_RESTORE=RESTORE_LOCAL_HML` em ambiente local/homologação descartável.

## 9. Riscos
- Não executar cutover automático.
- Não usar dados reais versionados.
- Não expor secrets, hashes, tokens, stack traces ou connection strings.

## 10. Checklist de aceite
- [ ] Restore/build/test reais executados em ambiente completo.
- [ ] PostgreSQL homologação criado e schema aplicado.
- [ ] API/Web sobem e health checks respondem.
- [ ] Fluxos SaaS, pesquisa, relatórios, LGPD, e-mail, importação e backup/restore validados.
- [ ] Pacote de produção gerado sem secrets/dumps/logs sensíveis.
