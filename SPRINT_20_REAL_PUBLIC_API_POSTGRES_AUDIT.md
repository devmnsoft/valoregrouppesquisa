# Sprint 20 — Auditoria da Jornada Pública Real API/PostgreSQL

Data: 2026-06-26

## Escopo auditado

Arquivos e diretórios auditados: `backend/Valora.Api/Controllers/PublicSurveysController.cs`, `backend/Valora.Api/Controllers/PublicResultsController.cs`, `backend/Valora.Api/Controllers/AuthController.cs`, `backend/Valora.Api/Program.cs`, `backend/Valora.Application/`, `backend/Valora.Infrastructure/`, `backend/Valora.Domain/`, `database/postgresql/`, `migration/`, `scripts/`, `api-client.js`, `api-repository.js`, `repository.js`, `app.js` e `package.json`.

## Respostas objetivas

1. **Onde ainda existe IsDemo?** Não encontrado no fluxo público oficial auditado.
2. **Onde ainda existe demo-public-token?** Não encontrado.
3. **Onde ainda existe demo-valora-insight?** Apenas em seed/teste local: `database/postgresql/012_seed_demo_valora_insight.sql`.
4. **Onde ainda existe BuildDemoSurvey?** Não encontrado.
5. **Onde ainda existe CalculateDemoResult?** Não encontrado.
6. **Onde ainda existe pending-backfill?** Não encontrado no backend público.
7. **Onde ainda existe metadata-ready hardcoded?** Em metadados reais de certificado: `PublicSurveyService`, `PublicResultService`, `CertificateRepository` e migration de certificados. Não existe em controller.
8. **Onde ainda existe survey = new { }?** Não encontrado.
9. **Onde ainda existe company = new { }?** Não encontrado.
10. **Onde ainda existe certificate = new { }?** Não encontrado.
11. **Onde controller calcula resultado diretamente?** Não encontrado em `PublicSurveysController` ou `PublicResultsController`.
12. **Onde controller instancia service manualmente com new?** Não encontrado em controllers públicos; usam DI por construtor primário.
13. **Onde PublicResultsController não valida hash do resultToken?** Corrigido: controller delega para `IPublicResultService`; service valida `responses.result_token_hash` com `IResultTokenService.Verify`.
14. **Onde a submissão pública não usa transação?** Corrigido em `PublicSurveyService.SubmitAsync` com `BeginTransaction`, `Commit` e `Rollback`.
15. **Onde response_answers não são persistidos?** Corrigido via `IResponseRepository.AddAnswersAsync`/`ResponseRepository.AddAnswersAsync`.
16. **Onde result_scores não são persistidos?** Corrigido via `IResultRepository.SaveResultAsync`/`ResultRepository.SaveResultAsync`.
17. **Onde dimension_scores não são persistidos?** Corrigido via `IResultRepository.SaveDimensionScoresAsync`/`ResultRepository.SaveDimensionScoresAsync`.
18. **Onde certificate metadata não é persistido?** Corrigido via `ICertificateRepository.CreateMetadataAsync`/`CertificateRepository.CreateMetadataAsync`.
19. **Onde email_job não é criado?** Corrigido em `PublicSurveyService.SubmitAsync` quando `communicationConsent=true` e há e-mail.
20. **Onde audit_log não é criado?** Corrigido em `PublicSurveyService.SubmitAsync` via `IAuditRepository.LogAsync` dentro da transação.
21. **Onde ainda existe CREATE SCHEMA IF NOT EXISTS valora?** Não encontrado; o runner e migrations usam `valorapesquisa`.
22. **Onde ainda existe valora., billing., communication., audit. ou migration.?** Não encontrado como schema SQL canônico em `backend`, `database/postgresql`, `migration` e `scripts`; menções textuais aparecem em nomes de scripts/documentação ou identificadores de frontend.
23. **Onde repository não usa Dapper?** Validadores indicam que repositories em `backend/Valora.Infrastructure/Repositories` importam Dapper.
24. **Onde há SQL direto em controller?** Não encontrado pelos validadores.
25. **Onde ainda existe TODO, mock, stub, demo-only ou NotImplemented?** Há TODO funcional no painel de logs do `app.js`, placeholder legado `migration/transform-to-postgres.js` e validações textuais em scripts. Não bloqueiam o fluxo público API/PostgreSQL real.

## Diagnóstico consolidado

- Controllers públicos estão finos e delegam para services de Application.
- O modo demo saiu dos controllers e permanece somente em seed/teste local.
- A jornada pública API/PostgreSQL passa a validar survey/link/form no banco, persistir resposta e scores, criar certificado, comunicação/e-mail e auditoria.
- `resultToken` é retornado uma única vez e somente o hash SHA-256 é persistido em `valorapesquisa.responses.result_token_hash`.
- `MigrationRunner` usa somente `valorapesquisa.schema_migrations`, lê apenas `.sql` diretamente em `database/postgresql`, executa por arquivo em transação e não repete scripts aplicados.
