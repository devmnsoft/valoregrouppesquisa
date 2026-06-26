# Sprint 5 — Auditoria de Arquitetura Híbrida e Migração

## Respostas obrigatórias

1. **Backend ASP.NET Core existe?** Sim: `backend/Valora.sln` contém Api, Domain, Application, Infrastructure e Tests.
2. **A solução backend compila?** A estrutura está pronta; neste container o comando não pôde ser executado porque `dotnet` não está instalado.
3. **PostgreSQL sobe via Docker?** Há `docker-compose.postgres.yml` com PostgreSQL 16 na porta 5434.
4. **Scripts SQL existem e estão completos?** Sim, a pasta `database/postgresql/` contém os scripts 001–011 exigidos, incluindo tabelas core, planos, surveys, responses, results, certificates, communications e audit.
5. **Cinco planos oficiais no seed?** Sim: free, essential, professional, corporate e enterprise em `010_seed_official_plans.sql`.
6. **Seed demo Valora Insight™?** Sim: `011_seed_demo_valora_insight.sql` cria organização, formulário, 5 dimensões, 25 perguntas, survey e link público demo.
7. **api-client.js existe?** Sim, expõe cliente HTTP com token bearer e tratamento de JSON/HTML.
8. **api-repository.js existe?** Sim, expõe autenticação, planos, jornada pública, certificados e status de migração.
9. **DATA_PROVIDER é respeitado?** Sim para provider `firebase`, `api` e `hybrid`; produção continua `firebase`.
10. **Jornada pública ainda chama callPublicFunction diretamente?** Não em `renderTakeSurvey`, `submitSurvey` ou `renderResult`; chamadas passam pelas funções oficiais.
11. **Gateway ainda necessário para e-mail?** Sim em produção Spark/external-api; SMTP backend/API é caminho paralelo local/controlado.
12. **Módulos ainda dependem de Firebase?** Auth legado, persistência principal em produção, Firestore repositories, algumas integrações administrativas e Cloud Functions opcionais.
13. **Módulos migráveis para API?** Planos públicos, register/login, me, jornada pública validate/submit/result, certificados metadata, comunicação de resultado e status de migração.
14. **Dados a normalizar antes do PostgreSQL?** Empresas/organizations, usuários/senhas, forms/questions/options, survey tokens, responses/answers, scores, communications e certificados.
15. **Riscos para DATA_PROVIDER=api em produção?** Homologação LGPD, migração de tokens/senhas, observabilidade, backup/rollback, performance, SMTP/IIS e paridade completa com Firestore.

## Ocorrências mapeadas

Foram auditadas ocorrências de `DATA_PROVIDER`, `HYBRID_PRIMARY_PROVIDER`, `API_BASE_URL`, providers públicos, gateway, Cloud Functions, renderização pública, certificados, migração e comunicação nos arquivos solicitados usando `rg`.
