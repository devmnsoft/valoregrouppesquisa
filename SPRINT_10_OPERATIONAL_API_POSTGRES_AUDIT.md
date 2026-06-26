# Sprint 10 — Auditoria operacional API/PostgreSQL

Auditoria criada antes das alterações funcionais da Sprint 10, cobrindo `package.json`, configurações, frontend provider, backend, PostgreSQL, migração, gateway, scripts e ferramentas Windows.

## Diagnóstico objetivo

1. **Arquivos prontos:** `api-client.js`, `api-repository.js`, `repository.js`, `runtime-capabilities.js`, `docker-compose.postgres.yml`, scripts principais de validação e documentação arquitetural inicial.
2. **Arquivos incompletos:** backend público ainda retornava contrato parcial para formulário/resultado, scripts de migração eram seguros em dry-run mas ainda não aplicavam PostgreSQL real sem driver/ambiente, e documentação Sprint 10 estava incompleta.
3. **Backend ASP.NET Core:** existe de verdade em `backend/Valora.sln`, com projetos Api, Application, Domain, Infrastructure e Tests.
4. **Compilação:** `dotnet` não está disponível neste container; a compilação deve ser executada no Windows/CI com SDK .NET 8.
5. **Controllers:** existem controllers reais para health, plans, auth, public, admin, communications e certificados.
6. **PostgreSQL Docker:** há `docker-compose.postgres.yml`; validação operacional depende de Docker disponível.
7. **Migrations idempotentes:** existem scripts `CREATE IF NOT EXISTS`/`ON CONFLICT`, porém havia numeração legada duplicada mantida por compatibilidade.
8. **Seed planos oficiais:** existe `010_seed_official_plans.sql`.
9. **Seed demo Valora Insight™:** existe `011_seed_demo_valora_insight.sql`.
10. **Validar survey demo:** backend precisava aceitar aliases demo e retornar formulário completo.
11. **Salvar resposta demo:** backend possuía persistência mínima; precisava calcular resultado e metadados completos.
12. **72/125:** calculadora existe; faltava teste dedicado e envelope público completo.
13. **Certificate metadata:** endpoint existe; resposta pública precisava expor metadata.
14. **email_job:** serviço/tabela existem; fluxo público precisava declarar status seguro.
15. **Export Firestore:** exporta coleções reais configuradas e dry-run seguro.
16. **Transformação:** gera arquivos normalizados em `migration/out` com warnings.
17. **Import:** suporta dry-run/apply/truncate/batch/backup em contrato operacional; apply exige ambiente controlado.
18. **Compare:** gera JSON e Markdown de comparação.
19. **DATA_PROVIDER=api:** provider existe e validadores passam em análise estática.
20. **DATA_PROVIDER=hybrid:** leitura compara em background e escrita usa apenas primário.
21. **Admin status:** runtime e repository expõem status/warnings; bloco UI já consome arquitetura parcial.
22. **Riscos cutover:** falta execução real com Docker/.NET neste container, credenciais Firestore de produção, SMTP transacional homologado e janela formal de manutenção.

## Ocorrências mapeadas

Termos auditados: `TODO`, `NotImplemented`, `throw new Error('não implementado')`, `501`, `mock`, `demo only`, `stub`, `DATA_PROVIDER`, `HYBRID_PRIMARY_PROVIDER`, `ALLOW_API_PRODUCTION_CUTOVER`, `API_BASE_URL`, `PUBLIC_SURVEY_VALIDATION_PROVIDER`, `PUBLIC_SUBMISSION_PROVIDER`, `RESULT_PROVIDER`, `callPublicFunction`, `firebaseCallable`, `submitSurveyResponse`, `validateSurveyLink`, `renderTakeSurvey`, `submitSurvey`, `renderResult`, `ValoraRepository`, `ValoraApiRepository`.

Resultado: os termos de provider existem por desenho; usos públicos diretos de Cloud Functions foram encapsulados por repository/fallback local; mocks/stubs críticos da rota pública API foram substituídos por respostas demo operacionais e JSON seguro.
